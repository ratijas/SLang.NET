using System;
using System.Collections.Generic;
using System.Linq;

namespace SLang.NET.Gen
{
    public partial class UnitReference : IEquatable<UnitReference>
    {
        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public bool Equals(UnitReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is UnitReference other && Equals(other);
        }

        public static bool operator ==(UnitReference left, UnitReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UnitReference left, UnitReference right)
        {
            return !Equals(left, right);
        }
    }

    public partial class RoutineReference : IEquatable<RoutineReference>
    {
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is RoutineReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Equals(RoutineReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Name, other.Name) && Equals(Unit, other.Unit);
        }

        public static bool operator ==(RoutineReference left, RoutineReference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RoutineReference left, RoutineReference right)
        {
            return !Equals(left, right);
        }
    }

    public abstract partial class Signature<T> : IEquatable<Signature<T>>
    {
        public bool Equals(Signature<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(ReturnType, other.ReturnType) &&
                   Parameters.SequenceEqual(other.Parameters);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is Signature<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(ReturnType) * 397) ^ Parameters.GetHashCode();
            }
        }

        public static bool operator ==(Signature<T> left, Signature<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Signature<T> left, Signature<T> right)
        {
            return !Equals(left, right);
        }
    }
}