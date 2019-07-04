using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;
using SLang.NET.Gen;

namespace SLang.NET.BuiltIns
{
    public class StringBuiltInUnitDefinition : BuiltInUnitDefinition
    {
        public StringBuiltInUnitDefinition(ModuleDefinition module)
            : base(new Identifier("String"), module.TypeSystem.String)
        {
        }

        public override void LoadFromLiteral(string literal, ILProcessor ip)
        {
            ip.Emit(OpCodes.Ldstr, literal);
        }
    }
}