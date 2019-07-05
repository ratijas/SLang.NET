using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;
using MoreLinq;

namespace SLang.NET.Gen
{
    public class UnitReference
    {
        public Identifier Name { get; }
        public Context Context { get; }

        public UnitReference(Context ctx, Identifier name)
        {
            Name = name;
            Context = ctx;
        }

        public UnitReference(Context ctx, UnitRef unitRef) : this(ctx, unitRef.Name)
        {
        }

        public virtual UnitDefinition Resolve()
        {
            return Context.Resolve(this);
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is UnitReference other)
            {
                return Name.Equals(other.Name);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

    public abstract class UnitDefinition : UnitReference
    {
        public abstract bool IsNative { get; }
        public TypeReference NativeType { get; protected set; }
        public List<RoutineDefinition> Routines { get; } = new List<RoutineDefinition>();

        protected UnitDefinition(Context ctx, Identifier name) : base(ctx, name)
        {
        }

        public override UnitDefinition Resolve()
        {
            return this;
        }

        public RoutineDefinition Resolve(RoutineReference routineReference)
        {
            try
            {
                return Routines.Single(routine => routine.Name.Equals(routineReference.Name));
            }
            catch (InvalidOperationException)
            {
                throw new RoutineNotFoundException(Context, routineReference);
            }
        }
        
        /// <summary>
        /// Add routine and its native underlying method definition.
        /// </summary>
        /// <param name="routine">Routine definition</param>
        internal void RegisterRoutine(RoutineDefinition routine)
        {
            Routines.Add(routine);
        }

        public virtual void Compile()
        {
        }
    }

    public abstract class BuiltInUnitDefinition : UnitDefinition
    {
        public sealed override bool IsNative => true;
        
        protected BuiltInUnitDefinition(Context ctx, Identifier name, TypeReference underlyingType) : base(ctx, name)
        {
            NativeType = ctx.NativeModule.ImportReference(underlyingType);
            // no need to register unit within context, because context managed to do it itself
        }

        public abstract void LoadFromLiteral(string literal, ILProcessor ip);
    }

    public class SLangUnitDefinition : UnitDefinition
    {
        public sealed override bool IsNative => false;

        public const string SLangUnitDotNETNamespace = "SLang";

        public TypeDefinition NativeTypeDefinition { get; protected set; }

        public SLangUnitDefinition(Context ctx, Identifier name) : base(ctx, name)
        {
            // low level
            NativeTypeDefinition = new TypeDefinition(SLangUnitDotNETNamespace, Name.Value,
                TypeAttributes.Public | TypeAttributes.Class,
                Context.NativeModule.TypeSystem.Object);
            NativeType = NativeTypeDefinition;
            
            // need to explicitly tell context to register unit
            Context.RegisterUnit(this);
        }

        public SLangUnitDefinition(Context ctx, UnitDeclaration ir) : this(ctx, ir.Name)
        {
            foreach (var declaration in ir.Declarations)
            {
                switch (declaration)
                {
                    case RoutineDeclaration routine:
                        // TODO: check is native
                        var _ = new SLangRoutineDefinition(this, routine);
                        break;

                    default:
                        throw new NotImplementedException("only routines declarations are supported within units");
                }
            }
        }

        public override void Compile()
        {
            foreach (var routine in Routines)
            {
                routine.Compile();
                NativeTypeDefinition.Methods.Add(routine.NativeMethod);
            }
        }
    }
}