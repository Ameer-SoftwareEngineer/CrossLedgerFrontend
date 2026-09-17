namespace CrossLedgerFrontend.Wallets;

/// <summary>A curated subset of the currencies CrossLedgerWeb's Currency value object
/// accepts (it supports far more) - enough to exercise both the two-decimal common case
/// and JPY's zero-decimal exception without overwhelming the wallet/send-money pickers.</summary>
public static class SupportedCurrencies
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "USD", "EUR", "GBP", "PKR", "AED", "INR", "JPY", "CAD", "AUD",
    };

    public static bool IsZeroDecimal(string currency) => currency == "JPY";

    public static string Format(decimal amount, string currency) =>
        amount.ToString(IsZeroDecimal(currency) ? "N0" : "N2");
}
