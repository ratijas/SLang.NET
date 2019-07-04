using System;
using SLang.IR;
using Mono.Cecil;


namespace SLang.NET.Gen
{
    public static class Compiler
    {
        public static AssemblyDefinition CompileToIL(Compilation c, string name)
        {
            var nameDef = new AssemblyNameDefinition(name, new Version(1, 0, 0, 0));
            var asm = AssemblyDefinition.CreateAssembly(nameDef, "SLangMainModule", ModuleKind.Console);

            var context = new Context(asm.MainModule);

            var globalAnonymousUnit =
                context.GlobalUnit = new SLangUnitDefinition(context, new Identifier("SLang$GlobalUnit"));

            var globalAnonymousRoutine = new SLangRoutineDefinition(globalAnonymousUnit, c.Anonymous);
            
            context.Compile();

            asm.EntryPoint = globalAnonymousRoutine.NativeMethod;
            
            return asm;
        }
    }
}