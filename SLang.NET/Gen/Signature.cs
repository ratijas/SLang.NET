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
        List<(Identifier Name, T Type)> Parameters { get; }

        ISignature<UnitDefinition> Resolve();
    }

    public class SignatureReference : ISignature<UnitReference>
    {
        public UnitReference ReturnType { get; set; }

        public List<(Identifier Name, UnitReference Type)> Parameters { get; } =
            new List<(Identifier Name, UnitReference Type)>();

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
            Parameters.AddRange(parameters.Select(p => (new Identifier(string.Empty), p)));
        }

        public SignatureReference(
            UnitReference returnType,
            IEnumerable<(Identifier Name, UnitReference Type)> parameters
        )
        {
            ReturnType = returnType;
            Parameters.AddRange(parameters);
        }

        public SignatureReference(Context ctx, RoutineDeclaration routine)
        {
            ReturnType = new UnitReference(ctx, routine.ReturnType);
            Parameters.AddRange(routine.Arguments.Select(argument =>
                (argument.Name, new UnitReference(ctx, argument.Type))));
        }

        public ISignature<UnitDefinition> Resolve()
        {
            var sig = new SignatureDefinition(
                ReturnType.Resolve(),
                Parameters.Select(argument => (argument.Name, argument.Type.Resolve())));
            return sig;
        }
    }

    public class SignatureDefinition : ISignature<UnitDefinition>
    {
        public UnitDefinition ReturnType { get; set; }

        public List<(Identifier Name, UnitDefinition Type)> Parameters { get; }
            = new List<(Identifier Name, UnitDefinition Type)>();

        public SignatureDefinition(Context ctx) : this(ctx.TypeSystem.Void)
        {
        }

        public SignatureDefinition(UnitDefinition returnType)
        {
            ReturnType = returnType;
        }

        public SignatureDefinition(
            UnitDefinition returnType,
            IEnumerable<(Identifier Name, UnitDefinition Type)> arguments
        )
            : this(returnType)
        {
            Parameters.AddRange(arguments);
        }

        public ISignature<UnitDefinition> Resolve()
        {
            return this;
        }
    }
}