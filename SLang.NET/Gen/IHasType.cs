using System;

namespace SLang.NET.Gen
{
    public interface IHasType
    {
        UnitDefinition GetType();
    }

    public partial class UnitReference : IHasType
    {
        public new UnitDefinition GetType()
        {
            return Resolve();
        }
    }

    public abstract partial class Variable : IHasType
    {
        public new UnitDefinition GetType()
        {
            return Type;
        }
    }

    public partial struct Parameter<T> : IHasType
    {
        public new UnitDefinition GetType()
        {
            if (Type is UnitReference unit)
                return unit.Resolve();

            throw new ArgumentNullException($"Cannot convert {nameof(T)} to {nameof(UnitReference)}");
        }
    }
}