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
