using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Protocol.Generated.Protocol;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;
using Acme.Net.Sdk.Rpc;
using System.Security.Cryptography;
using SigningISignature = Acme.Net.Sdk.Signing.ISignature;

public class SendAdiToAdiExample
{
    public static async Task Main(string[] args)
    {
        // enable SDK-wide JSON logging to console
        RPCClient.LogSink = Console.WriteLine;

        var client = new AcmeClient("https://mainnet.accumulatenetwork.io/v2");

        string sourceAccountUrl    = "acc://0test1test01.acme/tokens";
        string recipientAccountUrl = "acc://0test1test01.acme/staking";

        // 🔑 Replace with your actual 32-byte Ed25519 private key (hex)
        string privateKeyHex = "e81b0bb64ab68b5af018eef638661f50afc72dc16816f859c095c2c8a0b58c7b";
        byte[] privateKey = Convert.FromHexString(privateKeyHex);

        if (!Acme.Net.Sdk.Signing.SignatureKeyPair.TryImportFromSecretKeyBytes(
                privateKey, SignatureType.ED25519, out var keyPair))
            throw new InvalidOperationException("Failed to import Ed25519 private key");

        // --- Fixed timestamp in MICROSECONDS since Unix epoch ---
        const long FixedTimestampMicros = 1757520686204512L;

        var signer = new Signer()
            .WithKeyPair(keyPair)
            .WithUrl(new Url("acc://0test1test01.acme/book/1")) // Key Page URL
            .WithVersion(51)                                     // Key page version
            .WithType(SignatureType.ED25519)
            .WithTimeStamp(FixedTimestampMicros);

        ulong amountAtoms = 200_000UL; // 0.002 ACME

        var sendTokensBuilder = (SendTokensBuilder)client.CreateSendTokensBuilder();
        sendTokensBuilder.WithOrigin(sourceAccountUrl);
        sendTokensBuilder.WithSigner(signer);
        sendTokensBuilder.AddRecipient(recipientAccountUrl, amountAtoms);

        // ---------- OPTIONAL: Preview + Envelope JSON ----------
        Console.WriteLine("=== Debug Preview ===");
        var buildMethod = typeof(SendTokensBuilder)
            .GetMethod("BuildTransactionBody", BindingFlags.Instance | BindingFlags.NonPublic);
        var body = (ITransactionBody)buildMethod!.Invoke(sendTokensBuilder, null)!;

        var txPreview = new Transaction
        {
            Header = new TransactionHeader().WithPrincipal(new Url(sourceAccountUrl)),
            Body   = body
        };

        // Initiate with the SAME signer (fixed timestamp baked in)
        var sig = signer.Initiate(txPreview);
        var bs  = (BaseSignature)sig; // for convenience
        var signedTxHash = bs.TransactionHash;

        // Local Ed25519 verify: verify the SAME combined digest the signer used
        try
        {
            // sig-metadata hash = sha256(encode(signature metadata))
            var sigMeta = ((SigningISignature)sig).MarshalMetadata();
            var sigMdHash = SHA256.HashData(sigMeta);

            // combined digest = sha256(sigMdHash || txHash)
            var concat = new byte[sigMdHash.Length + signedTxHash.Length];
            Buffer.BlockCopy(sigMdHash, 0, concat, 0, sigMdHash.Length);
            Buffer.BlockCopy(signedTxHash, 0, concat, sigMdHash.Length, signedTxHash.Length);
            var toVerify = SHA256.HashData(concat);

            // NSec verify over the combined digest
            var alg = NSec.Cryptography.SignatureAlgorithm.Ed25519;
            var pub = NSec.Cryptography.PublicKey.Import(
                alg, bs.PublicKey, NSec.Cryptography.KeyBlobFormat.RawPublicKey);

            bool ok = alg.Verify(pub, toVerify, bs.SignatureBytes);

            Console.WriteLine("Local Ed25519 verify (combined digest): " + (ok ? "OK" : "FAILED"));
            Console.WriteLine("toSign (sha256(md||txHash)) = " + Convert.ToHexString(toVerify).ToLowerInvariant());
            Console.WriteLine("Sig (hex): " + Convert.ToHexString(bs.SignatureBytes).ToLowerInvariant());
            Console.WriteLine("Fixed timestamp (µs): " + FixedTimestampMicros);
        }
        catch (Exception e)
        {
            Console.WriteLine("Local Ed25519 verify threw: " + e.Message);
        }

        var signerPubBytes = keyPair.GetPublicKey();
        Console.WriteLine("Signer public key (hex): " + Convert.ToHexString(signerPubBytes).ToLowerInvariant());
        Console.WriteLine($"TxHash (hex): {ToHex(signedTxHash)}");

        // Build the SAME envelope you’ll send and print as JSON
        var envelope = new EnvelopeBuilder()
            .AddTransaction(txPreview)
            .AddSignature(sig)
            .Build();

        var jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        string envelopeJson = JsonSerializer.Serialize(envelope, jsonOpts);
        Console.WriteLine("--- ENVELOPE JSON (to be submitted) ---");
        Console.WriteLine(envelopeJson);
        Console.WriteLine("--- END ENVELOPE JSON ---");
        Console.WriteLine("=== End Debug Preview ===");
        // ---------- END PREVIEW ----------

        Console.WriteLine("Submitting transaction...");
        try
        {
            var response = await sendTokensBuilder.ExecuteSignedAsync();
            Console.WriteLine($"Transaction ID: {response.TxId}");
        }
        catch (RPCException rex)
        {
            Console.WriteLine("=== RPC Exception ===");
            Console.WriteLine($"RPC Code:    {rex.Code}");
            Console.WriteLine($"RPC Message: {rex.Message}");
            if (rex.Data != null) Console.WriteLine($"RPC Data:    {rex.Data}");
            Console.WriteLine("======================");
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== Exception ===");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("=================");
        }
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new System.Text.StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
