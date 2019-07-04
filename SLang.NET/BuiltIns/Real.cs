using System;
using System.Globalization;
using Mono.Cecil.Cil;
using SLang.IR;
using SLang.NET.Gen;

namespace SLang.NET.BuiltIns
{
    public class RealBuiltInUnitDefinition : BuiltInUnitDefinition
    {
        public static readonly Identifier UnitName = new Identifier("Real");

        public RealBuiltInUnitDefinition(Context ctx)
            : base(ctx, UnitName, ctx.NativeModule.TypeSystem.Double)
        {
        }

        public override void LoadFromLiteral(string literal, ILProcessor ip)
        {
            if (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var result))
                throw new FormatException($"Unable to parse real number literal: '{literal}'");

            ip.Emit(OpCodes.Ldc_R8, result);
        }
    }
}