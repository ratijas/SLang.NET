using System;
using System.Collections.Generic;
using SLang.IR;

namespace SLang.NET.Gen
{
    public class Scope
    {
        /// <summary>
        /// Create new root scope.
        /// </summary>
        public Scope(string name = "<anonymous>")
        {
            _root = this;
            _parent = null;
            Name = name;
        }

        /// <summary>
        /// Create child scope with parent.
        /// </summary>
        public Scope(Scope parent, string name = "<anonymous>")
        {
            _root = parent.RootScope();
            _parent = parent;
            Name = name;
        }

        private Scope _parent;
        private Scope _root;

        /// <summary>
        /// Optional parent scope. Null for _root scope.
        /// </summary>
        public Scope ParentScope() => _parent;

        public Scope RootScope() => _root;
        
        public string Name { get; }

        /// <summary>
        /// Variables declared in this scope.
        /// </summary>
        public Dictionary<Identifier, Variable> Variables = new Dictionary<Identifier, Variable>();

        public void Declare(Identifier name, Variable variable)
        {
            Variables.Add(name, variable);
        }

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

        public void Debug(int level = 0)
        {
            var indent = new string(' ', level * 4);
            Console.Out.WriteLine($"{indent}Scope");

            indent = new string(' ', (level + 1) * 4);

            foreach (var (name, variable) in Variables)
            {
                Console.Out.WriteLine($"{indent}{name}: {variable.Type}");
            }

            if (_parent != null)
                _parent.Debug(level + 1);
        }
    }
}