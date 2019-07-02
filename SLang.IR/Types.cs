using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SLang.IR.JSON;

namespace SLang.IR
{
    public abstract class Entity
    {
    }

    public class Identifier : Entity
    {
        public string Value { get; }

        public Identifier(string value)
        {
            Value = value;
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

    internal class DeclarationList : Entity
    {
        public List<Declaration> Declarations { get; } = new List<Declaration>();

        public DeclarationList(IEnumerable<Declaration> declarations)
        {
            Declarations.AddRange(declarations);
        }
    }

    internal class EntityList : Entity
    {
        public List<Entity> Children { get; } = new List<Entity>();

        public EntityList(IEnumerable<Entity> children)
        {
            Children.AddRange(children);
        }
    }

    public abstract class Declaration : Entity
    {
    }

    public class Routine : Declaration
    {
        public Identifier Name { get; set; }
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
        {
            Name = name;
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
        private static UnitRef _void = new UnitRef(new Identifier("$void"));
        public static UnitRef Void => _void;
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

    public class Literal : Expression
    {
        public string Value { get; set; }
        public UnitRef Type { get; set; }

        public Literal(string value, UnitRef ofType)
        {
            Value = value;
            Type = ofType;
        }
    }


//    ENTITY (abstract)
//
//    IDENTIFIER
//    COMPILATION (skipped in JSON IR)
//    TYPE (abstract)
//UNIT_REF
//MULTI_TYPE
//RANGE_TYPE
//TUPLE_TYPE (Not Implemented Yet in JSON IR)
//ROUTINE_TYPE (Not Implemented Yet)
//DECLARATION (abstract)
//VARIABLE
//ROUTINE
//INITIALIZER
//UNIT
//PACKAGE
//CONSTANT
//STATEMENT (abstract)
//BODY (skipped in JSON IR)
//IF
//STMT_IF_THEN
//CHECK
//RAISE
//RETURN
//BREAK
//ASSIGNMENT
//LOOP
//TRY
//CATCH
//EXPRESSION (abstract)
//PRIMARY (abstract)
//CONDITIONAL
//THIS
//RETURN_EXPR
//OLD
//REFERENCE
//UNRESOLVED
//LITERAL
//TUPLE_EXPR
//COND_IF_THEN
//SECONDARY
//MEMBER
//CALL
//UNARY
//NEW
//IN_EXPRESSION
//BINARY
//POWER
//MULTIPLICATIVE
//ADDITIVE
//RELATIONAL
//LOGICAL
}