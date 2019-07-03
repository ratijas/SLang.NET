using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;

namespace SLang.NET.Gen
{
    public abstract class UnitDefinition
    {
        public abstract bool IsNative { get; }
        public Identifier Name { get; protected set; }
        public ModuleDefinition NativeModule { get; protected set; }
        public TypeDefinition NativeType { get; protected set; }
        public List<RoutineDefinition> Routines { get; } = new List<RoutineDefinition>();
    }

    public abstract class BuiltInUnitDefinition : UnitDefinition
    {
        public sealed override bool IsNative => true;

        public static BuiltInUnitDefinition Get(ModuleDefinition module, Identifier unit)
        {
            // TODO: cache dictionary
            var dict = new Dictionary<Identifier, BuiltInUnitDefinition>();
            BuiltInUnitDefinition u;
            u = new IntegerBuiltInUnitDefinition(module);
            dict.Add(u.Name, u);
            u = new StringBuiltInUnitDefinition(module);
            dict.Add(u.Name, u);

            if (!dict.TryGetValue(unit, out var definition))
                throw new NotImplementedException("Some built-in units are not implemented yet");

            return definition;
        }

        protected BuiltInUnitDefinition(Identifier name, TypeReference underlyingType)
        {
            NativeType = underlyingType.Resolve();
            NativeModule = NativeType.Module;
            Name = name;
        }

        public abstract void LoadFromLiteral(string literal, ILProcessor ip);

        public class IntegerBuiltInUnitDefinition : BuiltInUnitDefinition
        {
            public IntegerBuiltInUnitDefinition(ModuleDefinition module)
                : base(new Identifier("Integer"), module.TypeSystem.Int32)
            {
            }

            public override void LoadFromLiteral(string literal, ILProcessor ip)
            {
                if (!int.TryParse(literal, out var result))
                    throw new FormatException($"Unable to parse integer literal: '{literal}'");

                ip.Emit(OpCodes.Ldc_I4, result);
            }
        }

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

    public class SLangUnitDefinition : UnitDefinition
    {
        public sealed override bool IsNative => false;

        public const string SLangUnitDotNETNamespace = "SLang";

        public SLangUnitDefinition(ModuleDefinition nativeModule, Identifier name)
        {
            Name = name;
            NativeModule = nativeModule;
            NativeType = new TypeDefinition(SLangUnitDotNETNamespace, Name.Value,
                TypeAttributes.Public | TypeAttributes.Class,
                NativeModule.TypeSystem.Object);
            NativeModule.Types.Add(NativeType);
        }

        public RoutineDefinition DefineRoutine(Routine routine)
        {
            var routineDefinition = new SLangRoutineDefinition(NativeModule, this, routine);
            NativeType.Methods.Add(routineDefinition.NativeMethod);
            Routines.Add(routineDefinition);
            return routineDefinition;
        }

        public void Compile()
        {
            foreach (var routine in Routines)
            {
                routine.Compile();
            }
        }
    }
}