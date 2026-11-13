using System;

namespace NsoServer.Ai
{
    /// <summary>Offline LLM protocol, not gameplay: constrain decoding to the fields the parser understands.</summary>
    public static class LlmReplyFormat
    {
        // One checked-in schema is shared with Python; retained for existing diagnostic callers.
        public static object Build()
        {
            return NsoServer.Data.Json.Parse(System.IO.File.ReadAllText(BrainClient.Resource("reply_schema.json"), System.Text.Encoding.UTF8));
        }
    }

    public sealed class LlmInvalidReplyException : FormatException
    {
        public readonly string Response;
        public LlmInvalidReplyException(string error, string response) : base(error + " | " + LlmPlanner.Short(response, 160))
        {
            Response = response ?? "";
        }
    }
}
