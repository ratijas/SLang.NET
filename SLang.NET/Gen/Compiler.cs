using System;
using SLang.IR;
using Mono.Cecil;


namespace SLang.NET.Gen
{
    public static class Compiler
    {
        public static AssemblyDefinition CompileToIL(Compilation root, string name)
        {
            var nameDef = new AssemblyNameDefinition(name, new Version(1, 0, 0, 0));
            var asm = AssemblyDefinition.CreateAssembly(nameDef, "SLangMainModule", ModuleKind.Console);

            var context = new Context(asm.MainModule);

            var globalAnonymousUnit =
                context.GlobalUnit = new SLangUnitDefinition(context, new Identifier("SLang$GlobalUnit"));

            var globalAnonymousRoutine = new SLangRoutineDefinition(globalAnonymousUnit, root.Anonymous);

            foreach (var declaration in root.Declarations)
            {
                switch (declaration)
                {
                    case RoutineDeclaration routine:
                    {
                        var _ = new SLangRoutineDefinition(globalAnonymousUnit, routine);
                        break;
                    }

                    case UnitDeclaration unit:
                    {
                        var _ = new SLangUnitDefinition(context, unit);
                        break;
                    }

                    default:
                        throw new NotImplementedException("only routine declarations are supported");
                }
            }

            context.Compile();

            asm.EntryPoint = globalAnonymousRoutine.NativeMethod;

            return asm;
        }
    }
}