using System;
using Mono.Cecil;
using SLang.IR;
using SLang.NET.Gen;

namespace SLang.NET.Gen
{
    public interface ITypeResolver
    {
        TypeReference ResolveType(ModuleReference module);
    }

    public class TypeResolver
    {
        public ModuleDefinition NativeModule { get; }

        public TypeResolver(ModuleDefinition nativeModule)
        {
            NativeModule = nativeModule;
        }

        public TypeReference ResolveType(UnitRef unitRef)
        {
            try
            {
                // TODO: explicit testing mechanism
                return BuiltInUnitDefinition.Get(NativeModule, unitRef.Name).NativeType;
            }
            catch (NotImplementedException e)
            {
                throw new NotImplementedException("User defined units are not implemented yet", e);
            }
        }
        public TypeReference ResolveType(Literal literal)
        {
            return ResolveType(literal.Type);
        }
        
        public TypeReference ResolveType(Expression expression)
        {
            switch (expression)
            {
                case null:
                    return ResolveType(UnitRef.Void);
                case Literal l:
                    return ResolveType(l);
                default:
                    throw new NotImplementedException("Expression type not supported");
            }
        }
    }
}