using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Acme.Net.Sdk;
using Acme.Net.Sdk.Helpers;
using Acme.Net.Sdk.Protocol;
using Acme.Net.Sdk.Protocol.Generated;
using Acme.Net.Sdk.Signing;
using Acme.Net.Sdk.Transactions;

/// <summary>
/// SDK Example 14: Header Metadata, Signature Memo/Data, and M-of-N Multi-Sig Co-Signing (V3).
///
/// Demonstrates the three capabilities wired through SmartSigner + MultiSig:
///   1. Transaction HEADER metadata (bytes)        — anchored structured tags
///   2. SIGNATURE memo (string) + data (bytes)     — per-signer reason + evidence hash
///   3. M-of-N co-signing of ONE transaction       — every signer signs the same tx hash
///
/// Mirrors the audit + multi-party authorization model used by the Verso integration.
/// Runs against Kermit testnet.
/// </summary>
class Program
{
    const string KermitBase = "https://kermit.accumulatenetwork.io";

    static async Task Main()
    {
        Console.WriteLine("=== SDK Example 14: Header Metadata + Signature Memo/Data + Multi-Sig ===\n");
        using var client = new Accumulate(KermitBase);
        var helper = new AccumulateHelper(client);

        // ---- Keys: one sponsor lite, two ADI signers (for 2-of-2) ----
        var liteKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var keyA = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519); // QA
        var keyB = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519); // Compliance

        var lid = Principal.ComputeUrl(liteKp.GetPublicKey()).String();
        var lta = Principal.ComputeUrl(liteKp.GetPublicKey(), new Url("acc://ACME")).String();

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var adiUrl = $"acc://csharp-ex14-{ts}.acme";
        var keyBookUrl = $"{adiUrl}/book";
        var keyPageUrl = $"{keyBookUrl}/1";
        var dataAccountUrl = $"{adiUrl}/mtrs";

        Console.WriteLine($"ADI: {adiUrl}\nData account: {dataAccountUrl}\n");

        // ---- Fund sponsor ----
        for (int i = 0; i < 5; i++)
        {
            try { await client.V2.FaucetAsync(lta); await Task.Delay(1500); } catch { /* retry */ }
        }
        long balance = await helper.PollForBalanceAsync(lta, timeout: TimeSpan.FromSeconds(60));
        if (balance <= 0) { Console.WriteLine("No faucet balance; aborting demo."); return; }

        var liteSigner = new SmartSigner(client.V3, liteKp, lid);
        int oracle = await helper.GetOracleAsync();

        await Submit(liteSigner, lta, TxBody.AddCredits(lid, AccumulateHelper.CreditsToAcme(20000, oracle).ToString(), oracle), "credits->lite");

        // ADI controlled initially by key A
        var keyAHash = Convert.ToHexString(SHA256.HashData(keyA.GetPublicKey())).ToLowerInvariant();
        await Submit(liteSigner, lta, TxBody.CreateIdentity(adiUrl, keyBookUrl, keyAHash), "create ADI");
        await Submit(liteSigner, lta, TxBody.AddCredits(keyPageUrl, AccumulateHelper.CreditsToAcme(10000, oracle).ToString(), oracle), "credits->key page");

        var signerA = new SmartSigner(client.V3, keyA, keyPageUrl);
        await Submit(signerA, adiUrl, TxBody.CreateDataAccount(dataAccountUrl), "create data account");

        // ============================================================
        // 1 + 2. Single-signer WriteData with HEADER METADATA and SIGNATURE MEMO/DATA
        // ============================================================
        Console.WriteLine("\n--- Audit write: header metadata + signature memo/data ---");
        var recordHashHex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("medkit MTR canonical record"))).ToLowerInvariant();
        var headerMetadata = Encoding.UTF8.GetBytes("{\"v\":1,\"tenant\":\"actineon\",\"stage\":\"fulfillment\"}");
        var evidenceHash = SHA256.HashData(Encoding.UTF8.GetBytes("qa-report.pdf"));

        var auditRes = await signerA.SignSubmitAndWaitAsync(
            principal: dataAccountUrl,
            body: TxBody.WriteData(new List<string> { recordHashHex }),
            memo: "mtrs|fulfillment|KIT-00042",          // header memo (tag 3)
            signatureMemo: "anchored by QA service",       // signature memo (tag 9)
            signatureData: evidenceHash,                    // signature data (tag 10)
            headerMetadata: headerMetadata);                // header metadata (tag 4)
        Console.WriteLine($"Audit write: {(auditRes.Success ? "OK " + auditRes.TxId : auditRes.Error)}");

        // ============================================================
        // 3. Make the key page 2-of-2, then CO-SIGN one transaction
        // ============================================================
        Console.WriteLine("\n--- Configure 2-of-2 key page (add key B, threshold 2) ---");
        var keyBHash = SHA256.HashData(keyB.GetPublicKey());
        await Submit(signerA, keyPageUrl, TxBody.UpdateKeyPage(new List<Dictionary<string, object?>> { TxBody.AddKeyOperation(Convert.ToHexString(keyBHash).ToLowerInvariant()) }), "add key B");
        await Submit(signerA, keyPageUrl, TxBody.UpdateKeyPage(new List<Dictionary<string, object?>> { TxBody.SetThresholdOperation(2) }), "set threshold 2");

        Console.WriteLine("\n--- Multi-party approval: QA + Compliance co-sign ONE transaction ---");
        var signerB = new SmartSigner(client.V3, keyB, keyPageUrl); // same key page, different key
        var approvalHashHex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("disposition approval KIT-00042"))).ToLowerInvariant();

        var submitResult = await MultiSig.SubmitAsync(
            client.V3,
            principal: dataAccountUrl,
            body: TxBody.WriteData(new List<string> { approvalHashHex }),
            initiator: new MultiSigParticipant(signerA, VoteType.Accept, "QA pass per SOP-7", SHA256.HashData(Encoding.UTF8.GetBytes("qa-evidence"))),
            coSigners: new[] { new MultiSigParticipant(signerB, VoteType.Accept, "PHI handling verified", SHA256.HashData(Encoding.UTF8.GetBytes("compliance-evidence"))) },
            headerMemo: "approval|disposition|KIT-00042");
        Console.WriteLine($"Co-signed approval submitted:\n{JsonSerializer.Serialize(submitResult, new JsonSerializerOptions { WriteIndented = true })}");

        Console.WriteLine("\nExample 14 COMPLETED.");
    }

    static async Task Submit(SmartSigner signer, string principal, Dictionary<string, object?> body, string label)
    {
        var r = await signer.SignSubmitAndWaitAsync(principal, body);
        Console.WriteLine($"  {label}: {(r.Success ? "OK" : r.Error)}");
        await Task.Delay(4000);
    }
}
