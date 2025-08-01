namespace Acme.Net.Sdk.Protocol
{
    /// <summary>
    /// Defines the numeric type codes for transaction types in the Accumulate protocol.
    /// These codes are used in the binary marshalling format.
    /// </summary>
    public static class TransactionTypeCode
    {
        /// <summary>
        /// Unknown represents an unknown transaction type.
        /// </summary>
        public const int Unknown = 0;

        /// <summary>
        /// CreateIdentity creates an ADI, which produces a synthetic chain.
        /// </summary>
        public const int CreateIdentity = 1;

        /// <summary>
        /// CreateTokenAccount creates an ADI token account, which produces a synthetic chain create transaction.
        /// </summary>
        public const int CreateTokenAccount = 2;

        /// <summary>
        /// SendTokens transfers tokens between token accounts, which produces a synthetic deposit tokens transaction.
        /// </summary>
        public const int SendTokens = 3;

        /// <summary>
        /// CreateDataAccount creates an ADI Data Account, which produces a synthetic chain create transaction.
        /// </summary>
        public const int CreateDataAccount = 4;

        /// <summary>
        /// WriteData writes data to an ADI Data Account, which does not produce a synthetic transaction.
        /// </summary>
        public const int WriteData = 5;

        /// <summary>
        /// WriteDataTo writes data to a Lite Data Account, which produces a synthetic write data transaction.
        /// </summary>
        public const int WriteDataTo = 6;

        /// <summary>
        /// AcmeFaucet produces a synthetic deposit tokens transaction that deposits ACME tokens into a lite token account.
        /// </summary>
        public const int AcmeFaucet = 7;

        /// <summary>
        /// CreateToken creates a token issuer, which produces a synthetic chain create transaction.
        /// </summary>
        public const int CreateToken = 8;

        /// <summary>
        /// IssueTokens issues tokens to a token account, which produces a synthetic token deposit transaction.
        /// </summary>
        public const int IssueTokens = 9;

        /// <summary>
        /// BurnTokens burns tokens from a token account, which produces a synthetic burn tokens transaction.
        /// </summary>
        public const int BurnTokens = 10;

        /// <summary>
        /// CreateLiteTokenAccount creates a lite token account.
        /// </summary>
        public const int CreateLiteTokenAccount = 11;

        /// <summary>
        /// CreateKeyPage creates a key page, which produces a synthetic chain create transaction.
        /// </summary>
        public const int CreateKeyPage = 12;

        /// <summary>
        /// CreateKeyBook creates a key book, which produces a synthetic chain create transaction.
        /// </summary>
        public const int CreateKeyBook = 13;

        /// <summary>
        /// AddCredits converts ACME tokens to credits, which produces a synthetic deposit credits transaction.
        /// </summary>
        public const int AddCredits = 14;

        /// <summary>
        /// UpdateKeyPage adds, removes, or updates keys in a key page, which does not produce a synthetic transaction.
        /// </summary>
        public const int UpdateKeyPage = 15;

        /// <summary>
        /// Remote is used to sign a remote transaction (SignPending).
        /// </summary>
        public const int Remote = 48;

        /// <summary>
        /// SyntheticCreateIdentity creates an identity.
        /// </summary>
        public const int SyntheticCreateIdentity = 49;

        /// <summary>
        /// SyntheticWriteData writes data to a data account.
        /// </summary>
        public const int SyntheticWriteData = 50;

        /// <summary>
        /// SyntheticDepositTokens deposits tokens into token accounts.
        /// </summary>
        public const int SyntheticDepositTokens = 51;

        /// <summary>
        /// SyntheticDepositCredits deposits credits into a credit holder.
        /// </summary>
        public const int SyntheticDepositCredits = 52;

        /// <summary>
        /// SyntheticBurnTokens returns tokens to a token issuer's pool of issuable tokens.
        /// </summary>
        public const int SyntheticBurnTokens = 53;
    }
}