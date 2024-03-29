using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using SLang.IR.JSON;

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

        public static Identifier Empty = new Identifier(string.Empty);

        public Identifier(string value)
        {
            Value = string.Intern(value);
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
        public RoutineDeclaration Anonymous { get; set; }

        public Compilation(IEnumerable<Declaration> declarations, RoutineDeclaration anonymous)
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

    public class RoutineDeclaration : Declaration
    {
        public bool IsForeign { get; set; }
        public List<Parameter> Parameters { get; } = new List<Parameter>();
        public UnitRef ReturnType { get; set; }
        public PreCondition PreCondition { get; set; }
        public List<Entity> Body { get; } = new List<Entity>();
        public PostCondition PostCondition { get; set; }

        public RoutineDeclaration(
            Identifier name,
            bool isForeign,
            IEnumerable<Parameter> parameters,
            UnitRef returnType,
            IEnumerable<Entity> body,
            PreCondition preCondition = null,
            PostCondition postCondition = null
        )
            : base(name)
        {
            IsForeign = isForeign;
            Parameters.AddRange(parameters);
            ReturnType = returnType;
            Body.AddRange(body);
            PreCondition = preCondition ?? new PreCondition();
            PostCondition = postCondition ?? new PostCondition();
        }

        internal class ParameterList : Entity
        {
            public List<Parameter> Parameters { get; } = new List<Parameter>();

            public ParameterList(IEnumerable<Parameter> parameters)
            {
                Parameters.AddRange(parameters);
            }
        }

        public class Parameter : Entity
        {
            // TODO: replace with more general TYPE (when it will be specified and implemented)
            public UnitRef Type { get; set; }
            public Identifier Name { get; set; }

            public Parameter(UnitRef type, Identifier name)
            {
                Type = type;
                Name = name;
            }
        }
    }

    public class VariableDeclaration : Declaration
    {
        // TODO: replace with appropriate Type type
        public UnitRef Type { get; set; }
        public Expression Initializer { get; set; }
        public UnitDeclaration.RefValSpec RefOrVal { get; set; }
        public bool IsConcurrent { get; set; }
        public bool IsForeign { get; set; }


        public VariableDeclaration(Identifier name, UnitRef type, Expression initializer)
            : base(name)
        {
            Type = type;
            Initializer = initializer;
        }
    }

    public class UnitDeclaration : Declaration
    {
        #region Internal classes

        public class RefValSpec : Entity
        {
            public bool IsRef { get; set; }

            [IgnoreDataMember]
            public bool IsVal
            {
                get => !IsRef;
                set => IsRef = !value;
            }

            public RefValSpec(bool isRef = true)
            {
                IsRef = isRef;
            }
        }

        internal class ConcurrentSpec : Entity
        {
            public bool Concurrent { get; }

            public ConcurrentSpec(bool concurrent)
            {
                Concurrent = concurrent;
            }
        }

        #endregion

        #region Members

        public RefValSpec RefOrVal { get; set; }
        public bool IsConcurrent { get; set; }
        public bool IsForeign { get; set; }
        public List<Declaration> Declarations { get; } = new List<Declaration>();
        public List<Expression> Invariants { get; } = new List<Expression>();

        #endregion

        public UnitDeclaration(
            Identifier name,
            RefValSpec refOrVal,
            bool isConcurrent,
            bool isForeign,
            IEnumerable<Declaration> declarations = null,
            IEnumerable<Expression> invariants = null
        )
            : base(name)
        {
            RefOrVal = refOrVal;
            IsConcurrent = isConcurrent;
            IsForeign = isForeign;
            if (declarations != null) Declarations.AddRange(declarations);
            if (invariants != null) Invariants.AddRange(invariants);
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
        public static UnitRef Void { get; } = new UnitRef(new Identifier(typeof(void).Name));

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
        public Callee Callee { get; set; }
        public List<Expression> Arguments { get; } = new List<Expression>();

        public Call(Callee callee, IEnumerable<Expression> arguments)
        {
            Callee = callee;
            Arguments.AddRange(arguments);
        }

        internal Call(Callee callee, ExpressionList arguments = null)
        {
            Callee = callee;
            if (arguments != null)
                Arguments.AddRange(arguments.Expressions);
        }
    }

    public class Callee : Expression
    {
        /// <summary>
        /// Declaring Unit. Must be null for global routines.
        /// </summary>
        public Identifier Unit { get; set; }

        public Identifier Routine { get; set; }

        public Callee(Identifier unit, Identifier routine)
        {
            Unit = unit;
            Routine = routine;
        }
    }

    public class If : Entity
    {
        public List<(Expression Condition, List<Entity> Body)> IfThen { get; }

        /// <summary>
        /// Else branch may be null.
        /// </summary>
        public List<Entity> Else { get; }

        public If(List<(Expression Condition, List<Entity> Body)> ifThen, List<Entity> @else = null)
        {
            if (ifThen.Count == 0)
                throw new IrEntityException(this, "If/then list must have at least one pair");
            IfThen = ifThen;
            Else = @else;
        }

        internal If(StmtIfThenList list, EntityList @else = null)
            : this(
                list.Children.Select(ifThen => (ifThen.Condition, ifThen.Body)).ToList(),
                @else?.Children)
        {
        }

        internal class StmtIfThenList : Entity
        {
            public List<StmtIfThen> Children { get; } = new List<StmtIfThen>();

            public StmtIfThenList(IEnumerable<StmtIfThen> children)
            {
                Children.AddRange(children);
            }
        }

        internal class StmtIfThen : Entity
        {
            public Expression Condition { get; }

            public List<Entity> Body { get; }

            public StmtIfThen(Expression condition, List<Entity> body)
            {
                Condition = condition;
                Body = body;
            }
        }
    }

    public class Assignment : Entity
    {
        public Expression LValue { get; set; }
        public Expression RValue { get; set; }

        public Assignment(Expression lValue, Expression rValue)
        {
            LValue = lValue;
            RValue = rValue;
        }
    }

    public class Loop : Entity
    {
        public Expression OptionalExitCondition { get; set; }
        public List<Entity> Body { get; } = new List<Entity>();

        public Loop(Expression optionalExitCondition, IEnumerable<Entity> body)
        {
            OptionalExitCondition = optionalExitCondition;
            Body.AddRange(body);
        }

        internal Loop(Expression optionalExitCondition, EntityList body)
            : this(optionalExitCondition, body?.Children)
        {
        }
    }
}