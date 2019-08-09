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

        public override void Stage1RoutineStubs()
        {
            var unitRef = new UnitRef(UnitName);

            var lhs = new Identifier("lhs");
            var rhs = new Identifier("rhs");

            RegisterRoutine(new SLangRoutineDefinition(this,
                new RoutineDeclaration(
                    new Identifier("operator+(Integer,Integer)"),
                    false,
                    new[]
                    {
                        new RoutineDeclaration.Parameter(unitRef, lhs),
                        new RoutineDeclaration.Parameter(unitRef, rhs),
                    },
                    unitRef,
                    new Entity[]
                    {
                        new Return(
                            new Call(Context.Intrinsics.GetCallee(new Identifier("add")), new[]
                            {
                                new Reference(lhs),
                                new Reference(rhs),
                            }))
                    }
                )));

            RegisterRoutine(new SLangRoutineDefinition(this,
                new RoutineDeclaration(
                    new Identifier("operator-(Integer,Integer)"),
                    false,
                    new[]
                    {
                        new RoutineDeclaration.Parameter(unitRef, lhs),
                        new RoutineDeclaration.Parameter(unitRef, rhs),
                    },
                    unitRef,
                    new Entity[]
                    {
                        new Return(
                            new Call(new Callee(Intrinsics.UnitName, new Identifier("sub")), new[]
                            {
                                new Reference(lhs),
                                new Reference(rhs),
                            }))
                    }
                )));

            base.Stage1RoutineStubs();
        }

        public override bool CanLoadFromLiteral => true;

        public override void LoadFromLiteral(string literal, ILProcessor ip)
        {
            if (!int.TryParse(literal, out var result))
                throw new LoadFromLiteralException(this, literal);

            ip.Emit(OpCodes.Ldc_I4, result);
        }
    }
}