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
    public partial class Variable
    {
        public UnitDefinition Type { get; }
        public Identifier Name { get; }
        public VariableDefinition NativeVariable { get; }

        public TypeReference NativeType => Type.NativeType;

        public Variable(UnitDefinition type, Identifier name)
        {
            Type = type;
            Name = name;
            NativeVariable = new VariableDefinition(Type.NativeType);
        }

        /// <summary>
        /// Create anonymous variable.
        /// </summary>
        /// <param name="type"></param>
        public Variable(UnitDefinition type) : this(type, Identifier.Empty)
        {
        }


        /// <summary>
        /// Store a value on the stack into variable and ensure that variable is added to method body's variables collection.
        /// </summary>
        /// <param name="ip">Method body's ILProcessor</param>
        public void Store(ILProcessor ip)
        {
            ip.Emit(OpCodes.Stloc, NativeVariable);
            EnsureAdded(ip);
        }

        /// <summary>
        /// Load the variable value onto the stack and ensure that variable is added to method body's variables collection.
        /// </summary>
        /// <param name="ip">Method body's ILProcessor</param>
        public void Load(ILProcessor ip)
        {
            ip.Emit(OpCodes.Ldloc, NativeVariable);
            EnsureAdded(ip);
        }

        private void EnsureAdded(ILProcessor ip)
        {
            if (!ip.Body.Variables.Contains(NativeVariable))
                ip.Body.Variables.Add(NativeVariable);
        }
    }
}