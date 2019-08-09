using Mono.Cecil.Cil;
using SLang.IR;
using SLang.NET.Gen;

namespace SLang.NET.BuiltIns
{
    public class StringBuiltInUnitDefinition : BuiltInUnitDefinition
    {
        public static readonly Identifier UnitName = new Identifier("String");

        public StringBuiltInUnitDefinition(Context ctx)
            : base(ctx, UnitName, ctx.NativeModule.TypeSystem.String)
        {
        }

        public override bool CanLoadFromLiteral => true;

        public override void LoadFromLiteral(string literal, ILProcessor ip)
        {
            ip.Emit(OpCodes.Ldstr, literal);
        }
    }
}