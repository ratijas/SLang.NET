using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static MoreLinq.Extensions.ForEachExtension;
using SLang.IR;

namespace SLang.NET.Gen
{
    public partial class RoutineReference
    {
        public Identifier Name { get; }

        /// <summary>
        /// Unit reference applies to routines declared within SLang Units.
        /// Global routines do not have explicit unit binding -- instead they are defined within
        /// special "$Runtime" unit, and only black magic of Context shall be able to resolve those.
        ///
        /// With that being said, UnitReference may be null, but after Resolve() UnitDefinition is always set.
        /// </summary>
        public UnitReference Unit { get; }

        public Context Context { get; }

        /// <summary>
        /// Global routines are always static. Routines declared within units must be marked as static explicitly.
        /// Default value `true` is provided as a temporary measure until IR supports such modifier.
        /// </summary>
        public bool IsStatic { get; } = true;

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
    }

    public abstract class RoutineDefinition : RoutineReference, IStagedCompilation
    {
        public abstract bool IsNative { get; }
        public SignatureReference SignatureReference { get; protected set; }
        public SignatureDefinition SignatureDefinition { get; protected set; }
        public new UnitDefinition Unit { get; protected set; }

        /// <summary>
        /// Underlying native method. This property is null until Stage1RoutineStubs() is completed.
        /// </summary>
        public MethodDefinition NativeMethod { get; protected set; }

        protected RoutineDefinition(UnitDefinition unit, Identifier name, SignatureReference signature)
            : base(unit, name)
        {
            Unit = unit;
            SignatureReference = signature;
        }

        public override RoutineDefinition Resolve()
        {
            return this;
        }

        public virtual void Stage1RoutineStubs()
        {
            SignatureDefinition = SignatureReference.Resolve();
        }

        public abstract void Stage2RoutineBody();

        public void VerifyCallArguments(IReadOnlyList<UnitDefinition> argumentTypes)
        {
            var parameters = SignatureDefinition.Parameters;

            // arity
            if (parameters.Length != argumentTypes.Count)
                throw new ArityMismatchException(this, argumentTypes.Count);

            // types
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var arg = argumentTypes[i];
                arg.AssertIsAssignableTo(param.Type);
            }
        }
    }

    public class NativeRoutineDefinition : RoutineDefinition
    {
        public sealed override bool IsNative => true;

        public NativeRoutineDefinition(
            UnitDefinition unit,
            Identifier name,
            SignatureReference signature,
            MethodDefinition nativeMethod
        )
            : base(unit, name, signature)
        {
            NativeMethod = nativeMethod;
        }

        public override void Stage2RoutineBody()
        {
            // do nothing
        }
    }

    public class SLangRoutineDefinition : RoutineDefinition
    {
        public sealed override bool IsNative => false;
        public bool IsUnboxedReturnType = false;

        private RoutineDeclaration AST { get; }

        public SLangRoutineDefinition(
            UnitDefinition unit,
            RoutineDeclaration routine
        )
            : base(unit, routine.Name, new SignatureReference(unit.Context, routine))
        {
            AST = routine;
        }

        // some globals to share between code generation functionality
        private ILProcessor ip;
        private Scope scopeRoot;
        private Scope scopeCurrent;

        public override void Stage1RoutineStubs()
        {
            base.Stage1RoutineStubs();

            // name, attributes & return type
            const MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Static;
            var returnType =
                (IsUnboxedReturnType && SignatureDefinition.ReturnType is BuiltInUnitDefinition builtInType)
                    ? builtInType.WrappedNativeType
                    : SignatureDefinition.ReturnType.NativeType;
            NativeMethod =
                new MethodDefinition(
                    Name.Value,
                    attributes,
                    returnType);

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
            // TODO: parent global scope
            scopeCurrent = scopeRoot = new Scope();
            // function arguments are in the root scope
            SignatureDefinition.Parameters.ForEach((parameter, index) =>
            {
                var argument = new ArgumentVariable(parameter.Type, parameter.Name, index);
                scopeRoot.Declare(parameter.Name, argument);
            });

            GenerateEntityList(AST.Body);

            FixInitLocals();
            ip = null;
            scopeCurrent = scopeRoot = null;
        }

        /// <summary>
        /// Generate block of entities, like routine body or if-then-else branches.
        /// </summary>
        /// <para>Stack behavior: entirely defined by entities in question.</para>
        /// <param name="entities">"ENTITY_LIST" AST fragment</param>
        private void GenerateEntityList(List<Entity> entities)
        {
            scopeCurrent = new Scope(scopeCurrent);

            foreach (var entity in entities)
            {
                switch (entity)
                {
                    case Call c:
                        GenerateStandaloneCall(c);
                        break;

                    case Return r:
                        GenerateReturn(r);
                        break;

                    case If conditionals:
                        GenerateConditionalStatements(conditionals);
                        break;

                    case VariableDeclaration declaration:
                        GenerateVariableDeclaration(declaration);
                        break;

                    case Assignment assignment:
                        GenerateAssignment(assignment);
                        break;

                    default:
                        throw new NotImplementedException("Entity type is not implemented: " + entity.GetType());
                }
            }

            scopeCurrent = scopeCurrent.ParentScope();
        }

        /// <summary>
        /// Generate "VARIABLE" declaration, evaluate its initializer and assign. 
        /// </summary>
        /// <para>Stack behavior: expects nothing, leaves nothing.</para>
        /// <param name="declaration">SLang IR variable declaration.</param>
        private void GenerateVariableDeclaration(VariableDeclaration declaration)
        {
            var name = declaration.Name;
            var varType = new UnitReference(Context, declaration.Type).Resolve();
            Variable variable = new BodyVariable(varType, name);

            // declare in the current scope
            scopeCurrent.Declare(name, variable);
            // evaluate initializer
            var exprType = GenerateExpression(declaration.Initializer);
            // assign
            variable.Store(ip);

            // verify
            varType.AssertIsAssignableFrom(exprType);
        }

        /// <summary>
        /// Generate "ASSIGNMENT" statement, evaluate lvalue, rvalue, and assign. 
        /// </summary>
        /// <para>Stack behavior: expects nothing, leaves nothing.</para>
        /// <param name="assignment">SLang IR assignment statement.</param>
        private void GenerateAssignment(Assignment assignment)
        {
            var rvalueType = GenerateExpression(assignment.RValue);

            switch (assignment.LValue)
            {
                case Reference reference:
                    var lvalue = scopeCurrent.Get(reference.Name);
                    lvalue.GetType().AssertIsAssignableFrom(rvalueType);
                    lvalue.Store(ip);
                    break;

                default:
                    throw new NotImplementedException(
                        $"only references are supported as lvalues, got: {assignment.LValue}");
            }
        }

        /// <summary>
        /// Generate "IF" statement with all "than", "elseif" and "else" branches.
        /// </summary>
        /// <para>Stack behavior: entirely defined by then/else branches.</para>
        /// <param name="conditionals">"IF" and "STMT_IF_THEN_LIST" with "STMT_IF_THEN" AST fragments</param>
        /// <exception cref="EmptyConditionalsException">"If" has no actual "if"/"then" pair</exception>
        /// <exception cref="TypeMismatchException">When type of condition is not an Integer unit type.</exception>
        private void GenerateConditionalStatements(If conditionals)
        {
            if (conditionals.IfThen.Count == 0)
                throw new EmptyConditionalsException(conditionals);

            var N = conditionals.IfThen.Count;
            var brStubs = new Instruction[N];
            var hasElse = conditionals.Else != null;
            var afterLabel = ip.Create(OpCodes.Nop);

            for (var i = 0; i < N; i++)
            {
                var (condition, body) = conditionals.IfThen[i];

                var brStub = ip.Create(OpCodes.Nop);
                brStubs[i] = brStub;

                if (i != 0)
                {
                    var here = ip.Create(OpCodes.Nop);
                    ip.Append(here);
                    ip.Replace(brStubs[i - 1], ip.Create(OpCodes.Brfalse, here));
                }

                var type = GenerateExpression(condition);
                type.AssertIsAssignableTo(Context.TypeSystem.Integer);

                if (type is BuiltInUnitDefinition builtInType)
                    builtInType.Unboxed(ip);

                ip.Append(brStub);

                GenerateEntityList(body);
                ip.Emit(OpCodes.Br, afterLabel);
            }

            if (hasElse)
            {
                var here = ip.Create(OpCodes.Nop);
                ip.Append(here);
                ip.Replace(brStubs[N - 1], ip.Create(OpCodes.Brfalse, here));
                GenerateEntityList(conditionals.Else);
            }

            ip.Append(afterLabel);
        }

        /// <summary>
        /// Generate "RETURN" statement.
        /// </summary>
        /// <para>
        /// Routines with <see cref="IsUnboxedReturnType"/> flag set:
        /// signature's return type is rewritten into unboxed primitive type.
        /// </para>
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

            if (IsUnboxedReturnType && type is BuiltInUnitDefinition builtInType)
            {
                builtInType.Unboxed(ip);
            }

            SignatureDefinition.ReturnType.AssertIsAssignableFrom(type);

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
                    var unit = new UnitReference(Context, literal.Type).Resolve();

                    if (!unit.CanLoadFromLiteral)
                        throw new LiteralsNotSupported(unit, literal.Value);

                    if (unit is BuiltInUnitDefinition u)
                    {
                        var storage = new VariableDefinition(u.NativeType);
                        ip.Body.Variables.Add(storage);
                        ip.Emit(OpCodes.Ldloca, storage);
                        u.LoadFromLiteral(literal.Value, ip);
                        ip.Emit(OpCodes.Call, u.Ctor);
                        ip.Emit(OpCodes.Ldloc, storage);
                    }

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

            var variable = scopeCurrent.Get(reference.Name);
            variable.Load(ip);
            return variable.GetType();
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
            var routine = new RoutineReference(Context, call.Callee).Resolve();

            if (!routine.IsStatic)
            {
                throw new NotImplementedException("Non-static unit routines are not implemented yet.");
            }

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
                var variable = new BodyVariable(routine.SignatureDefinition.ReturnType);
                variable.Store(ip);
                return variable;
            }

            return new BodyVariable(Context.TypeSystem.Void);
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