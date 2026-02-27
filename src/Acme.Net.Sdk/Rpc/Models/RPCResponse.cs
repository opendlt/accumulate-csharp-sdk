using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Acme.Net.Sdk.Api.V2;
using Acme.Net.Sdk.Rpc; // for RPCClient.LogSink

namespace Acme.Net.Sdk.Rpc.Models
{
    /// <summary>
    /// Represents a response from a JSON-RPC 2.0 API call.
    /// </summary>
    public class RPCResponse
    {
        // ---------- logging helpers ----------
        private static void Log(string msg) => RPCClient.LogSink?.Invoke(msg);

        private static string Pretty(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch { return json; }
        }

        private static string GetProp(JsonElement el, string name)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var p))
                return p.GetRawText();
            return "";
        }

        private static string GetPropAsString(JsonElement el, string name)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var p))
            {
                return p.ValueKind switch
                {
                    JsonValueKind.String => p.GetString() ?? "",
                    JsonValueKind.Number => p.GetRawText(),
                    JsonValueKind.True   => "true",
                    JsonValueKind.False  => "false",
                    _ => p.GetRawText()
                };
            }
            return "";
        }

        /// <summary>
        /// Logs a compact, human-friendly summary of the important result bits.
        /// </summary>
        private static void LogResultSummary(JsonElement result)
        {
            try
            {
                // Common fields on Accumulate results
                var txHash   = GetPropAsString(result, "transactionHash");
                var txid     = GetPropAsString(result, "txid");
                var simple   = GetPropAsString(result, "simpleHash");
                var hash     = GetPropAsString(result, "hash");
                var sigsJson = GetProp(result, "signatureHashes");

                if (!string.IsNullOrEmpty(txHash) || !string.IsNullOrEmpty(txid))
                {
                    Log($"RPC result: transactionHash={txHash}, txid={txid}, simpleHash={simple}, hash={hash}");
                }

                // Per-message results (array)
                if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("result", out var messages) &&
                    messages.ValueKind == JsonValueKind.Array)
                {
                    int i = 0;
                    foreach (var msg in messages.EnumerateArray())
                    {
                        var txID    = GetPropAsString(msg, "txID");
                        var code    = GetPropAsString(msg, "code");
                        var codeNum = GetPropAsString(msg, "codeNum");
                        Log($"RPC subresult[{i++}]: txID={txID}, code={code}, codeNum={codeNum}");
                    }
                }

                // Signature hashes if present
                if (!string.IsNullOrEmpty(sigsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(sigsJson);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            int n = 0;
                            foreach (var h in doc.RootElement.EnumerateArray())
                            {
                                Log($"RPC signatureHash[{n++}]: {h.GetRawText()}");
                            }
                        }
                    }
                    catch
                    {
                        Log($"RPC signatureHashes: {sigsJson}");
                    }
                }
            }
            catch
            {
                // Never let logging break the flow
            }
        }
        // ------------------------------------

        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("result")]
        public JsonElement? Result { get; set; }

        [JsonPropertyName("error")]
        public RPCError? Error { get; set; }

        public static RPCResponse From(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            RPCResponse? response = null;
            try
            {
                response = JsonSerializer.Deserialize<RPCResponse>(json, options);
            }
            catch (Exception ex)
            {
                Log($"*** RPC RESPONSE PARSE ERROR ***\n{ex}\nRaw:\n{json}\n*** END RESPONSE PARSE ERROR ***");
                throw;
            }

            if (response == null)
            {
                Log($"*** RPC RESPONSE NULL AFTER DESERIALIZE ***\nRaw:\n{json}\n*** END ***");
                throw new RPCException("Failed to parse RPC response");
            }

            // Detailed, structured logging
            if (response.Error != null)
            {
                var err = response.Error;
                Log($"RPC error summary: code={err.Code}, message={err.Message}");

                // FIX: handle nullable JsonElement properly
                if (err.Data.HasValue)
                {
                    var d = err.Data.Value;
                    var dataTxt = d.ValueKind == JsonValueKind.String
                        ? (d.GetString() ?? "")
                        : d.GetRawText();
                    Log($"RPC error.data: {dataTxt}");
                }

                // Pretty print the whole error object (safe)
                try
                {
                    var errJson = JsonSerializer.Serialize(err, new JsonSerializerOptions { WriteIndented = true });
                    Log($"RPC error object:\n{errJson}");
                }
                catch { /* ignore */ }

                // Throw after logging
                throw new RPCException(err);
            }

            if (response.Result.HasValue)
            {
                // Summary of important fields
                LogResultSummary(response.Result.Value);

                // Pretty-print the result (useful during troubleshooting)
                try
                {
                    var pretty = Pretty(response.Result.Value.GetRawText());
                    Log($"RPC result payload (pretty):\n{pretty}");
                }
                catch { /* ignore */ }
            }
            else
            {
                Log("RPC response has no 'result' and no 'error'.");
            }

            return response;
        }

        public TxResponse AsTransactionResponse()
        {
            if (!Result.HasValue)
            {
                Log("AsTransactionResponse: result is null");
                throw new InvalidOperationException("RPC response does not contain a result");
            }

            string resultJson = Result.Value.GetRawText();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            TxResponse? txResponse = null;
            try
            {
                txResponse = JsonSerializer.Deserialize<TxResponse>(resultJson, options);
            }
            catch (Exception ex)
            {
                Log($"*** TxResponse DESERIALIZE ERROR ***\n{ex}\nResult JSON:\n{Pretty(resultJson)}\n*** END ***");
                throw;
            }

            if (txResponse == null)
            {
                Log($"AsTransactionResponse: failed to parse result JSON:\n{Pretty(resultJson)}");
                throw new InvalidOperationException("Failed to parse transaction response");
            }

            return txResponse;
        }
    }
}
