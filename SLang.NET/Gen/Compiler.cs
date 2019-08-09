using System;
using Mono.Cecil;
using SLang.IR;

namespace SLang.NET.Gen
{
    public static class Compiler
    {
        public static AssemblyDefinition CompileToIL(Compilation root, string name)
        {
            var nameDef = new AssemblyNameDefinition(name, new Version(1, 0, 0, 0));
            var asm = AssemblyDefinition.CreateAssembly(nameDef, "SLangMainModule", ModuleKind.Console);

            var context = new Context(asm.MainModule);

            var globalUnit =
                context.GlobalUnit =
                    context.RegisterUnit(new SLangUnitDefinition(context, new Identifier("$GlobalUnit")));

            var globalAnonymousRoutine =
                globalUnit.RegisterRoutine(
                    new SLangRoutineDefinition(globalUnit, root.Anonymous)
                        {IsUnboxedReturnType = true});

            foreach (var declaration in root.Declarations)
            {
                switch (declaration)
                {
                    case RoutineDeclaration routine:
                        globalUnit.RegisterRoutine(new SLangRoutineDefinition(globalUnit, routine));
                        break;

                    case UnitDeclaration unit:
                        context.RegisterUnit(new SLangUnitDefinition(context, unit));
                        break;

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