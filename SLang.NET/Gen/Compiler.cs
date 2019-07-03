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
            
            var unit = new SLangUnitDefinition(asm.MainModule, new Identifier("SLang$Main"));

            c.Anonymous.Name = new Identifier("$Main");
            var anonymousRoutine = unit.DefineRoutine(c.Anonymous);

            unit.Compile();
            
            asm.EntryPoint = anonymousRoutine.NativeMethod;

            asm.MainModule.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new [] {typeof(int)}));
            
            return asm;
        }
    }
}