using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;

namespace SLang.NET.Gen
{
    public class RoutineReference
    {
        public Identifier Name { get; protected set; }

        /// <summary>
        /// Unit reference applies to routines declared within SLang Units.
        /// Global routines do not have explicit unit binding -- instead they are defined within
        /// special "$Runtime" unit, and only black magic of Context shall be able to resolve those.
        ///
        /// With that being said, UnitReference may be null, but after Resolve() UnitDefinition is always set.
        /// </summary>
        public UnitReference Unit { get; protected set; }

        public Context Context { get; }

        /// <summary>
        /// Global routines are always static. Routines declared within units must be marked as static explicitly.
        /// Default value `true` is provided as a temporary measure until IR supports such modifier.
        /// </summary>
        public bool IsStatic { get; set; } = true;

        public RoutineReference(UnitReference unitReference, Identifier name)
        {
            Name = name;
            Unit = unitReference;
            Context = Unit.Context;
        }

        public RoutineReference(Context ctx, Identifier name)
        {
            Name = name;
            Unit = null;
            Context = ctx;
        }

        public RoutineReference(Context ctx, Callee callee)
        {
            Name = callee.Routine;
            Unit = callee.Unit == null ? null : new UnitReference(ctx, callee.Unit);
            Context = ctx;
        }

        public virtual RoutineDefinition Resolve()
        {
            return Context.Resolve(this);
        }

        public override string ToString()
        {
            return Unit == null ? $"::{Name}" : $"{Unit}::{Name}";
        }

        public override bool Equals(object obj)
        {
            if (obj is RoutineReference other)
            {
                return Unit.Equals(other.Unit) && Name.Equals(other.Name);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }

    public abstract class RoutineDefinition : RoutineReference, IStagedCompilation
    {
        public abstract bool IsNative { get; }
        public ISignature<UnitReference> SignatureReference { get; protected set; }
        public ISignature<UnitDefinition> SignatureDefinition { get; protected set; }
        public new UnitDefinition Unit { get; protected set; }

        /// <summary>
        /// Underlying native method. This property is null until Compile() is called.
        /// </summary>
        public MethodDefinition NativeMethod { get; protected set; }

        protected RoutineDefinition(UnitDefinition unit, Identifier name, ISignature<UnitReference> signature)
            : base(unit, name)
        {
            Unit = unit;
            SignatureReference = signature;
        }

        public override RoutineDefinition Resolve()
        {
            return this;
        }

        public abstract void Stage1RoutineStubs();
        public abstract void Stage2RoutineBody();

        public void VerifyCallArguments(IReadOnlyList<UnitDefinition> argumentTypes)
        {
            var parameters = SignatureDefinition.Parameters;

            // arity
            if (parameters.Count != argumentTypes.Count)
                throw new ArityMismatchException(this, argumentTypes.Count);

            // types
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                var arg = argumentTypes[i];
                if (!param.Type.IsAssignableFrom(arg))
                    throw new TypeMismatchException(param.Type, arg);
            }
        }
    }

    public class NativeRoutineDefinition : RoutineDefinition
    {
        public sealed override bool IsNative => true;

        private MethodReference methodReference;

        public NativeRoutineDefinition(
            UnitDefinition unit,
            Identifier name,
            ISignature<UnitReference> signature,
            MethodReference nativeMethod
        )
            : base(unit, name, signature)
        {
            methodReference = nativeMethod;
        }

        public override void Stage1RoutineStubs()
        {
            NativeMethod = methodReference.Resolve();
        }

        public override void Stage2RoutineBody()
        {
            // do nothing
        }
    }

    public class SLangRoutineDefinition : RoutineDefinition
    {
        public sealed override bool IsNative => false;
        private RoutineDeclaration AST { get; }

        public SLangRoutineDefinition(
            UnitDefinition unit,
            RoutineDeclaration routine
        )
            : base(unit, routine.Name, new SignatureReference(unit.Context, routine))
        {
            AST = routine;
            // explicitly tell unit to add routine
            unit.RegisterRoutine(this);
        }

        // some globals to share between code generation functionality
        private ILProcessor ip;

        public override void Stage1RoutineStubs()
        {
            SignatureDefinition = SignatureReference.Resolve();

            // name, attributes & return type
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Static;
            NativeMethod =
                new MethodDefinition(
                    Name.Value,
                    attributes,
                    SignatureDefinition.ReturnType.NativeType);

            // parameters types
            foreach (var param in SignatureDefinition.Parameters)
            {
                var nativeParam = new ParameterDefinition(param.Type.NativeType) {Name = param.Name.Value};
                NativeMethod.Parameters.Add(nativeParam);
            }
        }

        public override void Stage2RoutineBody()
        {
            if (NativeMethod == null)
                throw new CompilationStageException(Context, this, 2);

            ip = NativeMethod.Body.GetILProcessor();

            foreach (var entity in AST.Body)
            {
                switch (entity)
                {
                    case Call c:
                        GenerateStandaloneCall(c);
                        break;

                    case Return r:
                        GenerateReturn(r);
                        break;
                }
            }

            FixInitLocals();
            ip = null;
        }

        /// <summary>
        /// Generate "RETURN" expression.
        /// </summary>
        /// <para>Stack behavior:</para>
        /// <list type="number">
        /// <item><description>stack should be empty</description></item>
        /// <item><description>generate and load expression (if any)</description></item>
        /// <item><description>emit <c>Ret</c> opcode</description></item>
        /// </list>
        /// <param name="ret">"RETURN" AST fragment</param>
        /// <exception cref="TypeMismatchException">When actual returned type is not compatible with routine's signature</exception>
        private void GenerateReturn(Return ret)
        {
            var type = GenerateExpression(ret.OptionalValue);

            if (!SignatureDefinition.ReturnType.IsAssignableFrom(type))
                throw new TypeMismatchException(SignatureReference.ReturnType, type);

            ip.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Generate code which evaluates expression and stores its result into a local variable.
        /// </summary>
        /// <para>Stack behavior:</para>
        /// <list type="number">
        /// <item><description>recursively evaluate expression</description></item>
        /// <item><description>leave result on the stack</description></item>
        /// </list>
        /// <param name="expression">"EXPRESSION" AST fragment</param>
        /// <returns>Type of evaluated expression</returns>
        private UnitDefinition GenerateExpression(Expression expression)
        {
            switch (expression)
            {
                case null:
                    return Context.TypeSystem.Void;

                case Literal literal:
                    var unit = Context.ResolveBuiltIn(new UnitReference(Context, literal.Type));
                    unit.LoadFromLiteral(literal.Value, ip);
                    return unit;

                case Call call:
                    var routine = GenerateCall(call);
                    return routine.SignatureDefinition.ReturnType;

                case Reference reference:
                    return GenerateLoadReference(reference);

                // TODO: more expression classes
                default:
                    throw new NotImplementedException("Some expressions are not implemented");
            }
        }

        /// <summary>
        /// Generate code to load a value pointed by a reference
        /// </summary>
        /// <para>Stack behavior:</para>
        /// <list type="number">
        /// <item><description>generate code to load a value by the reference</description></item>
        /// <item><description>leave result on the stack</description></item>
        /// </list>
        /// <param name="reference">"REFERENCE" AST fragment</param>
        /// <returns>Type of loaded value</returns>
        /// <exception cref="UnresolvedReferenceException"></exception>
        private UnitDefinition GenerateLoadReference(Reference reference)
        {
            // TODO: Scope.Lookup(reference.Name);

            var index = SignatureDefinition.Parameters.FindIndex(p => p.Name.Equals(reference.Name));
            if (index != -1)
            {
                ip.Emit(OpCodes.Ldarg, index);
                var param = SignatureDefinition.Parameters[index];
                return param.Type;
            }

            throw new UnresolvedReferenceException(reference);
        }

        /// <summary>
        /// Generate call to a routine, leaving returned value on the stack.
        /// </summary>
        /// <para>Stack behavior: </para>
        /// <list type="number">
        /// <item><description>evaluate each routine argument in order from 1 to N</description></item>
        /// <item><description>arguments are on the stack one after another</description></item>
        /// <item><description>call routine</description></item>
        /// <item><description>leave returned value (if any) on the stack</description></item>
        /// </list>
        /// <para>Use returned routine definition to check whether the return type if void,
        /// in order to determine if there is a value left on the stack.</para>
        /// <param name="call">"CALL" AST fragment</param>
        /// <returns>Resolved routine definition</returns>
        private RoutineDefinition GenerateCall(Call call)
        {
            if (call.Callee.Unit != null)
                throw new NotImplementedException("Unit routines are not implemented yet.");

            var routine = new RoutineReference(Context, call.Callee).Resolve();

            // arguments:
            {
                // compile & leave on the stack
                var args = new List<UnitDefinition>(call.Arguments.Count);
                foreach (var expression in call.Arguments)
                {
                    args.Add(GenerateExpression(expression));
                }

                // verify
                routine.VerifyCallArguments(args);
            }

            ip.Emit(OpCodes.Call, routine.NativeMethod);

            return routine;
        }

        /// <summary>
        /// Generate call to a routine and store result into a variable.
        /// </summary>
        /// <para>Stack behavior: </para>
        /// <list type="number">
        /// <item><description>create new variable</description></item>
        /// <item><description>store returned value</description></item>
        /// <item><description>nothing left on the stack</description></item>
        /// </list>
        /// <param name="call">"CALL" AST fragment</param>
        /// <returns>SLang variable which holds a value returned from a call.</returns>
        private Variable GenerateVariableFromCall(Call call)
        {
            var routine = GenerateCall(call);
            if (!routine.SignatureReference.ReturnType.IsVoid)
            {
                var variable = new Variable(routine.SignatureDefinition.ReturnType);
                variable.Store(ip);
                return variable;
            }

            return new Variable(Context.TypeSystem.Void);
        }

        /// <summary>
        /// Generate call to a routine and drop result.
        /// </summary>
        /// <para>Stack behavior: </para>
        /// <list type="number">
        /// <item><description>pop returned value (if any)</description></item>
        /// <item><description>nothing left on the stack</description></item>
        /// </list>
        /// <param name="call">"CALL" AST fragment</param>
        private void GenerateStandaloneCall(Call call)
        {
            var routine = GenerateCall(call);
            if (!routine.SignatureReference.ReturnType.IsVoid)
            {
                // drop result
                // TODO: destructors?
                ip.Emit(OpCodes.Pop);
            }
        }

        /// <summary>
        /// Fix absurd error of Mono.Cecil not initializing locals automatically when they do exist.
        /// </summary>
        /// https://stackoverflow.com/questions/56819716/net-methoddefinition-body-initlocals-false
        private void FixInitLocals()
        {
            if (NativeMethod.Body.Variables.Count > 0)
                NativeMethod.Body.InitLocals = true;
        }
    }
}