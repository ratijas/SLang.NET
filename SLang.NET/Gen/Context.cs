using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using MoreLinq;
using SLang.IR;
using SLang.NET.BuiltIns;

namespace SLang.NET.Gen
{
    public class Context
    {
        public SLangUnitDefinition GlobalUnit { get; set; }

        public List<UnitDefinition> Units { get; } = new List<UnitDefinition>();

        public List<BuiltInUnitDefinition> BuiltInUnits { get; } = new List<BuiltInUnitDefinition>();

        public ModuleDefinition NativeModule { get; }

        public TypeSystem TypeSystem { get; }

        public Context(ModuleDefinition nativeModule)
        {
            NativeModule = nativeModule;
            RegisterBuiltIns();
            TypeSystem = new TypeSystem(this);
        }

        private void RegisterBuiltIns()
        {
            // high level
            BuiltInUnits.Add(new VoidBuiltInUnitDefinition(this));
            BuiltInUnits.Add(new IntegerBuiltInUnitDefinition(this));
            BuiltInUnits.Add(new StringBuiltInUnitDefinition(this));

            Units.AddRange(BuiltInUnits);

            // low level: no need to add native type
        }

        /// <summary>
        /// Add unit and its native underlying type. 
        /// </summary>
        /// <param name="unit">User-defined SLang unit</param>
        internal void RegisterUnit(SLangUnitDefinition unit)
        {
            // high level
            Units.Add(unit);
            // low level
            NativeModule.Types.Add(unit.NativeTypeDefinition);
        }

        public UnitDefinition Resolve(UnitReference unitReference)
        {
            try
            {
                return Units.Single(unit => unit.Name.Equals(unitReference.Name));
            }
            catch (InvalidOperationException)
            {
                throw new UnitNotFoundException(this, unitReference);
            }
        }

        public RoutineDefinition Resolve(RoutineReference routineReference)
        {
            var unit = Resolve(routineReference.Unit);
            return unit.Resolve(routineReference);
        }

        public BuiltInUnitDefinition ResolveBuiltIn(UnitReference unitReference)
        {
            try
            {
                return BuiltInUnits.Single(unit => unit.Name.Equals(unitReference.Name));
            }
            catch (InvalidOperationException)
            {
                throw new UnitNotFoundException(this, unitReference);
            }
        }

        public void Compile()
        {
            Units.ForEach(unit => unit.Compile());
        }
    }

    public sealed class TypeSystem
    {
        public Context Context { get; }

        public TypeSystem(Context context)
        {
            Context = context;
        }

        private BuiltInUnitDefinition Resolve(out BuiltInUnitDefinition unitRef, Identifier builtIn)
        {
            unitRef = Context.ResolveBuiltIn(new UnitReference(Context, builtIn));
            return unitRef;
        }

        private BuiltInUnitDefinition type_void;
        private BuiltInUnitDefinition type_integer;
        private BuiltInUnitDefinition type_string;

        public BuiltInUnitDefinition Void =>
            type_void ?? Resolve(out type_void, VoidBuiltInUnitDefinition.UnitName);

        public BuiltInUnitDefinition Integer =>
            type_integer ?? Resolve(out type_integer, IntegerBuiltInUnitDefinition.UnitName);

        public BuiltInUnitDefinition String =>
            type_string ?? Resolve(out type_string, StringBuiltInUnitDefinition.UnitName);

        // TODO: add more built-in units
    }
}