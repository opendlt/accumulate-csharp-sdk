using System.Numerics;

namespace Acme.Net.Sdk;

/// <summary>
/// ACME amount helper. Accumulate denominates ACME in <em>base units</em> where
/// <b>1 ACME = 1e8 base units</b>. Passing whole ACME where base units are
/// expected is the single most common integration bug; use this type to convert
/// explicitly.
/// </summary>
/// <example>
/// <code>
/// var body = TxBody.SendTokens("acc://bob.acme/tokens", Amount.Acme(5).ToWire()); // "500000000"
/// </code>
/// </example>
public readonly struct Amount
{
    /// <summary>Base units in one whole ACME (1e8).</summary>
    public const long AcmeBaseUnits = 100_000_000L;

    /// <summary>Number of decimal places in ACME.</summary>
    public const int AcmePrecision = 8;

    /// <summary>The amount as an integer number of base units.</summary>
    public BigInteger BaseUnits { get; }

    private Amount(BigInteger baseUnits) => BaseUnits = baseUnits;

    /// <summary>Create from whole ACME. <c>Amount.Acme(1)</c> == 1e8 base units.</summary>
    public static Amount Acme(decimal wholeAcme) => new((BigInteger)(wholeAcme * AcmeBaseUnits));

    /// <summary>Create from raw base units.</summary>
    public static Amount FromBaseUnits(BigInteger units) => new(units);

    /// <summary>Create from raw base units expressed as a string (the wire form).</summary>
    public static Amount FromBaseUnits(string units) => new(BigInteger.Parse(units));

    /// <summary>
    /// Create from whole units of a <b>custom token</b> with the given precision.
    /// </summary>
    /// <remarks>
    /// Custom tokens declare their own precision at creation; the wire format is
    /// always base units. <c>Amount.Token(1000, 8)</c> is 1000 whole tokens =
    /// <c>100000000000</c> base units.
    /// <para>
    /// Without this the only options are hand-computing a power of ten or passing
    /// a raw base-unit string, and both are routinely got wrong: issuing
    /// <c>1000</c> against a precision-8 token mints <c>0.00001</c> tokens, not
    /// 1000 — and the transaction succeeds either way, so the mistake is silent.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Amount.Token(1000, 8).ToWire(); // "100000000000"
    /// Amount.Token(100, 2).ToWire();  // "10000"
    /// Amount.Token(1000, 0).ToWire(); // "1000"
    /// </code>
    /// </example>
    public static Amount Token(decimal wholeTokens, int precision) =>
        new((BigInteger)(wholeTokens * (decimal)BigInteger.Pow(10, precision)));

    /// <summary>The amount in whole units of a token with the given precision.</summary>
    public decimal ToToken(int precision) =>
        (decimal)BaseUnits / (decimal)BigInteger.Pow(10, precision);

    /// <summary>
    /// ACME base units needed to buy <paramref name="creditCount"/> credits at
    /// <paramref name="oraclePrice"/> (the integer oracle value from the network).
    /// </summary>
    public static Amount Credits(long creditCount, long oraclePrice) =>
        new(new BigInteger(creditCount) * AcmeBaseUnits * 100 / oraclePrice);

    /// <summary>Wire representation: base units as a string (what TxBody expects).</summary>
    public string ToWire() => BaseUnits.ToString();

    /// <inheritdoc />
    public override string ToString() => ToWire();

    /// <summary>The amount expressed in whole ACME.</summary>
    public decimal ToAcme() => (decimal)BaseUnits / AcmeBaseUnits;
}
