using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SLang.IR;

namespace SLang.NET.Gen
{
    /// <summary>
    /// SLang-specific variable definition.
    /// </summary>
    /// <para>
    /// In addition to Mono.Cecil VariableDefinition type, includes SLang UnitDefinition and optional name Identifier.
    /// </para>
    public abstract partial class Variable
    {
        public UnitDefinition Type { get; }
        public Identifier Name { get; }

        public TypeReference NativeType => Type.NativeType;

        protected Variable(UnitDefinition type, Identifier name)
        {
            Type = type;
            Name = name;
        }

        /// <summary>
        /// Create anonymous variable.
        /// </summary>
        /// <param name="type"></param>
        protected Variable(UnitDefinition type) : this(type, Identifier.Empty)
        {
        }


        /// <summary>
        /// Store a value on the stack into variable and ensure that variable is added to method body's variables collection.
        /// </summary>
        /// <param name="ip">Method body's ILProcessor</param>
        public abstract void Store(ILProcessor ip);

        /// <summary>
        /// Load the variable value onto the stack and ensure that variable is added to method body's variables collection.
        /// </summary>
        /// <param name="ip">Method body's ILProcessor</param>
        public abstract void Load(ILProcessor ip);

        /// <summary>
        /// Load the <i>address</i> of the variable onto the stack and ensure that variable is added to method body's variables collection. 
        /// </summary>
        /// <param name="ip">Method body's ILProcessor</param>
        public abstract void LoadA(ILProcessor ip);
    }

    public class BodyVariable : Variable
    {
        public VariableDefinition NativeVariable { get; }

        public BodyVariable(UnitDefinition type, Identifier name) : base(type, name)
        {
            NativeVariable = new VariableDefinition(Type.NativeType);
        }

        /// <summary>
        /// Create anonymous variable.
        /// </summary>
        /// <param name="type"></param>
        public BodyVariable(UnitDefinition type) : this(type, Identifier.Empty)
        {
        }

        public override void Store(ILProcessor ip)
        {
            ip.Emit(OpCodes.Stloc, NativeVariable);
            EnsureAdded(ip);
        }

        public override void Load(ILProcessor ip)
        {
            ip.Emit(OpCodes.Ldloc, NativeVariable);
            EnsureAdded(ip);
        }

        public override void LoadA(ILProcessor ip)
        {
            ip.Emit(OpCodes.Ldloca, NativeVariable);
            EnsureAdded(ip);
        }

        private void EnsureAdded(ILProcessor ip)
        {
            if (!ip.Body.Variables.Contains(NativeVariable))
                ip.Body.Variables.Add(NativeVariable);
        }
    }

    /// <summary>
    /// Optimized class to use routine argument as a variable.
    /// </summary>
    public class ArgumentVariable : Variable
    {
        public int Index { get; }

        public ArgumentVariable(UnitDefinition type, Identifier name, int index) : base(type, name)
        {
            if (index < 0)
                throw new ArgumentNullException(nameof(index));
            Index = index;
        }

        public ArgumentVariable(UnitDefinition type, int index) : this(type, Identifier.Empty, index)
        {
        }

        public override void Store(ILProcessor ip)
        {
            ip.Emit(OpCodes.Starg, Index);
        }

        public override void Load(ILProcessor ip)
        {
            ip.Emit(OpCodes.Ldarg, Index);
        }

        public override void LoadA(ILProcessor ip)
        {
            ip.Emit(OpCodes.Ldarga, Index);
        }
    }
}