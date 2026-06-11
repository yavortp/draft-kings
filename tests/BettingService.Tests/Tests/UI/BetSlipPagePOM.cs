using Microsoft.Playwright;

namespace BettingService.Tests.Pages;

public class BetSlipPagePOM
{
    private readonly IPage _page;

    public BetSlipPagePOM(IPage page)
    {
        _page = page;
    }

    // Locators
    public ILocator SelectionButtons => _page.Locator(".selection-btn");
    public ILocator PlaceBetButton => _page.Locator("#place-bet-btn");
    public ILocator StakeInput => _page.Locator("#stake-input");
    public ILocator BetConfirmation => _page.Locator("#bet-confirmation");
    public ILocator BetSlipSelectionName => _page.Locator("#betslip-selection-name");
    public ILocator BetSlipOdds => _page.Locator("#betslip-odds");
    public ILocator PotentialPayout => _page.Locator("#potential-payout");
    public ILocator UserBalance => _page.GetByTestId("user-balance");
    public ILocator BetsList => _page.Locator("#bets-list");

    // Helper methods
    public async Task GoToAsync(string baseUrl)
    {
        await _page.GotoAsync(baseUrl);
        await _page.WaitForSelectorAsync(".selection-btn");
    }

    public async Task SelectFirstBetAsync()
    {
        await SelectionButtons.First.ClickAsync();
    }

    public async Task EnterStakeAsync(string amount)
    {
        await StakeInput.FillAsync(amount);
    }

    public async Task PlaceBetAsync()
    {
        await PlaceBetButton.ClickAsync();
    }

    public async Task<string> GetSelectionNameAsync(IElementHandle selection)
    {
        var element = await selection.QuerySelectorAsync(".selection-name");
        return await element!.TextContentAsync() ?? string.Empty;
    }

    public async Task<string> GetSelectionOddsAsync(IElementHandle selection)
    {
        var element = await selection.QuerySelectorAsync(".selection-odds");
        return await element!.TextContentAsync() ?? string.Empty;
    }

    public async Task<(string name, string odds)> GetSelectionDataAsync(IElementHandle selection)
    {
        var nameElement = await selection.QuerySelectorAsync(".selection-name");
        var oddsElement = await selection.QuerySelectorAsync(".selection-odds");
        var name = await nameElement!.TextContentAsync() ?? string.Empty;
        var odds = await oddsElement!.TextContentAsync() ?? string.Empty;
        return (name, odds);
    }

    public async Task PressArrowUpOnStakeAsync() => await _page.Keyboard.PressAsync("ArrowUp");
    public async Task PressArrowDownOnStakeAsync() => await _page.Keyboard.PressAsync("ArrowDown");

    public string ExpectedPayout(decimal stake, string oddsText)
        => $"£{(stake * decimal.Parse(oddsText)):F2}";
}