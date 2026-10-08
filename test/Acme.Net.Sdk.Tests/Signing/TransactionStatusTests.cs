using System.Text.Json;
using Acme.Net.Sdk.Signing;

namespace Acme.Net.Sdk.Tests.Signing;

/// <summary>
/// SignSubmitAndWaitAsync must read the v3 status shape. Found against Kermit: v3 reports
/// <c>status</c> as a code NAME beside <c>statusNo</c> and an <c>error</c> object. Only an
/// object-shaped status was understood, so a transaction was never seen as delivered or as failed
/// and the wait fell through to "assume success" - which reported a rejected
/// ReleaseLockedOperation as a success. The JSON below is what Kermit actually returned.
/// </summary>
public class TransactionStatusTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void V3_Delivered_Record_Is_Delivered()
    {
        var state = SmartSigner.InterpretTransactionStatus(
            Parse("{\"status\":\"delivered\",\"statusNo\":201,\"error\":null}"), out var error);
        Assert.Equal(TransactionState.Delivered, state);
        Assert.Null(error);
    }

    [Fact]
    public void V3_Rejected_Record_Is_Failed_With_The_Nodes_Message()
    {
        var state = SmartSigner.InterpretTransactionStatus(Parse(
            "{\"status\":\"unauthenticated\",\"statusNo\":401,"
            + "\"error\":{\"message\":\"preimage does not match hash\",\"code\":\"unauthenticated\",\"codeID\":401}}"),
            out var error);
        Assert.Equal(TransactionState.Failed, state);
        Assert.Contains("preimage does not match hash", error);
        Assert.Contains("401", error);
    }

    [Theory]
    [InlineData("{\"status\":\"pending\",\"statusNo\":202}")]
    [InlineData("{\"status\":\"remote\",\"statusNo\":204}")]
    [InlineData("{}")]
    public void Not_Yet_Delivered_Stays_Pending(string json)
    {
        Assert.Equal(TransactionState.Pending, SmartSigner.InterpretTransactionStatus(Parse(json), out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Legacy_Object_Status_Still_Works()
    {
        Assert.Equal(TransactionState.Delivered,
            SmartSigner.InterpretTransactionStatus(Parse("{\"status\":{\"delivered\":true}}"), out _));
        var failed = SmartSigner.InterpretTransactionStatus(
            Parse("{\"status\":{\"delivered\":true,\"error\":{\"message\":\"boom\"}}}"), out var error);
        Assert.Equal(TransactionState.Failed, failed);
        Assert.Equal("boom", error);
        Assert.Equal(TransactionState.Pending,
            SmartSigner.InterpretTransactionStatus(Parse("{\"status\":{\"delivered\":false}}"), out _));
    }
}
