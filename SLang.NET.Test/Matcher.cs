using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RE = System.Text.RegularExpressions;

namespace SLang.NET.Test
{
    public abstract class Matcher
    {
        public abstract bool IsMatch(string str);

        public abstract string Pattern { get; }

        public class Ignore : Matcher
        {
            public override bool IsMatch(string _)
            {
                return true;
            }

            public override string Pattern => "<Any>";
        }

        public class Exact : Matcher
        {
            public string Literal;

            public Exact(string literal)
            {
                Literal = literal;
            }

            public override bool IsMatch(string str)
            {
                return Literal.Equals(str);
            }

            public override string Pattern => '"' + Literal + '"';
        }

        public class Regex : Matcher
        {
            internal string pattern;
            public RE.Regex Re;

            public Regex(string regExp)
            {
                pattern = regExp;
                Re = new RE.Regex($"^{regExp}$");
            }

            public override bool IsMatch(string str)
            {
                return Re.IsMatch(str);
            }

            public override string Pattern => $"/{pattern}/";
        }
    }

    public class MatcherJsonConverter : JsonConverter<Matcher>
    {
        public override void WriteJson(JsonWriter writer, Matcher value, JsonSerializer serializer)
        {
            switch (value)
            {
                case Matcher.Ignore _:
                    serializer.Serialize(writer, new {ignore = true});
                    break;

                case Matcher.Exact exact:
                    serializer.Serialize(writer, exact.Literal);
                    break;

                case Matcher.Regex regex:
                    serializer.Serialize(writer, new {regex = regex.pattern});
                    break;
            }
        }

        public override Matcher ReadJson(JsonReader reader, Type objectType, Matcher existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            Matcher matcher = existingValue;

            switch (token)
            {
                case JValue value when value.Type == JTokenType.String && value.Value is string literal:
                    matcher = new Matcher.Exact(literal);
                    break;
                case JObject obj
                    when obj.Property("ignore", StringComparison.InvariantCultureIgnoreCase)?.Value is JValue ignore &&
                         ignore.Type == JTokenType.Boolean &&
                         ignore.Value is bool shouldIgnore &&
                         shouldIgnore:
                    matcher = new Matcher.Ignore();
                    break;
                case JObject obj
                    when obj.Property("regex", StringComparison.InvariantCultureIgnoreCase)?.Value is JValue regex &&
                         regex.Type == JTokenType.String &&
                         regex.Value is string regexString:
                    matcher = new Matcher.Regex(regexString);
                    break;
            }

            return matcher;
        }
    }
}