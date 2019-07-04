using System;
using System.Collections.Generic;

namespace SLang.IR
{
    public abstract class Entity
    {
    }

    internal class EntityList : Entity
    {
        public List<Entity> Children { get; } = new List<Entity>();

        public EntityList(IEnumerable<Entity> children)
        {
            Children.AddRange(children);
        }
    }

    public sealed class Identifier : Entity
    {
        public string Value { get; }

        public Identifier(string value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is Identifier identifier && Value.Equals(identifier.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public class Compilation : Entity
    {
        public List<Declaration> Declarations { get; } = new List<Declaration>();
        public Routine Anonymous { get; set; }

        public Compilation(IEnumerable<Declaration> declarations, Routine anonymous)
        {
            Declarations.AddRange(declarations);
            Anonymous = anonymous;
        }
    }

    public abstract class Declaration : Entity
    {
        public Identifier Name { get; set; }

        protected Declaration(Identifier name)
        {
            Name = name;
        }
    }

    internal class DeclarationList : Entity
    {
        public List<Declaration> Declarations { get; } = new List<Declaration>();

        public DeclarationList(IEnumerable<Declaration> declarations)
        {
            Declarations.AddRange(declarations);
        }
    }

    public class Routine : Declaration
    {
        public bool IsForeign { get; set; }
        public List<Argument> Arguments { get; } = new List<Argument>();
        public UnitRef ReturnType { get; set; }
        public PreCondition PreCondition { get; set; }
        public List<Entity> Body { get; } = new List<Entity>();
        public PostCondition PostCondition { get; set; }

        public Routine(
            Identifier name,
            bool isForeign,
            IEnumerable<Argument> arguments,
            UnitRef returnType,
            PreCondition preCondition,
            IEnumerable<Entity> body,
            PostCondition postCondition
        )
            : base(name)
        {
            IsForeign = isForeign;
            Arguments.AddRange(arguments);
            ReturnType = returnType;
            PreCondition = preCondition;
            Body.AddRange(body);
            PostCondition = postCondition;
        }

        public class Argument
        {
            public UnitRef Type { get; set; }
            public Identifier Name { get; set; }

            public Argument(UnitRef type, Identifier name)
            {
                Type = type;
                Name = name;
            }

            internal Argument(Entity fromEntity)
            {
                // TODO: refine specification
                throw new NotImplementedException("TODO: refine specification");
            }
        }
    }

    public class Variable : Declaration
    {
        // TODO: replace with appropriate Type type
        /// <summary>
        /// Type may be implicit, in which case it is up to compiler to infer it from initializer expression.
        /// </summary>
        public UnitRef OptionalType { get; set; }

        public Expression OptionalInitializer { get; set; }

        public Variable(Identifier name, UnitRef type, Expression initializer)
            : base(name)
        {
            OptionalType = type;
            OptionalInitializer = initializer;
        }
    }

    public class ForeignSpec : Entity
    {
        public bool IsForeign { get; }

        public ForeignSpec(bool isForeign = false)
        {
            IsForeign = isForeign;
        }
    }

    public class UnitRef : Entity
    {
        public static UnitRef Void { get; } = new UnitRef(new Identifier("$void"));

        public Identifier Name { get; }

        public UnitRef(Identifier name)
        {
            Name = name;
        }
    }

    internal class ExpressionList : Entity
    {
        public List<Expression> Expressions { get; } = new List<Expression>();

        public ExpressionList(IEnumerable<Expression> expressions)
        {
            Expressions.AddRange(expressions);
        }
    }

    public abstract class PrePostCondition : Entity
    {
        public List<Expression> ExpressionList { get; } = new List<Expression>();

        internal PrePostCondition(ExpressionList expressionList = null)
        {
            if (expressionList != null)
                ExpressionList.AddRange(expressionList.Expressions);
        }

        protected PrePostCondition()
        {
        }
    }

    public class PreCondition : PrePostCondition
    {
        internal PreCondition(ExpressionList expressionList = null) : base(expressionList)
        {
        }

        public PreCondition()
        {
        }
    }

    public class PostCondition : PrePostCondition
    {
        internal PostCondition(ExpressionList expressionList = null) : base(expressionList)
        {
        }

        public PostCondition()
        {
        }
    }

    public class Return : Entity
    {
        public Expression OptionalValue { get; set; }

        public Return(Expression value = null)
        {
            OptionalValue = value;
        }
    }

    public abstract class Expression : Entity
    {
    }

    public abstract class Primary : Expression
    {
    }

    public class Literal : Primary
    {
        public string Value { get; set; }
        public UnitRef Type { get; set; }

        public Literal(string value, UnitRef ofType)
        {
            Value = value;
            Type = ofType;
        }
    }

    public class Reference : Primary
    {
        public Identifier Name { get; }

        public Reference(Identifier name)
        {
            Name = name;
        }
    }

    public abstract class Secondary : Expression
    {
    }

    public class Call : Secondary
    {
        public Expression Callee { get; set; }
        public List<Expression> Arguments { get; } = new List<Expression>();

        public Call(Expression callee, IEnumerable<Expression> arguments)
        {
            Callee = callee;
            Arguments.AddRange(arguments);
        }

        internal Call(Expression callee, ExpressionList arguments = null)
        {
            Callee = callee;
            if (arguments != null)
                Arguments.AddRange(arguments.Expressions);
        }
    }
}