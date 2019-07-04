using System;
using Mono.Cecil.Cil;
using SLang.IR;
using SLang.NET.Gen;

namespace SLang.NET.BuiltIns
{
    public class IntegerBuiltInUnitDefinition : BuiltInUnitDefinition
    {
        public static readonly Identifier UnitName = new Identifier("Integer");

        public IntegerBuiltInUnitDefinition(Context ctx)
            : base(ctx, UnitName, ctx.NativeModule.TypeSystem.Int32)
        {
        }

        public override void LoadFromLiteral(string literal, ILProcessor ip)
        {
            if (!int.TryParse(literal, out var result))
                throw new FormatException($"Unable to parse integer literal: '{literal}'");

            ip.Emit(OpCodes.Ldc_I4, result);
        }
    }
}