using Newtonsoft.Json;

namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Defines the interface for transaction body objects.
    /// </summary>
    [JsonConverter(typeof(TransactionBodyConverter))]
    public interface ITransactionBody : IMarshallable, IRPCBody
    {
    }

    /// <summary>
    /// JSON converter for transaction body objects that handles polymorphic deserialization based on the 'type' property.
    /// </summary>
    public class TransactionBodyConverter : JsonConverter
    {
        public override bool CanConvert(System.Type objectType)
        {
            return typeof(ITransactionBody).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            // TODO: Uncomment this when Generated.Protocol types are available
            /*
            var jObject = Newtonsoft.Json.Linq.JObject.Load(reader);
            var typeProperty = jObject["type"]?.ToString();

            ITransactionBody transactionBody = typeProperty switch
            {
                "createIdentity" => new Generated.Protocol.CreateIdentity(),
                "createTokenAccount" => new Generated.Protocol.CreateTokenAccount(),
                "sendTokens" => new Generated.Protocol.SendTokens(),
                "createDataAccount" => new Generated.Protocol.CreateDataAccount(),
                "writeData" => new Generated.Protocol.WriteData(),
                "writeDataTo" => new Generated.Protocol.WriteDataTo(),
                "acmeFaucet" => new Generated.Protocol.AcmeFaucet(),
                "createToken" => new Generated.Protocol.CreateToken(),
                "issueTokens" => new Generated.Protocol.IssueTokens(),
                "burnTokens" => new Generated.Protocol.BurnTokens(),
                "createKeyPage" => new Generated.Protocol.CreateKeyPage(),
                "createKeyBook" => new Generated.Protocol.CreateKeyBook(),
                "addCredits" => new Generated.Protocol.AddCredits(),
                "updateKeyPage" => new Generated.Protocol.UpdateKeyPage(),
                "updateAccountAuth" => new Generated.Protocol.UpdateAccountAuth(),
                "updateKey" => new Generated.Protocol.UpdateKey(),
                "remote" => new Generated.Protocol.RemoteTransaction(),
                "syntheticCreateIdentity" => new Generated.Protocol.SyntheticCreateIdentity(),
                "syntheticWriteData" => new Generated.Protocol.SyntheticWriteData(),
                "syntheticDepositTokens" => new Generated.Protocol.SyntheticDepositTokens(),
                "syntheticDepositCredits" => new Generated.Protocol.SyntheticDepositCredits(),
                "syntheticBurnTokens" => new Generated.Protocol.SyntheticBurnTokens(),
                "syntheticForwardTransaction" => new Generated.Protocol.SyntheticForwardTransaction(),
                "systemGenesis" => new Generated.Protocol.SystemGenesis(),
                "systemWriteData" => new Generated.Protocol.SystemWriteData(),
                "blockValidatorAnchor" => new Generated.Protocol.BlockValidatorAnchor(),
                "directoryAnchor" => new Generated.Protocol.DirectoryAnchor(),
                _ => throw new JsonSerializationException($"Unknown transaction body type: {typeProperty}")
            };

            serializer.Populate(jObject.CreateReader(), transactionBody);
            return transactionBody;
            */

            // Temporary implementation until Generated.Protocol types are available
            throw new System.NotImplementedException("Transaction body deserialization not implemented yet");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override bool CanWrite => false;
    }
}
