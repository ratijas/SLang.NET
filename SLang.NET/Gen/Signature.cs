using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using SLang.IR;

namespace SLang.NET.Gen
{
    public class Signature
    {
        public TypeReference ReturnType { get; set; }
        public List<TypeReference> Arguments { get; } = new List<TypeReference>();

        public Signature(ModuleDefinition module) : this(module.TypeSystem.Void)
        {
        }
        
        public Signature(TypeReference returnType)
        {
            ReturnType = returnType;
        }

        public Signature(IMethodSignature methodReference)
        {
            ReturnType = methodReference.ReturnType;
            Arguments.AddRange(methodReference.Parameters.Select(p => p.ParameterType));
        }

        public void AddArgument(TypeReference nativeType)
        {
            Arguments.Add(nativeType);
        }
    }
}