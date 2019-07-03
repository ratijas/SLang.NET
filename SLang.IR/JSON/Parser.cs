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
            IDENTIFIER = "IDENTIFIER",
            ENTITY_LIST = "ENTITY_LIST",
            UNIT_REF = "UNIT_REF",
            EXPRESSION_LIST = "EXPRESSION_LIST",
            PRECONDITION = "PRECONDITION",
            POSTCONDITION = "POSTCONDITION",
            RETURN = "RETURN",
            LITERAL = "LITERAL",
            CALL = "CALL",
            REFERENCE = "REFERENCE",
            VARIABLE = "VARIABLE"
            ;

        public Parser()
        {
            _parsers = new Dictionary<string, ParserDelegate>
            {
                {COMPILATION, ParseCompilation},
                {DECLARATION_LIST, ParseDeclarationList},
                {ROUTINE, ParseRoutine},
                {FOREIGN_SPEC, ParseForeignSpec},
                {IDENTIFIER, ParseIdentifier},
                {ENTITY_LIST, ParseEntityList},
                {UNIT_REF, ParseUnitRef},
                {EXPRESSION_LIST, ParseExpressionList},
                {PRECONDITION, ParsePreCondition},
                {POSTCONDITION, ParsePostCondition},
                {RETURN, ParseReturn},
                {LITERAL, ParseLiteral},
                {CALL, ParseCall},
                {REFERENCE, ParseReference},
                {VARIABLE, ParseVariable},
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
            var routine = children.OfType<Routine>().FirstOrDefault();

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

        private Routine ParseRoutine(JsonEntity o)
        {
            EntityMixin.CheckType(o, ROUTINE);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                // name
                var name = children.OfType<Identifier>().Single();
                // is foreign? (may be null in JSON but not in an object model)
                var isForeign = children.OfType<ForeignSpec>().Select(spec => spec.IsForeign).DefaultIfEmpty(false).SingleOrDefault();
                // argument list
                var arguments = children.OfType<EntityList>().First().Children.Select(entity => new Routine.Argument(entity));
                // return type (may be null is JSON but not in an object model)
                var returnType = children.OfType<UnitRef>().DefaultIfEmpty(UnitRef.Void).SingleOrDefault();
                // precondition body (may be null)
                var pre = children.OfType<PreCondition>().DefaultIfEmpty(new PreCondition()).SingleOrDefault();
                // routine body
                var body = children.OfType<EntityList>().Skip(1).First().Children;
                // postcondition body (may be null)
                var post = children.OfType<PostCondition>().DefaultIfEmpty(new PostCondition()).SingleOrDefault();

                // ReSharper disable ArgumentsStyleNamedExpression
                return new Routine(
                    name: name,
                    isForeign: isForeign,
                    arguments: arguments,
                    returnType: returnType,
                    preCondition: pre,
                    body: body,
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
                var callee = children.OfType<Expression>().Single();
                var arguments = children.OfType<ExpressionList>().SingleOrDefault();

                return new Call(callee, arguments);
            });
        }

        private Reference ParseReference(JsonEntity o)
        {
            EntityMixin.CheckType(o, REFERENCE);
            EntityMixin.ValueMustBeNull(o);

            return Guard(o, children =>
            {
                var name = children.OfType<Identifier>().Single();

                return new Reference(name);
                // TODO: example and parser source code mismatch
                throw new NotImplementedException("example and parser source code mismatch");
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