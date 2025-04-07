using Newtonsoft.Json;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Interface for signature objects used in the Acme protocol.
    /// </summary>
    [JsonConverter(typeof(SignatureConverter))]
    public interface ISignature
    {
    }

    /// <summary>
    /// JSON converter for signature objects that handles polymorphic deserialization based on the 'type' property.
    /// </summary>
    public class SignatureConverter : JsonConverter
    {
        public override bool CanConvert(System.Type objectType)
        {
            return typeof(ISignature).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, System.Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            // TODO: Uncomment this when Generated.Protocol types are available
            /*
            var jObject = Newtonsoft.Json.Linq.JObject.Load(reader);
            var typeProperty = jObject["type"]?.ToString();

            ISignature signature = typeProperty switch
            {
                "ed25519" => new Generated.Protocol.ED25519Signature(),
                "rcd1" => new Generated.Protocol.RCD1Signature(),
                "receipt" => new Generated.Protocol.ReceiptSignature(),
                "partition" => new Generated.Protocol.PartitionSignature(),
                "internal" => new Generated.Protocol.InternalSignature(),
                "authority" => new Generated.Protocol.AuthoritySignature(),
                _ => throw new JsonSerializationException($"Unknown signature type: {typeProperty}")
            };

            serializer.Populate(jObject.CreateReader(), signature);
            return signature;
            */
            
            // Temporary implementation until Generated.Protocol types are available
            throw new System.NotImplementedException("Signature deserialization not implemented yet");
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override bool CanWrite => false;
    }
}
