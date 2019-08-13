using System.Collections.Generic;
using SLang.IR;

namespace SLang.NET.Gen
{
    public class Scope
    {
        /// <summary>
        /// Create new root scope.
        /// </summary>
        public Scope()
        {
            _root = this;
            _parent = null;
        }

        /// <summary>
        /// Create child scope with parent.
        /// </summary>
        public Scope(Scope parent)
        {
            _root = parent.RootScope();
            _parent = parent;
        }

        private Scope _parent;
        private Scope _root;

        /// <summary>
        /// Optional parent scope. Null for _root scope.
        /// </summary>
        public Scope ParentScope() => _parent;

        public Scope RootScope() => _root;

        /// <summary>
        /// Variables declared in this scope.
        /// </summary>
        public Dictionary<Identifier, Variable> Variables;

        public Variable Get(Identifier name)
        {
            if (Variables.TryGetValue(name, out var variable))
            {
                return variable;
            }
            else if (ParentScope() != null)
            {
                try
                {
                    return ParentScope().Get(name);
                }
                catch (VariableNotFoundException)
                {
                }
            }

            throw new VariableNotFoundException(this, name);
        }
    }
}