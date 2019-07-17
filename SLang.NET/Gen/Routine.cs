using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                    // TODO: replace with some polymorphism
                    case Return r:
                        GenerateReturn(r);
                        break;
                }
            }

            FixInitLocals();
            ip = null;
        }

        private void GenerateReturn(Return r)
        {
            var expr = r?.OptionalValue;
            var exprVar = GenerateExpression(expr);
            var exprVarType = exprVar?.VariableType ?? Context.TypeSystem.Void.NativeType;
            
            if (!SignatureDefinition.ReturnType.NativeType.FullName.Equals(exprVarType.FullName))
                throw new TypeMismatchException(SignatureReference.ReturnType, exprVarType);

            if (SignatureDefinition.ReturnType.Equals(Context.TypeSystem.Void))
            {
                // void method returning with void routine.  do nothing.
            }
            else
            {
                Debug.Assert(exprVar != null);
                Debug.Assert(!exprVarType.FullName.Equals(Context.TypeSystem.Void.NativeType.FullName));

                ip.Body.Variables.Add(exprVar);
                ip.Emit(OpCodes.Ldloc, exprVar);
            }

            ip.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Generate code which evaluates expression and puts its result into new local variable definition.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns>Index </returns>
        private VariableDefinition GenerateExpression(Expression expression)
        {
            switch (expression)
            {
                case null:
                    return null;
                case Literal literal:
                    var unit = Context.ResolveBuiltIn(new UnitReference(Context, literal.Type));
                    var result = new VariableDefinition(unit.NativeType);
                    unit.LoadFromLiteral(literal.Value, ip);
                    ip.Emit(OpCodes.Stloc, result);
                    return result;
                case Call call:
                    return GenerateVariableFromCall(call);

                // TODO: more expression classes
                default:
                    throw new NotImplementedException("Some expressions are not implemented");
            }
        }

        /// <summary>
        /// Generates call to a routine, leaving returned value on the stack.
        /// </summary>
        /// <param name="call"></param>
        /// <returns>Resolved routine definition</returns>
        /// <exception cref="NotImplementedException"></exception>
        private RoutineDefinition GenerateCall(Call call)
        {
            if (call.Callee.Unit != null)
                throw new NotImplementedException("Unit routines are not implemented yet.");

            var routine = new RoutineReference(Context, call.Callee).Resolve();

            // arguments:
            {
                // compile
                var args = new List<VariableDefinition>(call.Arguments.Count);
                foreach (var expression in call.Arguments)
                {
                    args.Add(GenerateExpression(expression));
                }

                // verify
                VerifyCallArguments(routine, args);

                // add & load
                foreach (var arg in args)
                {
                    ip.Body.Variables.Add(arg);
                    ip.Emit(OpCodes.Ldloc, arg);
                }
            }

            ip.Emit(OpCodes.Call, routine.NativeMethod);

            return routine;
        }

        private VariableDefinition GenerateVariableFromCall(Call call)
        {
            var routine = GenerateCall(call);
            if (!routine.SignatureReference.ReturnType.Equals(Context.TypeSystem.Void))
            {
                var variable = new VariableDefinition(routine.SignatureDefinition.ReturnType.NativeType);
                ip.Emit(OpCodes.Stloc, variable);
                return variable;
            }

            return null;
        }

        private void GenerateStandaloneCall(Call call)
        {
            var routine = GenerateCall(call);
            if (!routine.SignatureReference.ReturnType.Equals(Context.TypeSystem.Void))
            {
                // drop result
                // TODO: destructors?
                ip.Emit(OpCodes.Pop);
            }
        }

        private static void VerifyCallArguments(RoutineDefinition routine,
            List<VariableDefinition> arguments)
        {
            var signature = routine.SignatureDefinition;
            var parameters = signature.Parameters;

            // arity
            if (parameters.Count != arguments.Count)
                throw new ArityMismatchException(routine, arguments.Count);

            // types
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                var arg = arguments[i];
                // TODO: types equality / IsAssignableFrom
                if (!param.Type.NativeType.FullName.Equals(arg.VariableType.Resolve().FullName))
                    throw new TypeMismatchException(param.Type, arg.VariableType);
            }
        }

        private void FixInitLocals()
        {
            if (NativeMethod.Body.Variables.Count > 0)
                NativeMethod.Body.InitLocals = true;
        }
    }
}