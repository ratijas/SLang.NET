using SLang.IR;
using SLang.NET.Gen;

namespace SLang.NET.BuiltIns
{
    public class VoidBuiltInUnitDefinition : BuiltInUnitDefinition
    {
        public static readonly Identifier UnitName = UnitRef.Void.Name;

        public VoidBuiltInUnitDefinition(Context ctx)
            : base(ctx, ctx.NativeModule.TypeSystem.Void)
        {
        }
    }
}