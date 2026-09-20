namespace CrossLedgerFrontend.Wallets;

/// <summary>Lets a page that just changed wallet balances (added a wallet, sent a
/// transfer) tell MainLayout's persistent sidebar balances to refresh - MainLayout isn't
/// remounted by SPA navigation, so a same-page mutation wouldn't otherwise be visible to
/// it until the user happened to navigate somewhere else.</summary>
public sealed class WalletChangeNotifier
{
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
