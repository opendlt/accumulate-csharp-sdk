namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Protocol-specific constants used throughout the SDK.
    /// </summary>
    public static class ProtocolConstants
    {
        /// <summary>
        /// Standard hash size in bytes (SHA-256).
        /// </summary>
        public const int HashSizeBytes = 32;
        
        /// <summary>
        /// Field numbers for AddCredits transaction.
        /// </summary>
        public static class AddCreditsFields
        {
            public const int Type = 1;
            public const int Recipient = 2;
            public const int Amount = 3;
            public const int Oracle = 4;
        }
        
        /// <summary>
        /// Field numbers for CreateToken transaction.
        /// </summary>
        public static class CreateTokenFields
        {
            public const int Type = 1;
            public const int Url = 2;
            public const int Symbol = 3;
            public const int Precision = 5;
            public const int Properties = 6;
            public const int SupplyLimit = 7;
        }
        
        /// <summary>
        /// Field numbers for WriteData transaction.
        /// </summary>
        public static class WriteDataFields
        {
            public const int Type = 1;
            public const int Entry = 2;
            public const int Scratch = 3;
            public const int WriteToState = 4;
        }
        
        /// <summary>
        /// Field numbers for WriteDataTo transaction.
        /// </summary>
        public static class WriteDataToFields
        {
            public const int Type = 1;
            public const int Recipient = 2;
            public const int Entry = 3;
        }
        
        /// <summary>
        /// Field numbers for UpdateKeyPage transaction.
        /// </summary>
        public static class UpdateKeyPageFields
        {
            public const int Type = 1;
            public const int Operation = 2;
        }
        
        /// <summary>
        /// Field numbers for TransactionHeader.
        /// </summary>
        public static class TransactionHeaderFields
        {
            public const int Principal = 1;
            public const int Initiator = 2;
            public const int Memo = 3;
            public const int Metadata = 4;
        }
        
        /// <summary>
        /// Field numbers for KeyPageOperation.
        /// </summary>
        public static class KeyPageOperationFields
        {
            public const int Type = 1;
            public const int Key = 2;
            public const int Owner = 3;
            public const int Threshold = 4;
            public const int TxnThreshold = 5;
        }
        
        /// <summary>
        /// Default values.
        /// </summary>
        public static class Defaults
        {
            public const int TokenPrecision = 8;
            public const int KeyPageThreshold = 1;
        }
    }
}