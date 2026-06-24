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
/// SDK Example 15: Independent / asynchronous M-of-N — "sign pending" (V3, Kermit).
///
/// This is the LIVE verification of the sign-pending capability that lets independent authorities
/// (who hold their own keys and sign at different times) approve one transaction:
///
///   1. Configure a 2-of-2 key page (key A = QA, key B = Compliance).
///   2. INITIATOR (A) submits a WriteData approval via MultiSig.InitiateAsync → transaction is
///      PENDING (1 of 2 signatures). A only shares the transaction HASH + principal.
///   3. Confirm it is pending: QueryPendingAsync on the principal + QueryTransactionAsync.
///   4. CO-SIGNER (B), as if from a separate wallet/process and WITHOUT the original body, signs the
///      pending transaction by hash via SmartSigner.SignRemoteSubmitAndWaitAsync.
///   5. Confirm the transaction is now DELIVERED (threshold reached, executed).
///
/// Mirrors the Verso multi-party authorization flow where each authority signs from their own wallet.
/// </summary>
class Program
{
    static readonly string KermitBase = System.Environment.GetEnvironmentVariable("ACCUMULATE_BASE_URL") ?? "https://kermit.accumulatenetwork.io";

    static async Task<int> Main()
    {
        Console.WriteLine("=== SDK Example 15: Sign-Pending Independent M-of-N (C#) ===\n");
        using var client = new Accumulate(KermitBase);
        var helper = new AccumulateHelper(client);

        var liteKp = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519);
        var keyA = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519); // QA (initiator)
        var keyB = AccKeyPairGenerator.GenerateSignatureKeyPair(SignatureType.ED25519); // Compliance (independent co-signer)

        var lid = Principal.ComputeUrl(liteKp.GetPublicKey()).String();
        var lta = Principal.ComputeUrl(liteKp.GetPublicKey(), new Url("acc://ACME")).String();

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var adiUrl = $"acc://csharp-ex15-{ts}.acme";
        var keyBookUrl = $"{adiUrl}/book";
        var keyPageUrl = $"{keyBookUrl}/1";
        var dataAccountUrl = $"{adiUrl}/approvals";

        Console.WriteLine($"ADI: {adiUrl}\nData account: {dataAccountUrl}\nKey page: {keyPageUrl}\n");

        // ---- Fund sponsor ----
        for (int i = 0; i < 5; i++)
        {
            try { await client.V2.FaucetAsync(lta); await Task.Delay(1500); } catch { /* retry */ }
        }
        long balance = await helper.PollForBalanceAsync(lta, timeout: TimeSpan.FromSeconds(60));
        if (balance <= 0) { Console.WriteLine("No faucet balance; aborting demo."); return 1; }

        var liteSigner = new SmartSigner(client.V3, liteKp, lid);
        int oracle = await helper.GetOracleAsync();
        await Submit(liteSigner, lta, TxBody.AddCredits(lid, AccumulateHelper.CreditsToAcme(20000, oracle).ToString(), oracle), "credits->lite");

        // ---- ADI controlled initially by key A, then make the page 2-of-2 ----
        var keyAHash = Convert.ToHexString(SHA256.HashData(keyA.GetPublicKey())).ToLowerInvariant();
        await Submit(liteSigner, lta, TxBody.CreateIdentity(adiUrl, keyBookUrl, keyAHash), "create ADI");
        await Submit(liteSigner, lta, TxBody.AddCredits(keyPageUrl, AccumulateHelper.CreditsToAcme(20000, oracle).ToString(), oracle), "credits->key page");

        var signerA = new SmartSigner(client.V3, keyA, keyPageUrl);
        await Submit(signerA, adiUrl, TxBody.CreateDataAccount(dataAccountUrl), "create data account");

        var keyBHash = Convert.ToHexString(SHA256.HashData(keyB.GetPublicKey())).ToLowerInvariant();
        await Submit(signerA, keyPageUrl, TxBody.UpdateKeyPage(new() { TxBody.AddKeyOperation(keyBHash) }), "add key B");
        await Submit(signerA, keyPageUrl, TxBody.UpdateKeyPage(new() { TxBody.SetThresholdOperation(2) }), "set threshold 2");

        // ============================================================
        // Step 1: INITIATOR (A) makes the approval PENDING (1 of 2)
        // ============================================================
        Console.WriteLine("\n--- Step 1: Initiator (QA) submits — leaves tx PENDING (1 of 2) ---");
        var approvalHashHex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("disposition approval KIT-00042"))).ToLowerInvariant();

        var initiated = await MultiSig.InitiateAsync(
            principal: dataAccountUrl,
            body: TxBody.WriteData(new List<string> { approvalHashHex }),
            initiator: new MultiSigParticipant(signerA, VoteType.Accept, "QA pass per SOP-7",
                SHA256.HashData(Encoding.UTF8.GetBytes("qa-evidence"))),
            headerMemo: "approval|disposition|KIT-00042");

        var submit1 = await client.V3.SubmitAsync(initiated.Envelope);
        Console.WriteLine($"  Initiator submitted. Tx hash: {initiated.TransactionHashHex}");
        Console.WriteLine($"  (Initiator shares ONLY this hash + principal '{initiated.Principal}' with co-signers.)");
        await Task.Delay(4000);

        // ============================================================
        // Step 2: Confirm it is PENDING (capability b — query pending/signatures)
        // ============================================================
        Console.WriteLine("\n--- Step 2: Confirm pending state (QueryPending + QueryTransaction) ---");
        try
        {
            var pending = await client.V3.QueryPendingAsync(dataAccountUrl, new Acme.Net.Sdk.V3.RangeOptions { Start = 0, Count = 10 });
            Console.WriteLine($"  Pending on {dataAccountUrl}:\n{Format(pending)}");
        }
        catch (Exception ex) { Console.WriteLine($"  QueryPending: {ex.Message}"); }

        var txId = $"acc://{initiated.TransactionHashHex}@{dataAccountUrl.Substring("acc://".Length)}";
        try
        {
            var txBefore = await client.V3.QueryTransactionAsync(txId);
            Console.WriteLine($"  Transaction before co-sign (expect pending / 1 signature):\n{Format(txBefore)}");
        }
        catch (Exception ex) { Console.WriteLine($"  QueryTransaction: {ex.Message}"); }

        // ============================================================
        // Step 3: CO-SIGNER (B) signs the PENDING tx — independent / asynchronous
        // ============================================================
        var signerB = new SmartSigner(client.V3, keyB, keyPageUrl);
        var evidenceB = SHA256.HashData(Encoding.UTF8.GetBytes("compliance-evidence"));

        // 3a. Hash-only path (sign-pending by hash; no original body). Print the raw submit result.
        Console.WriteLine("\n--- Step 3a: Co-signer signs by HASH only (remote / sign-pending) ---");
        try
        {
            var remoteResp = await signerB.SignRemoteAndSubmitAsync(
                initiated.TransactionHash, initiated.Principal,
                vote: VoteType.Accept, signatureMemo: "PHI handling verified", signatureData: evidenceB);
            Console.WriteLine($"  Remote submit response:\n{Format(remoteResp)}");
        }
        catch (Exception ex) { Console.WriteLine($"  Remote submit threw: {ex.Message}"); }
        await Task.Delay(6000);

        bool delivered = await IsDelivered(client, txId);
        bool deliveredByRemote = delivered;

        // 3b. If hash-only did not complete it, co-sign with the FULL transaction (robust path:
        //     re-supply the initiator-set header+body, which the initiator shared out-of-band).
        if (!delivered)
        {
            Console.WriteLine("\n--- Step 3b: Co-signer signs the FULL shared transaction (robust path) ---");
            // Reset cached version (page may have advanced) before signing again.
            signerB.InvalidateCache();
            var fullResp = await signerB.CoSignAndSubmitAsync(
                initiated.Header, initiated.Body,
                vote: VoteType.Accept, signatureMemo: "PHI handling verified", signatureData: evidenceB);
            Console.WriteLine($"  Full-tx co-sign response:\n{Format(fullResp)}");
            await Task.Delay(6000);
        }

        // ============================================================
        // Step 4: Confirm DELIVERED (threshold reached, executed)
        // ============================================================
        Console.WriteLine("\n--- Step 4: Confirm the transaction executed (2 of 2) ---");
        for (int i = 0; i < 15 && !delivered; i++)
        {
            try
            {
                var txAfter = await client.V3.QueryTransactionAsync(txId);
                if (txAfter.TryGetProperty("status", out var st) && st.TryGetProperty("delivered", out var d) && d.ValueKind == JsonValueKind.True)
                {
                    delivered = true;
                    Console.WriteLine($"  DELIVERED. Final transaction:\n{Format(txAfter)}");
                    break;
                }
            }
            catch { /* keep polling */ }
            await Task.Delay(2000);
        }

        Console.WriteLine();
        if (delivered)
        {
            var path = deliveredByRemote ? "HASH-ONLY (remote / sign-pending)" : "FULL-TRANSACTION co-sign";
            Console.WriteLine($"VERIFIED: independent M-of-N round-trip succeeded (pending -> delivered) via the {path} path.");
            Console.WriteLine("Example 15 COMPLETED SUCCESSFULLY!");
            return 0;
        }

        Console.WriteLine("NOT VERIFIED: transaction did not reach 'delivered' after polling.");
        Console.WriteLine("  Inspect the Step 3a/3b submit responses above for the rejection reason.");
        return 2;
    }

    // A V3 transaction query reports status as a string ("pending"/"delivered") plus statusNo
    // (Delivered=201, Pending=202 — see Go core pkg/errors/status.yml).
    static async Task<bool> IsDelivered(Accumulate client, string txId)
    {
        try
        {
            var tx = await client.V3.QueryTransactionAsync(txId);
            if (tx.TryGetProperty("statusNo", out var n) && n.ValueKind == JsonValueKind.Number && n.GetInt32() == 201)
                return true;
            if (tx.TryGetProperty("status", out var st))
            {
                if (st.ValueKind == JsonValueKind.String)
                    return string.Equals(st.GetString(), "delivered", StringComparison.OrdinalIgnoreCase);
                if (st.ValueKind == JsonValueKind.Object && st.TryGetProperty("delivered", out var d) && d.ValueKind == JsonValueKind.True)
                    return true;
            }
            return false;
        }
        catch { return false; }
    }

    static async Task Submit(SmartSigner signer, string principal, Dictionary<string, object?> body, string label)
    {
        var r = await signer.SignSubmitAndWaitAsync(principal, body);
        Console.WriteLine($"  {label}: {(r.Success ? "OK" : r.Error)}");
        await Task.Delay(4000);
    }

    static string Format(JsonElement e) => JsonSerializer.Serialize(e, new JsonSerializerOptions { WriteIndented = true });
}
