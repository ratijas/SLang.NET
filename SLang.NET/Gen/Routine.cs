using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;


namespace SLang.NET.Gen
{
    public abstract class RoutineDefinition
    {
        public abstract bool IsNative { get; }
        public Identifier Name { get; protected set; }
        public Signature Signature { get; protected set; }
        public UnitDefinition Unit { get; protected set; }
        public ModuleDefinition NativeModule { get; protected set; }
        public MethodDefinition NativeMethod { get; protected set; }

        public abstract void Compile();
    }
    
    public class NativeRoutineDefinition : RoutineDefinition
    {
        public sealed override bool IsNative => true;

        public NativeRoutineDefinition(Identifier name, UnitDefinition unitDefinition, MethodReference methodReference)
        {
            Name = name;
            NativeMethod = methodReference.Resolve();
            Unit = unitDefinition;
            Signature = new Signature(methodReference);
            // TODO
        }

        public override void Compile()
        {
            // do nothing
        }
    }

    public class SLangRoutineDefinition : RoutineDefinition
    {
        public sealed override bool IsNative => false;

        public TypeDefinition NativeType => Unit.NativeType;
        public Routine Routine { get; }

        private TypeResolver resolver;

        public SLangRoutineDefinition(ModuleDefinition nativeModule, UnitDefinition unit, Routine routine)
        {
            NativeModule = nativeModule;
            Unit = unit;
            Name = routine.Name;
            Routine = routine;
            resolver = new TypeResolver(NativeModule);

            // Return type
            var returnTypeRef = new TypeResolver(NativeModule).ResolveType(routine.ReturnType);
            Signature = new Signature(returnTypeRef);

            NativeMethod =
                new MethodDefinition(
                    routine.Name.Value,
                    MethodAttributes.Public | MethodAttributes.Static,
                    returnTypeRef);

            // Arguments (parameters)
            foreach (Routine.Argument argument in routine.Arguments)
            {
                var argType = resolver.ResolveType(argument.Type);
                var param = new ParameterDefinition(argType) { Name = argument.Name.Value };
                NativeMethod.Parameters.Add(param);
                Signature.AddArgument(argType);
            }
        }

        // some globals to share between code generation functionality
        private ILProcessor ip;

        public override void Compile()
        {
            Clear();
            ip = NativeMethod.Body.GetILProcessor();
            
            foreach (var entity in Routine.Body)
            {
                switch (entity)
                {
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
            if (r.OptionalValue != null)
            {
                var returnExpr = r.OptionalValue;
                var returnVar = GenerateExpression(returnExpr);
                
                // TODO: in future replace with subclass IsAssignableFrom check
                
                // AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                if (!returnVar.VariableType.ToString().Equals(Signature.ReturnType.ToString()))
                {
                    throw new Exception($"Return type mismatch. Expected: {Signature.ReturnType}, actual: {returnVar.VariableType}");
                }

                if (returnVar.VariableType != NativeModule.TypeSystem.Void)
                {
                    ip.Body.Variables.Add(returnVar);
                    ip.Emit(OpCodes.Ldloc, returnVar);
                }
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
            TypeReference tyRef = resolver.ResolveType(expression);
            var result = new VariableDefinition(tyRef);

            switch (expression)
            {
                case Literal literal:
                    var literalType = BuiltInUnitDefinition.Get(NativeModule, literal.Type.Name);
                    literalType.LoadFromLiteral(literal.Value, ip);
                    ip.Emit(OpCodes.Stloc, result);
                    break;
                // TODO: more expression classes
                default:
                    throw new NotImplementedException("Some expressions are not implemented");
            }

            return result;
        }

        private void Clear()
        {
            var body = NativeMethod.Body;
            body.Variables.Clear();
            body.Instructions.Clear();
            body.ExceptionHandlers.Clear();
        }

        private void FixInitLocals()
        {
            if (NativeMethod.Body.Variables.Count > 0)
                NativeMethod.Body.InitLocals = true;
        }
    }
}