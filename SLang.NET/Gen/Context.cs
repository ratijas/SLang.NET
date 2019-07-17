using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using SLang.IR;
using SLang.NET.BuiltIns;

namespace SLang.NET.Gen
{
    public class Context
    {
        public SLangUnitDefinition GlobalUnit { get; set; }

        public List<UnitDefinition> Units { get; } = new List<UnitDefinition>();

        public List<BuiltInUnitDefinition> BuiltInUnits { get; } = new List<BuiltInUnitDefinition>();

        public SLangUnitDefinition Runtime { get; }

        public ModuleDefinition NativeModule { get; }

        public TypeSystem TypeSystem { get; }

        public Context(ModuleDefinition nativeModule)
        {
            NativeModule = nativeModule;
            TypeSystem = new TypeSystem(this);
            Runtime = new SLangUnitDefinition(this, new Identifier("$Runtime"));
            RegisterBuiltIns();
        }

        private void RegisterBuiltIns()
        {
            // high level
            BuiltInUnits.Add(new VoidBuiltInUnitDefinition(this));
            BuiltInUnits.Add(new IntegerBuiltInUnitDefinition(this));
            BuiltInUnits.Add(new StringBuiltInUnitDefinition(this));
            BuiltInUnits.Add(new RealBuiltInUnitDefinition(this));

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
            if (routineReference.Unit != null)
            {
                var unit = routineReference.Unit.Resolve();
                return unit.Resolve(routineReference);
            }
            else
            {
                try
                {
                    return GlobalUnit.Resolve(routineReference);
                }
                catch (RoutineNotFoundException)
                {
                    return Runtime.Resolve(routineReference);
                }
            }
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
            Units.ForEach(unit => unit.Stage1RoutineStubs());
            Units.ForEach(unit => unit.Stage2RoutineBody());
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
        private BuiltInUnitDefinition type_real;

        public BuiltInUnitDefinition Void =>
            type_void ?? Resolve(out type_void, VoidBuiltInUnitDefinition.UnitName);

        public BuiltInUnitDefinition Integer =>
            type_integer ?? Resolve(out type_integer, IntegerBuiltInUnitDefinition.UnitName);

        public BuiltInUnitDefinition String =>
            type_string ?? Resolve(out type_string, StringBuiltInUnitDefinition.UnitName);

        public BuiltInUnitDefinition Real =>
            type_real ?? Resolve(out type_real, RealBuiltInUnitDefinition.UnitName);

        // TODO: add more built-in units
    }
}