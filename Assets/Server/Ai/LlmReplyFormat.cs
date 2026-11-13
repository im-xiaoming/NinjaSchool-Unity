using System;
using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>Offline LLM protocol, not gameplay: constrain decoding to the fields the parser understands.</summary>
    public static class LlmReplyFormat
    {
        private static Dictionary<string, object> Integer(int min, int max)
        { return new Dictionary<string, object> { { "type", "integer" }, { "minimum", min }, { "maximum", max } }; }
        public static object Build()
        {
            var stepProperties = new Dictionary<string, object> {
                { "goal", new Dictionary<string, object> { { "type", "string" }, { "enum", new[] { "quest", "hunt", "shop", "collect", "rest", "explore", "travel", "idle", "goto" } } } },
                { "map", Integer(-1, 10000) }, { "mob", Integer(-1, 10000) }, { "count", Integer(1, 30) }, { "seconds", Integer(5, 120) },
                { "x", Integer(-1, 20000) }, { "y", Integer(-1, 20000) }
            };
            var step = new Dictionary<string, object> { { "type", "object" }, { "additionalProperties", false }, { "properties", stepProperties },
                { "required", new[] { "goal", "map", "mob", "count", "seconds", "x", "y" } } };
            var weightProperties = new Dictionary<string, object>();
            foreach (var n in Priority.Names) weightProperties[n] = new Dictionary<string, object> { { "type", "number" }, { "minimum", 0 }, { "maximum", Priority.Max } };
            var weights = new Dictionary<string, object> { { "type", "object" }, { "additionalProperties", false }, { "properties", weightProperties },
                { "required", Priority.Names } };
            var schema = new Dictionary<string, object> {
                { "type", "object" }, { "additionalProperties", false }, { "required", new[] { "reason", "say", "fleeHp", "priorities", "steps" } },
                { "properties", new Dictionary<string, object> {
                    { "reason", new Dictionary<string, object> { { "type", "string" }, { "maxLength", 200 } } },
                    { "say", new Dictionary<string, object> { { "type", "string" }, { "maxLength", 80 } } },
                    { "fleeHp", Integer(10, 60) },
                    { "priorities", weights },
                    { "steps", new Dictionary<string, object> { { "type", "array" }, { "minItems", 1 }, { "maxItems", 4 }, { "items", step } } }
                } }
            };
            return new Dictionary<string, object> { { "type", "json_schema" }, { "json_schema", new Dictionary<string, object> {
                { "name", "nso_agent_advice" }, { "strict", true }, { "schema", schema } } } };
        }
    }

    public sealed class LlmInvalidReplyException : FormatException
    {
        public readonly string Response;
        public LlmInvalidReplyException(string error, string response) : base(error + " | " + LlmPlanner.Short(response, 160))
        { Response = response ?? ""; }
    }
}
