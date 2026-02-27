namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Defines the numeric type codes for transaction types in the Accumulate protocol.
    /// These codes are used in the binary marshalling format.
    /// Values match the authoritative Go core / Rust SDK definitions.
    /// </summary>
    public static class TransactionTypeCode
    {
        // ---- User transactions ----

        public const int Unknown = 0;
        public const int CreateIdentity = 1;
        public const int CreateTokenAccount = 2;
        public const int SendTokens = 3;
        public const int CreateDataAccount = 4;
        public const int WriteData = 5;
        public const int WriteDataTo = 6;
        public const int AcmeFaucet = 7;
        public const int CreateToken = 8;
        public const int IssueTokens = 9;
        public const int BurnTokens = 10;
        public const int CreateLiteTokenAccount = 11;
        public const int CreateKeyPage = 12;
        public const int CreateKeyBook = 13;
        public const int AddCredits = 14;
        public const int UpdateKeyPage = 15;
        public const int LockAccount = 16;
        public const int BurnCredits = 17;
        public const int TransferCredits = 18;
        // 19-20 unused
        public const int UpdateAccountAuth = 21;
        public const int UpdateKey = 22;

        // ---- Network/system transactions ----

        public const int NetworkMaintenance = 46;
        public const int ActivateProtocolVersion = 47;
        public const int Remote = 48;

        // ---- Synthetic transactions ----

        public const int SyntheticCreateIdentity = 49;
        public const int SyntheticWriteData = 50;
        public const int SyntheticDepositTokens = 51;
        public const int SyntheticDepositCredits = 52;
        public const int SyntheticBurnTokens = 53;
        public const int SyntheticForwardTransaction = 54;

        // ---- System transactions ----

        public const int SystemGenesis = 96;
        public const int DirectoryAnchor = 97;
        public const int BlockValidatorAnchor = 98;
        public const int SystemWriteData = 99;

        private static readonly Dictionary<int, string> _codeToApiName;
        private static readonly Dictionary<string, int> _apiNameToCode;

        static TransactionTypeCode()
        {
            _codeToApiName = new Dictionary<int, string>
            {
                [Unknown] = "unknown",
                [CreateIdentity] = "createIdentity",
                [CreateTokenAccount] = "createTokenAccount",
                [SendTokens] = "sendTokens",
                [CreateDataAccount] = "createDataAccount",
                [WriteData] = "writeData",
                [WriteDataTo] = "writeDataTo",
                [AcmeFaucet] = "acmeFaucet",
                [CreateToken] = "createToken",
                [IssueTokens] = "issueTokens",
                [BurnTokens] = "burnTokens",
                [CreateLiteTokenAccount] = "createLiteTokenAccount",
                [CreateKeyPage] = "createKeyPage",
                [CreateKeyBook] = "createKeyBook",
                [AddCredits] = "addCredits",
                [UpdateKeyPage] = "updateKeyPage",
                [UpdateAccountAuth] = "updateAccountAuth",
                [UpdateKey] = "updateKey",
                [LockAccount] = "lockAccount",
                [TransferCredits] = "transferCredits",
                [BurnCredits] = "burnCredits",
                [NetworkMaintenance] = "networkMaintenance",
                [ActivateProtocolVersion] = "activateProtocolVersion",
                [Remote] = "remote",
                [SyntheticCreateIdentity] = "syntheticCreateIdentity",
                [SyntheticWriteData] = "syntheticWriteData",
                [SyntheticDepositTokens] = "syntheticDepositTokens",
                [SyntheticDepositCredits] = "syntheticDepositCredits",
                [SyntheticBurnTokens] = "syntheticBurnTokens",
                [SyntheticForwardTransaction] = "syntheticForwardTransaction",
                [SystemGenesis] = "systemGenesis",
                [DirectoryAnchor] = "directoryAnchor",
                [BlockValidatorAnchor] = "blockValidatorAnchor",
                [SystemWriteData] = "systemWriteData",
            };

            _apiNameToCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _codeToApiName)
            {
                _apiNameToCode[kvp.Value] = kvp.Key;
            }
        }

        /// <summary>
        /// Gets the camelCase API/wire name for a transaction type code.
        /// Returns "unknown" if the code is not recognized.
        /// </summary>
        public static string GetApiName(int code)
        {
            if (_codeToApiName.TryGetValue(code, out var name))
                return name;
            return "unknown";
        }

        /// <summary>
        /// Gets the transaction type code for a camelCase API/wire name.
        /// Returns <see cref="Unknown"/> (0) if the name is not recognized.
        /// </summary>
        public static int FromApiName(string name)
        {
            if (name != null && _apiNameToCode.TryGetValue(name, out var code))
                return code;
            return Unknown;
        }
    }
}
