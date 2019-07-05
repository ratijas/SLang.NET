using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;

namespace SLang.NET.Gen
{
    public class RoutineReference
    {
        public Identifier Name { get; protected set; }

        public UnitReference Unit { get; protected set; }

        public Context Context { get; }

        public RoutineReference(UnitReference unitReference, Identifier name)
        {
            Name = name;
            Unit = unitReference;
            Context = Unit.Context;
        }

        public virtual RoutineDefinition Resolve()
        {
            return Context.Resolve(this);
        }

        public override string ToString()
        {
            return $"{Unit}::{Name}";
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

    public abstract class RoutineDefinition : RoutineReference
    {
        public abstract bool IsNative { get; }
        public ISignature<UnitReference> SignatureReference { get; protected set; }
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

        public abstract void Compile();

        public override RoutineDefinition Resolve()
        {
            return this;
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

        public override void Compile()
        {
            NativeMethod = methodReference.Resolve();
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

        public override void Compile()
        {
            MakeMethodWithSignature();
            
            ip = NativeMethod.Body.GetILProcessor();

            foreach (var entity in AST.Body)
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

        private void MakeMethodWithSignature()
        {
            var signature = SignatureReference.Resolve();

            // name, attributes & return type
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Static;
            NativeMethod =
                new MethodDefinition(
                    Name.Value,
                    attributes,
                    signature.ReturnType.NativeType);

            // parameters types
            foreach (var (name, unit) in signature.Parameters)
            {
                var param = new ParameterDefinition(unit.NativeType) {Name = name.Value};
                NativeMethod.Parameters.Add(param);
            }
        }

        private void GenerateReturn(Return r)
        {
            if (r.OptionalValue != null)
            {
                var returnExpr = r.OptionalValue;
                var returnVar = GenerateExpression(returnExpr);

                // TODO: in future replace with subclass IsAssignableFrom check
                // XXX: AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
                if (!NativeMethod.ReturnType.FullName.Equals(returnVar.VariableType.FullName))
                {
                    throw new Exception(
                        $"Return type mismatch. Expected: {NativeMethod.ReturnType.FullName}, actual: {returnVar.VariableType.FullName}");
                }

                if (!NativeMethod.ReturnType.FullName.Equals(Context.TypeSystem.Void.NativeType.FullName))
                {
                    ip.Body.Variables.Add(returnVar);
                    ip.Emit(OpCodes.Ldloc, returnVar);
                }
                else
                {
                    Console.WriteLine($"Return type mismatch. Actual: {NativeMethod.ReturnType.FullName}, " +
                                      $"Void is: {Context.TypeSystem.Void.NativeType.FullName}");
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
            switch (expression)
            {
                case Literal literal:
                    var unit = Context.ResolveBuiltIn(new UnitReference(Context, literal.Type));
                    var result = new VariableDefinition(unit.NativeType);
                    unit.LoadFromLiteral(literal.Value, ip);
                    ip.Emit(OpCodes.Stloc, result);
                    return result;
                // TODO: more expression classes
                default:
                    throw new NotImplementedException("Some expressions are not implemented");
            }
        }

        private void FixInitLocals()
        {
            if (NativeMethod.Body.Variables.Count > 0)
                NativeMethod.Body.InitLocals = true;
        }
    }
}