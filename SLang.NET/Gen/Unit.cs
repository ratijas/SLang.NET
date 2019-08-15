using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;

namespace SLang.NET.Gen
{
    public partial class UnitReference
    {
        public Identifier Name { get; }
        public Context Context { get; }

        public UnitReference(Context ctx, Identifier name)
        {
            Name = name;
            Context = ctx;
        }

        public UnitReference(Context ctx, UnitRef unitRef) : this(ctx, unitRef.Name)
        {
        }

        public virtual UnitDefinition Resolve()
        {
            return Context.Resolve(this);
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        /// <summary>
        /// Shortcut for <c>Equals(Context.TypeSystem.Void)</c>.
        /// </summary>
        public bool IsVoid => Equals(Context.TypeSystem.Void);
    }

    public abstract class UnitDefinition : UnitReference, IStagedCompilation
    {
        /// <summary>
        /// Native underlying type, like .NET class or struct.
        /// </summary>
        // TODO: optimize ImportReference. this should not be done every time NativeType is accessed.
        public TypeReference NativeType => Context.NativeModule.ImportReference(NativeTypeDefinition);

        /// <summary>
        /// Native Underlying type definition.
        /// </summary>
        public TypeDefinition NativeTypeDefinition { get; protected set; }

        public bool IsForeign { get; protected set; } = false;

        public List<RoutineDefinition> Routines { get; } = new List<RoutineDefinition>();

        protected UnitDefinition(Context ctx, Identifier name) : base(ctx, name)
        {
        }

        public override UnitDefinition Resolve()
        {
            return this;
        }

        public RoutineDefinition Resolve(RoutineReference routineReference)
        {
            try
            {
                return Routines.Single(routine => routine == routineReference);
            }
            catch (InvalidOperationException)
            {
                throw new RoutineNotFoundException(Context, routineReference);
            }
        }

        /// <summary>
        /// Add routine and its native underlying method definition.
        /// </summary>
        /// <param name="routine">Routine definition</param>
        internal T RegisterRoutine<T>(T routine) where T : RoutineDefinition
        {
            // high level
            Routines.Add(routine);
            // low level, should be:
            // NativeTypeDefinition.Methods.Add(routine.NativeMethod);
            // but NativeMethod is null until Stage1RoutineStubs().

            return routine;
        }

        public bool IsAssignableFrom(IHasType other)
        {
            return Equals(other.GetType());
        }

        public bool IsAssignableTo(IHasType other)
        {
            return other.GetType().IsAssignableFrom(this);
        }

        public void AssertIsAssignableFrom(IHasType other)
        {
            if (!IsAssignableFrom(other))
                throw new TypeMismatchException(this, other);
        }

        public void AssertIsAssignableTo(IHasType other)
        {
            other.GetType().AssertIsAssignableFrom(this);
        }

        public virtual void Stage1RoutineStubs()
        {
            foreach (var routine in Routines)
            {
                routine.Stage1RoutineStubs();
                if (!IsForeign)
                {
                    NativeTypeDefinition.Methods.Add(routine.NativeMethod);
                }
            }
        }

        public virtual void Stage2RoutineBody()
        {
            foreach (var routine in Routines)
            {
                routine.Stage2RoutineBody();
            }
        }

        public virtual bool CanLoadFromLiteral => false;

        public virtual void LoadFromLiteral(string literal, ILProcessor ip)
        {
            throw new NotImplementedException($"{this} type does not support literal values");
        }
    }

    /// <summary>
    /// Special primitive types which wrap around another native platform type, usually using struct.
    /// </summary>
    public abstract class BuiltInUnitDefinition : UnitDefinition
    {
        public const string SLangBuiltInUnitDotNETNamespace = "SLang.NET";
        public const string ValueFieldName = "value";

        public TypeReference WrappedNativeType { get; }
        public FieldDefinition ValueField { get; }
        public MethodDefinition Ctor { get; set; }

        /// <summary>
        /// Indirect proxy to the underlying wrapped type. Contains single <see cref="ValueField"/> of <see cref="WrappedNativeType"/>.
        /// </summary>
        protected BuiltInUnitDefinition(Context ctx, Identifier name, TypeReference wrappedType) : base(ctx, name)
        {
            var module = Context.NativeModule;

            // public struct SLang.NET$TWrapper
            // {
            //     private T value;
            // }

            // 1. underlying wrapped type
            WrappedNativeType = module.ImportReference(wrappedType);

            // 2. exposed public type
            const TypeAttributes typeAttributes =
                TypeAttributes.Public |
                TypeAttributes.Sealed |
                TypeAttributes.SequentialLayout |
                TypeAttributes.Class;

            TypeReference baseType = module.ImportReference(
                new TypeReference(
                    module.TypeSystem.Object.Namespace,
                    nameof(ValueType),
                    null,
                    module.TypeSystem.CoreLibrary));

            NativeTypeDefinition = new TypeDefinition(
                SLangBuiltInUnitDotNETNamespace,
                Name.Value,
                typeAttributes,
                baseType
            );

            // 2.1 the only field, containing underlying wrapped value
            const FieldAttributes fieldAttributes =
                FieldAttributes.Public;

            ValueField = new FieldDefinition(ValueFieldName, fieldAttributes, WrappedNativeType);
            NativeTypeDefinition.Fields.Add(ValueField);
        }

        /// <summary>
        /// Direct proxy to the underlying type.
        /// <see cref="ValueField"/> remains null, and <see cref="WrappedNativeType"/> points to the same type as
        /// <see cref="UnitDefinition.NativeType"/>.
        /// <param name="wrappedType">Type reference must be imported (if needed) before calling this constructor.
        /// <c>NativeModule.TypeSystem.*</c> types like Void must NOT be explicitly imported.</param>
        /// </summary>
        protected BuiltInUnitDefinition(Context ctx, TypeReference wrappedType)
            : base(ctx, new Identifier(wrappedType.Name))
        {
            WrappedNativeType = wrappedType;
            NativeTypeDefinition = WrappedNativeType.Resolve();
            IsForeign = true;
        }

        private static Identifier CtorIdentifier = new Identifier(".ctor");

        public override void Stage1RoutineStubs()
        {
            // struct constructor
            // public SLang.NET$TWrapper(T value)
            // {
            //     this.value = value;
            // }
            if (!IsForeign)
            {
                const MethodAttributes attributes =
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName;

                Ctor = new MethodDefinition(
                    CtorIdentifier.Value,
                    attributes,
                    Context.NativeModule.ImportReference(Context.NativeModule.TypeSystem.Void)
                );
                Ctor.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, WrappedNativeType));

                NativeTypeDefinition.Methods.Add(Ctor);
            }

            base.Stage1RoutineStubs();
        }

        public override void Stage2RoutineBody()
        {
            // constructor
            if (!IsForeign)
            {
                var ip = Ctor.Body.GetILProcessor();

                ip.Emit(OpCodes.Ldarg_0);
                ip.Emit(OpCodes.Ldarg_1);
                ip.Emit(OpCodes.Stfld, ValueField);
                ip.Emit(OpCodes.Ret);
            }

            base.Stage2RoutineBody();
        }

        /// <summary>
        /// Given that instance of the underlying type is on the top of the stack, wrap it into new object of this type.
        /// </summary>
        /// <para>Stack behavior:</para>
        /// <list type="number">
        /// <item><description>raw (underlying) value is on the top of the stack</description></item>
        /// <item><description>instantiate new object wrapping that value</description></item>
        /// <item><description>new object is stored is a variable</description></item>
        /// <item><description>stack is empty: neither raw nor boxed value</description></item>
        /// </list>
        /// <returns>An instance of the <see cref="Variable"/> is the only way to access the created boxed object.</returns>
        public Variable Boxed(ILProcessor ip)
        {
            if (!IsForeign)
            {
                var boxed = new BodyVariable(this);
                var raw = new VariableDefinition(WrappedNativeType);

                ip.Body.Variables.Add(raw);
                ip.Body.InitLocals = true;

                ip.Emit(OpCodes.Stloc, raw);
                boxed.LoadA(ip);
                ip.Emit(OpCodes.Ldloc, raw);
                ip.Emit(OpCodes.Call, Ctor);

                return boxed;
            }

            return new BodyVariable(Context.TypeSystem.Void);
        }

        /// <summary>
        /// Given that instance of this type is on the top of the stack, unwrap it into an object of the underlying type.
        /// </summary>
        /// <para>Stack behavior:</para>
        /// <list type="number">
        /// <item><description>instance of this type is on the top of the stack</description></item>
        /// <item><description>unwrap object, extracting raw value</description></item>
        /// <item><description>raw value is left on the stack</description></item>
        /// </list>
        public void Unboxed(ILProcessor ip)
        {
            if (!IsForeign)
            {
                ip.Emit(OpCodes.Ldfld, ValueField);
            }
        }
    }

    public class SLangUnitDefinition : UnitDefinition
    {
        public const string SLangUnitDotNETNamespace = "SLang";

        public SLangUnitDefinition(Context ctx, Identifier name) : base(ctx, name)
        {
            // public class SLang$My.Unit
            // {
            // }

            const TypeAttributes typeAttributes =
                TypeAttributes.Public |
                TypeAttributes.Class;

            TypeReference baseType = Context.NativeModule.ImportReference(Context.NativeModule.TypeSystem.Object);

            NativeTypeDefinition = new TypeDefinition(
                SLangUnitDotNETNamespace,
                Name.Value,
                typeAttributes,
                baseType
            );
        }

        public SLangUnitDefinition(Context ctx, UnitDeclaration ir) : this(ctx, ir.Name)
        {
            foreach (var declaration in ir.Declarations)
            {
                switch (declaration)
                {
                    case RoutineDeclaration routine:
                        RegisterRoutine(new SLangRoutineDefinition(this, routine));
                        break;

                    default:
                        throw new NotImplementedException("only routines declarations are supported within units");
                }
            }
        }
    }
}