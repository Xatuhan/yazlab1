using System;

namespace YazLab.NpcDialogue
{

    [Serializable]
    internal class OllamaChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public bool stream;
        public bool think;
        public ReplySchema format = new ReplySchema();
        public string keep_alive;
        public OllamaOptions options = new OllamaOptions();
    }

    [Serializable]
    internal class OllamaOptions
    {
        public int num_ctx;
        public int num_predict;
        public float temperature;
    }

    [Serializable]
    internal class ReplySchema
    {
        public string type = "object";
        public SchemaProperties properties = new SchemaProperties();
        public string[] required = { "action", "reply" };
    }

    [Serializable]
    internal class SchemaProperties
    {
        public EnumProperty action = new EnumProperty();
        public StringProperty reply = new StringProperty();
    }

    [Serializable]
    internal class EnumProperty
    {
        public string type = "string";
        public string[] @enum = { "allow", "deny", "attack" };
    }

    [Serializable]
    internal class StringProperty
    {
        public string type = "string";
    }

    [Serializable]
    internal class OllamaChatResponse
    {
        public ChatMessage message;
        public string error;
    }
}
