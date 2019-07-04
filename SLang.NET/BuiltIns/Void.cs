using System;
using Mono.Cecil.Cil;
using SLang.IR;
using SLang.NET.Gen;

namespace SLang.NET.BuiltIns
{
    public class VoidBuiltInUnitDefinition : BuiltInUnitDefinition
    {
        public static readonly Identifier UnitName = UnitRef.Void.Name;

        public VoidBuiltInUnitDefinition(Context ctx)
            : base(ctx, UnitName, ctx.NativeModule.TypeSystem.Void)
        {
        }

        public override void LoadFromLiteral(string literal, ILProcessor ip)
        {
            throw new NotImplementedException("Impossible to load Void literal. This call should not have happened");
        }
    }
}