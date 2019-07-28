using System;
using System.Collections.Generic;
using System.Linq;

namespace SLang.IR.JSON
{
    static class EntityMixin
    {
        public static void CheckType(JsonEntity json, string type)
        {
            if (json.Type != type)
                throw new JsonFormatException(json, $"type expected to be {type}");
        }

        public static void ValueMustBeNull(JsonEntity json)
        {
            if (json.Value != null)
                throw new JsonFormatException(json, "value must be null");
        }

        public static void ValueMustNotBeNull(JsonEntity json)
        {
            if (json.Value == null)
                throw new JsonFormatException(json, "value must not be null");
        }
    }


    public class Parser
    {
        protected delegate Entity ParserDelegate(JsonEntity o);

        protected readonly Dictionary<string, ParserDelegate> _parsers;

        protected const string
            COMPILATION = "COMPILATION",
            DECLARATION_LIST = "DECLARATION_LIST",
            ROUTINE = "ROUTINE",
            FOREIGN_SPEC = "FOREIGN_SPEC",
            PARAMETER_LIST = "PARAMETER_LIST",
            PARAMETER = "PARAMETER",
            IDENTIFIER = "IDENTIFIER",
            ENTITY_LIST = "ENTITY_LIST",
            UNIT_REF = "UNIT_REF",
            EXPRESSION_LIST = "EXPRESSION_LIST",
            PRECONDITION = "PRECONDITION",
            POSTCONDITION = "POSTCONDITION",
            RETURN = "RETURN",
            LITERAL = "LITERAL",
            CALL = "CALL",
            CALLEE = "CALLEE",
            REFERENCE = "REFERENCE",
            VARIABLE = "VARIABLE",
            UNIT = "UNIT",
            REF_VAL_SPEC = "REF_VAL_SPEC",
            CONCURRENT_SPEC = "CONCURRENT_SPEC",
            IF = "IF",
            STMT_IF_THEN_LIST = "STMT_IF_THEN_LIST",
            STMT_IF_THEN = "STMT_IF_THEN";

        public Parser()
        {
            _parsers = new Dictionary<string, ParserDelegate>
            {
                {COMPILATION, ParseCompilation},
                {DECLARATION_LIST, ParseDeclarationList},
                {ROUTINE, ParseRoutine},
                {FOREIGN_SPEC, ParseForeignSpec},
                {PARAMETER_LIST, ParseParameterList},
                {PARAMETER, ParseParameter},
                {IDENTIFIER, ParseIdentifier},
                {ENTITY_LIST, ParseEntityList},
                {UNIT_REF, ParseUnitRef},
                {EXPRESSION_LIST, ParseExpressionList},
                {PRECONDITION, ParsePreCondition},
                {POSTCONDITION, ParsePostCondition},
                {RETURN, ParseReturn},
                {LITERAL, ParseLiteral},
                {CALL, ParseCall},
                {CALLEE, ParseCallee},
                {REFERENCE, ParseReference},
                {VARIABLE, ParseVariable},
                {UNIT, ParseUnit},
                {REF_VAL_SPEC, ParseRefValSpec},
                {CONCURRENT_SPEC, ParseConcurrentSpec},
                {IF, ParseIf},
                {STMT_IF_THEN_LIST, ParseStmtIfThenList},
                {STMT_IF_THEN, ParseStmtIfThen},
            };
        }

        public Entity Parse(JsonEntity o)
        {
            if (o == null) return null;
            var ty = o.Type;
            if (string.IsNullOrEmpty(ty)) throw new JsonFormatException(o);
            if (!_parsers.TryGetValue(ty, out var parser)) throw new NotImplementedException(ty);

            return parser(o);
        }

        protected IEnumerable<Entity> ParseChildren(JsonEntity o)
        {
            return o.Children?.Select(Parse) ?? new Entity[0];
        }

        public Compilation ParseCompilation(JsonEntity o)
        {
            EntityMixin.CheckType(o, COMPILATION);
            EntityMixin.ValueMustBeNull(o);

            var children = ParseChildren(o).ToList();
            var declarations = children.OfType<DeclarationList>().FirstOrDefault()?.Declarations;
            var routine = children.OfType<RoutineDeclaration>().FirstOrDefault();

            return new Compilation(declarations, routine);
        }

        private DeclarationList ParseDeclarationList(JsonEntity o)
        {
            EntityMixin.CheckType(o, DECLARATION_LIST);
            EntityMixin.ValueMustBeNull(o);

            return new DeclarationList(ParseChildren(o).OfType<Declaration>().ToList());
        }

        private EntityList ParseEntityList(JsonEntity o)
        {
            EntityMixin.CheckType(o, ENTITY_LIST);
            EntityMixin.ValueMustBeNull(o);

            return new EntityList(ParseChildren(o).ToList());
        }

        private RoutineDeclaration ParseRoutine(JsonEntity o)
        {
            EntityMixin.CheckType(o, ROUTINE);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                // name
                var name = children.OfType<Identifier>().Single();
                // is foreign? (may be null in JSON but not in an object model)
                var isForeign = children.OfType<ForeignSpec>().Select(spec => spec.IsForeign).DefaultIfEmpty(false)
                    .SingleOrDefault();
                // parameters list
                var parameters = children.OfType<RoutineDeclaration.ParameterList>().Single().Parameters;
                // return type (may be null is JSON but not in an object model)
                var returnType = children.OfType<UnitRef>().DefaultIfEmpty(UnitRef.Void).SingleOrDefault();
                // precondition body (may be null)
                var pre = children.OfType<PreCondition>().DefaultIfEmpty(new PreCondition()).SingleOrDefault();
                // routine body
                var body = children.OfType<EntityList>().Single().Children;
                // postcondition body (may be null)
                var post = children.OfType<PostCondition>().DefaultIfEmpty(new PostCondition()).SingleOrDefault();

                // ReSharper disable ArgumentsStyleNamedExpression
                return new RoutineDeclaration(
                    name: name,
                    isForeign: isForeign,
                    parameters: parameters,
                    returnType: returnType,
                    body: body,
                    preCondition: pre,
                    postCondition: post);
            });
        }

        private ForeignSpec ParseForeignSpec(JsonEntity o)
        {
            EntityMixin.CheckType(o, FOREIGN_SPEC);
            switch (o.Value)
            {
                case null:
                    return new ForeignSpec(false);
                case "foreign":
                    return new ForeignSpec(true);
                default:
                    throw new JsonFormatException(o, "invalid foreign spec: value must be either null or \"foreign\"");
            }
        }

        private RoutineDeclaration.ParameterList ParseParameterList(JsonEntity o)
        {
            EntityMixin.CheckType(o, PARAMETER_LIST);
            EntityMixin.ValueMustBeNull(o);

            return new RoutineDeclaration.ParameterList(ParseChildren(o).OfType<RoutineDeclaration.Parameter>());
        }

        private RoutineDeclaration.Parameter ParseParameter(JsonEntity o)
        {
            EntityMixin.CheckType(o, PARAMETER);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                // TODO: replace with more general TYPE (when it will be specified and implemented)
                var type = children.OfType<UnitRef>().Single();
                var name = children.OfType<Identifier>().Single();
                return new RoutineDeclaration.Parameter(type, name);
            });
        }

        private Identifier ParseIdentifier(JsonEntity o)
        {
            EntityMixin.CheckType(o, IDENTIFIER);
            EntityMixin.ValueMustNotBeNull(o);
            if (string.IsNullOrEmpty(o.Value)) throw new JsonFormatException(o, "Identifier must not be empty");

            return new Identifier(o.Value);
        }

        private UnitRef ParseUnitRef(JsonEntity o)
        {
            EntityMixin.CheckType(o, UNIT_REF);
            EntityMixin.ValueMustNotBeNull(o);
            if (string.IsNullOrEmpty(o.Value)) throw new JsonFormatException(o, "Unit Ref must not be empty");

            return new UnitRef(new Identifier(o.Value));
        }

        private ExpressionList ParseExpressionList(JsonEntity o)
        {
            EntityMixin.CheckType(o, EXPRESSION_LIST);
            EntityMixin.ValueMustBeNull(o);

            return new ExpressionList(ParseChildren(o).OfType<Expression>());
        }

        private PreCondition ParsePreCondition(JsonEntity o)
        {
            EntityMixin.CheckType(o, PRECONDITION);
            EntityMixin.ValueMustBeNull(o);

            var expressions = ParseChildren(o).OfType<ExpressionList>().FirstOrDefault();

            return new PreCondition(expressions);
        }

        private PostCondition ParsePostCondition(JsonEntity o)
        {
            EntityMixin.CheckType(o, POSTCONDITION);
            EntityMixin.ValueMustBeNull(o);

            var expressions = ParseChildren(o).OfType<ExpressionList>().FirstOrDefault();

            return new PostCondition(expressions);
        }

        private Return ParseReturn(JsonEntity o)
        {
            EntityMixin.CheckType(o, RETURN);
            EntityMixin.ValueMustBeNull(o);

            var optionalExpression = ParseChildren(o).OfType<Expression>().FirstOrDefault();
            return new Return(optionalExpression);
        }

        private Literal ParseLiteral(JsonEntity o)
        {
            EntityMixin.CheckType(o, LITERAL);
            EntityMixin.ValueMustNotBeNull(o);

            var unitRef = ParseChildren(o).OfType<UnitRef>().FirstOrDefault();
            if (unitRef == null) throw new JsonFormatException(o, "literal unit reference not found");

            return new Literal(o.Value, unitRef);
        }

        private Call ParseCall(JsonEntity o)
        {
            EntityMixin.CheckType(o, CALL);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                var callee = children.OfType<Callee>().Single();
                var arguments = children.OfType<ExpressionList>().SingleOrDefault();

                return new Call(callee, arguments);
            });
        }

        private Callee ParseCallee(JsonEntity o)
        {
            EntityMixin.CheckType(o, CALLEE);
            EntityMixin.ValueMustBeNull(o);

            var unit = o.Children[0] == null ? null : ParseIdentifier(o.Children[0]);
            var routine = ParseIdentifier(o.Children[1]);

            return new Callee(unit, routine);
        }

        private Reference ParseReference(JsonEntity o)
        {
            EntityMixin.CheckType(o, REFERENCE);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                var name = children.OfType<Identifier>().Single();

                return new Reference(name);
            });
        }

        private Variable ParseVariable(JsonEntity o)
        {
            EntityMixin.CheckType(o, VARIABLE);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                var name = children.OfType<Identifier>().Single();
                var type = children.OfType<UnitRef>().DefaultIfEmpty(null).SingleOrDefault();
                var init = children.OfType<Expression>().DefaultIfEmpty(null).SingleOrDefault();

                if (type == null && init == null)
                    throw new JsonFormatException(o, $"variable {name} has no type nor initializer");
                return new Variable(name, type, init);
            });
        }

        private UnitDeclaration ParseUnit(JsonEntity o)
        {
            EntityMixin.CheckType(o, UNIT);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                var name = children.OfType<Identifier>().Single();
                var refValSpec = children.OfType<UnitDeclaration.RefValSpec>().Single();
                var concurrentSpec = children.OfType<UnitDeclaration.ConcurrentSpec>().SingleOrDefault();
                var concurrent = concurrentSpec?.Concurrent ?? false;
                var foreignSpec = children.OfType<ForeignSpec>().SingleOrDefault();
                var isForeign = foreignSpec?.IsForeign ?? false;
                var declarations = children.OfType<DeclarationList>().SingleOrDefault()?.Declarations;
                var invariants = children.OfType<ExpressionList>().SingleOrDefault()?.Expressions;

                return new UnitDeclaration(name, refValSpec, concurrent, isForeign, declarations, invariants);
            });
        }

        private UnitDeclaration.RefValSpec ParseRefValSpec(JsonEntity o)
        {
            EntityMixin.CheckType(o, REF_VAL_SPEC);
            EntityMixin.ValueMustNotBeNull(o);

            switch (o.Value)
            {
                case "ref":
                    return new UnitDeclaration.RefValSpec(isRef: true);
                case "val":
                    return new UnitDeclaration.RefValSpec(isRef: false);
                default:
                    throw new JsonFormatException(o, $"{REF_VAL_SPEC} must be either ref or val");
            }
        }

        private UnitDeclaration.ConcurrentSpec ParseConcurrentSpec(JsonEntity o)
        {
            EntityMixin.CheckType(o, CONCURRENT_SPEC);

            switch (o.Value)
            {
                case null:
                    return new UnitDeclaration.ConcurrentSpec(concurrent: false);
                case "concurrent":
                    return new UnitDeclaration.ConcurrentSpec(concurrent: true);
                default:
                    throw new JsonFormatException(o, $"{CONCURRENT_SPEC} must be either \"concurrent\" or null");
            }
        }

        private If ParseIf(JsonEntity o)
        {
            EntityMixin.CheckType(o, IF);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                var pairs = children.OfType<If.StmtIfThenList>().Single();
                var @else = children.OfType<EntityList>().SingleOrDefault();
                return new If(pairs, @else);
            });
        }

        private If.StmtIfThenList ParseStmtIfThenList(JsonEntity o)
        {
            EntityMixin.CheckType(o, STMT_IF_THEN_LIST);
            EntityMixin.ValueMustBeNull(o);
            
            return new If.StmtIfThenList(ParseChildren(o).OfType<If.StmtIfThen>().ToList());
        }

        private If.StmtIfThen ParseStmtIfThen(JsonEntity o)
        {
            EntityMixin.CheckType(o, STMT_IF_THEN);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                var condition = children.OfType<Expression>().Single();
                var body = children.OfType<EntityList>().Single().Children;
                return new If.StmtIfThen(condition, body);
            });
        }

        /// <summary>
        /// Guard against First/Single methods throwing on empty/too long sequences
        /// </summary>
        /// <param name="o"></param>
        /// <param name="f"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="JsonFormatException"></exception>
        protected T Guard<T>(JsonEntity o, Func<List<Entity>, T> f)
        {
            try
            {
                var children = ParseChildren(o).ToList();
                return f(children);
            }
            catch (InvalidOperationException e)
            {
                throw new JsonFormatException(o, $"{o.Type} definition is invalid", e);
            }
        }
    }
}