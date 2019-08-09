using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using SLang.IR;
using SLang.NET.BuiltIns;
using static MoreLinq.Extensions.ForEachExtension;

namespace SLang.NET.Gen
{
    public class Context
    {
        /// <summary>
        /// Collection of user-defined types / units.
        /// </summary>
        public List<UnitDefinition> Units { get; } = new List<UnitDefinition>();

        /// <summary>
        /// Container for user-defined static global routines.
        ///
        /// <para>Owned by <see cref="Units"/> list.</para>
        /// </summary>
        public SLangUnitDefinition GlobalUnit { get; set; }

        /// <summary>
        /// Collection of pre-defined units, which are part of the standard library.
        /// </summary>
        public List<BuiltInUnitDefinition> RuntimeUnits { get; } = new List<BuiltInUnitDefinition>();

        /// <summary>
        /// Container for pre-defined static global routines, which are part of the standard library.
        ///
        /// <para>Owned by <see cref="Units"/> list.</para>
        /// </summary>
        public SLangUnitDefinition RuntimeGlobalUnit { get; }

        /// <summary>
        /// Collection of platform-specific intrinsic routines.
        /// Normally they should not be exposed to user programs,
        /// but only visible / used from SLang standard library.
        ///
        /// <para>Owned by <see cref="Units"/> list.</para>
        /// </summary>
        public Intrinsics Intrinsics { get; }

        public IEnumerable<UnitDefinition> AllUnits =>
            Units.Concat(RuntimeUnits);

        /// <summary>
        /// Underlying platform's native module.
        /// </summary>
        public ModuleDefinition NativeModule { get; }

        /// <summary>
        /// Bridge between SLang type system and platform type system. Provides 
        /// </summary>
        public TypeSystem TypeSystem { get; }

        public Context(ModuleDefinition nativeModule)
        {
            NativeModule = nativeModule;
            TypeSystem = new TypeSystem(this);

            Intrinsics = RegisterUnit(new Intrinsics(this));
            RuntimeGlobalUnit = RegisterUnit(new SLangUnitDefinition(this, new Identifier("$Runtime")));
            RegisterRuntimeUnits();
        }

        private void RegisterRuntimeUnits()
        {
            // high level
            RegisterRuntimeUnit(new VoidBuiltInUnitDefinition(this));
            RegisterRuntimeUnit(new IntegerBuiltInUnitDefinition(this));
            RegisterRuntimeUnit(new StringBuiltInUnitDefinition(this));
            RegisterRuntimeUnit(new RealBuiltInUnitDefinition(this));

            // low level: no need to add native type
        }

        private void RegisterRuntimeUnit(BuiltInUnitDefinition unit)
        {
            // high level
            RuntimeUnits.Add(unit);
            // low level
            RegisterNativeType(unit);
        }

        /// <summary>
        /// Add unit and its native underlying type. 
        /// </summary>
        /// <param name="unit">User-defined SLang unit</param>
        public T RegisterUnit<T>(T unit) where T : SLangUnitDefinition
        {
            // high level
            Units.Add(unit);
            // low level
            RegisterNativeType(unit);

            return unit;
        }

        private void RegisterNativeType(UnitDefinition unit)
        {
            if (!unit.IsForeign)
                NativeModule.Types.Add(unit.NativeTypeDefinition);
        }

        public UnitDefinition Resolve(UnitReference unitReference)
        {
            try
            {
                return AllUnits.Single(unit => unit == unitReference);
            }
            catch (InvalidOperationException)
            {
                throw new UnitNotFoundException(this, unitReference);
            }
        }

        public RoutineDefinition Resolve(RoutineReference routine)
        {
            // No unit reference? -> try global, then runtime.
            // Else, resolve unit and forward the request.
            if (routine.Unit == null)
            {
                try
                {
                    RoutineReference r = new RoutineReference(GlobalUnit, routine.Name);
                    return GlobalUnit.Resolve(r);
                }
                catch (RoutineNotFoundException)
                {
                    RoutineReference r = new RoutineReference(RuntimeGlobalUnit, routine.Name);
                    return RuntimeGlobalUnit.Resolve(r);
                }
            }
            else
            {
                var unit = routine.Unit.Resolve();
                return unit.Resolve(routine);
            }
        }

        internal BuiltInUnitDefinition ResolveBuiltIn(UnitReference unitReference)
        {
            try
            {
                return RuntimeUnits.Single(unit => unit == unitReference);
            }
            catch (InvalidOperationException)
            {
                throw new UnitNotFoundException(this, unitReference);
            }
        }

        public void Compile()
        {
            AllUnits.ForEach(unit => unit.Stage1RoutineStubs());
            AllUnits.ForEach(unit => unit.Stage2RoutineBody());
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