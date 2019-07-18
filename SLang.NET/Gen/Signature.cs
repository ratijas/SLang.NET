using System.Collections.Generic;
using System.Linq;
using SLang.IR;

namespace SLang.NET.Gen
{
    public interface ISignature<T>
    {
        T ReturnType { get; set; }

        /// <summary>
        /// Parameters may have name, or it can be an empty string.
        /// </summary>
        List<Parameter<T>> Parameters { get; }

        ISignature<UnitDefinition> Resolve();
    }

    public partial struct Parameter<T>
    {
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

    public class SignatureReference : ISignature<UnitReference>
    {
        public UnitReference ReturnType { get; set; }

        public List<Parameter<UnitReference>> Parameters { get; } =
            new List<Parameter<UnitReference>>();

        public SignatureReference(Context ctx) : this(ctx.TypeSystem.Void)
        {
        }

        public SignatureReference(UnitReference returnType)
        {
            ReturnType = returnType;
        }

        public SignatureReference(UnitReference returnType, IEnumerable<UnitReference> parameters)
        {
            ReturnType = returnType;
            Parameters.AddRange(parameters.Select(p => new Parameter<UnitReference>(p)));
        }

        public SignatureReference(
            UnitReference returnType,
            IEnumerable<Parameter<UnitReference>> parameters
        )
        {
            ReturnType = returnType;
            Parameters.AddRange(parameters);
        }

        public SignatureReference(Context ctx, RoutineDeclaration routine)
        {
            ReturnType = new UnitReference(ctx, routine.ReturnType);
            Parameters.AddRange(routine.Parameters.Select(param =>
                new Parameter<UnitReference>(param.Name, new UnitReference(ctx, param.Type))));
        }

        public ISignature<UnitDefinition> Resolve()
        {
            return new SignatureDefinition(
                ReturnType.Resolve(),
                Parameters.Select(param => new Parameter<UnitDefinition>(param.Name, param.Type.Resolve())));
        }
    }

    public class SignatureDefinition : ISignature<UnitDefinition>
    {
        public UnitDefinition ReturnType { get; set; }

        public List<Parameter<UnitDefinition>> Parameters { get; }
            = new List<Parameter<UnitDefinition>>();

        public SignatureDefinition(Context ctx) : this(ctx.TypeSystem.Void)
        {
        }

        public SignatureDefinition(UnitDefinition returnType)
        {
            ReturnType = returnType;
        }

        public SignatureDefinition(
            UnitDefinition returnType,
            IEnumerable<Parameter<UnitDefinition>> parameters
        )
            : this(returnType)
        {
            Parameters.AddRange(parameters);
        }

        public ISignature<UnitDefinition> Resolve()
        {
            return this;
        }
    }
}