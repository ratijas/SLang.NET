using System;
using System.Collections.Generic;
using Mono.Cecil;
using SLang.IR;
using SR = System.Reflection;

namespace SLang.NET.Gen
{
    public class Native
    {
        private static Dictionary<string, SR.MethodInfo> _methods = new Dictionary<string, SR.MethodInfo>
        {
            {"StandardIO$put$Integer", typeof(Console).GetMethod(nameof(Console.Write), new[] {typeof(int)})},
            {"StandardIO$put$String", typeof(Console).GetMethod(nameof(Console.Write), new[] {typeof(string)})},
        };

        public static SR.MethodInfo GetMethod(string name)
        {
            return _methods[name];
        }

        public static MethodReference ImportMethod(ModuleDefinition module, Identifier methodName)
        {
            return module.ImportReference(GetMethod(methodName.Value));
        }
    }
}