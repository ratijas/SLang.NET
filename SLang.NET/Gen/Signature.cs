using System.Collections.Generic;
using System.Linq;
using SLang.IR;

namespace SLang.NET.Gen
{
    public abstract partial class Signature<T> where T : UnitReference
    {
        public T ReturnType { get; }

        public Parameter<T>[] Parameters { get; } = new Parameter<T>[0];


        /// <summary>
        /// No parameters, empty return type.
        /// </summary>
        /// <param name="ctx"></param>
        protected Signature(Context ctx) : this(ctx.TypeSystem.Void as T)
        {
        }

        /// <summary>
        /// No parameters, only the return type.
        /// </summary>
        /// <param name="returnType"></param>
        protected Signature(T returnType)
        {
            ReturnType = returnType;
        }

        /// <summary>
        /// With unnamed parameters and the return type.
        /// </summary>
        /// <param name="returnType"></param>
        /// <param name="parameters"></param>
        protected Signature(T returnType, IEnumerable<T> parameters)
        {
            ReturnType = returnType;
            Parameters = parameters.Select(p => new Parameter<T>(p)).ToArray();
        }

        /// <summary>
        /// With named parameters and the return type.
        /// </summary>
        /// <param name="returnType"></param>
        /// <param name="parameters"></param>
        protected Signature(T returnType, IEnumerable<Parameter<T>> parameters)
        {
            ReturnType = returnType;
            Parameters = parameters.ToArray();
        }


        public abstract SignatureDefinition Resolve();
    }

    public partial struct Parameter<T>
    {
        /// <summary>
        /// Parameters may have name, or it can be an empty identifier.
        /// </summary>
        public Identifier Name;

        public T Type;

        public Parameter(Identifier name, T type)
        {
            Name = name;
            Type = type;
        }

        public Parameter(T type) : this(Identifier.Empty, type)
        {
        }
    }

    /// <summary>
    /// Not yet resolved signature, operates on <see cref="UnitReference"/>s.
    /// Resolves to <see cref="SignatureDefinition"/>.
    /// </summary>
    public class SignatureReference : Signature<UnitReference>
    {
        public SignatureReference(Context ctx) : base(ctx)
        {
        }

        public SignatureReference(UnitReference returnType) : base(returnType)
        {
        }

        public SignatureReference(UnitReference returnType, IEnumerable<UnitReference> parameters)
            : base(returnType, parameters)
        {
        }

        public SignatureReference(UnitReference returnType, IEnumerable<Parameter<UnitReference>> parameters)
            : base(returnType, parameters)
        {
        }

        /// <summary>
        /// From JSON IR routine declaration.
        /// </summary>
        /// <para>Extracts named parameters and the return type.</para>
        /// <param name="ctx"></param>
        /// <param name="routine"></param>
        public SignatureReference(Context ctx, RoutineDeclaration routine)
            : base(
                new UnitReference(ctx, routine.ReturnType),
                routine
                    .Parameters
                    .Select(param =>
                        new Parameter<UnitReference>(
                            param.Name,
                            new UnitReference(ctx, param.Type))))
        {
        }

        public override SignatureDefinition Resolve()
        {
            return new SignatureDefinition(
                ReturnType.Resolve(),
                Parameters.Select(param => new Parameter<UnitDefinition>(param.Name, param.Type.Resolve())));
        }
    }

    /// <summary>
    /// Resolved signature, operates on <see cref="UnitDefinition"/>s.
    /// Resolves to itself.
    /// </summary>
    public class SignatureDefinition : Signature<UnitDefinition>
    {
        public SignatureDefinition(Context ctx) : base(ctx)
        {
        }

        public SignatureDefinition(UnitDefinition returnType) : base(returnType)
        {
        }

        public SignatureDefinition(UnitDefinition returnType, IEnumerable<UnitDefinition> parameters)
            : base(returnType, parameters)
        {
        }

        public SignatureDefinition(UnitDefinition returnType, IEnumerable<Parameter<UnitDefinition>> parameters)
            : base(returnType, parameters)
        {
        }

        public override SignatureDefinition Resolve()
        {
            return this;
        }
    }
}