using BettingService.Tests.Pages;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BalanceUiTests : PlaywrightTest
{
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private IAPIRequestContext _api = null!;
    private ApiHelpers Api = null!;
    private BetSlipPagePOM BetSlip = null!;
    private readonly string _baseUrl = "http://localhost:5000";

    [SetUp]
    public async Task SetUp()
    {
        _api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl
        });

        Api = new ApiHelpers(_api);

        _browser = await Playwright.Chromium.LaunchAsync(new() { Headless = false });
        _page = await _browser.NewPageAsync();

        BetSlip = new BetSlipPagePOM(_page); 
        await BetSlip.GoToAsync(_baseUrl);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _page.CloseAsync();
        await _browser.CloseAsync();
        await _api.DisposeAsync();
    }

    [Test]
    public async Task Balance_Updates_In_UI_After_Bet_Is_Placed()
    {
        // Verify balance element is displayed
        await Expect(BetSlip.UserBalance).ToBeVisibleAsync();
        var initialBalance = await BetSlip.UserBalance.TextContentAsync();
        Assert.That(initialBalance, Is.EqualTo("£100.00"), "Initial balance should be £100.00");

        // Select a bet
        await BetSlip.SelectFirstBetAsync();

        // Fill stake and place bet
        await BetSlip.EnterStakeAsync("10");
        await Expect(BetSlip.BetSlipSelectionName).ToBeVisibleAsync();
        await Expect(BetSlip.StakeInput).ToHaveValueAsync("10");
        await BetSlip.PlaceBetAsync();

        // Verify bet was placed successfully
        await Expect(BetSlip.BetConfirmation).ToBeVisibleAsync();
        await Expect(BetSlip.BetsList).ToBeVisibleAsync();

        // Verify balance is still displayed correctly
        await Expect(BetSlip.UserBalance).ToHaveTextAsync("£90.00");
    }

    [Test]
    // Is BE fully connected to FE?
    public async Task Balance_Updates_In_UI_After_Bet_Is_Settled()
    {
        var IdsArray = await Api.CreateMarketIdAndSelectionIdWtihSingleSelection(selectionName: "Team A", odds: 2.50m);
        var selectionId = IdsArray[1];
        var eventId = IdsArray[2];
        await _page.ReloadAsync();
        await _page.WaitForSelectorAsync(".selection-btn");

        // Verify balance element is displayed
        var balanceElement = _page.GetByTestId("user-balance");
        await Expect(balanceElement).ToBeVisibleAsync();
        var initialBalance = await balanceElement.TextContentAsync();
        Assert.That(initialBalance, Is.EqualTo("£100.00"), "Initial balance should be £100.00");

        // Select a bet
        var selectionBtn = _page.Locator(".selection-btn").First;
        await selectionBtn.ClickAsync();

        // Fill stake and place bet
        await _page.FillAsync("#stake-input", "10");
        await _page.Locator("#place-bet-btn").ClickAsync();

        // Verify bet was placed successfully
        await Expect(_page.Locator("#bet-confirmation")).ToBeVisibleAsync();
        await Expect(_page.Locator("#bets-list")).ToBeVisibleAsync();

        //Settle bet
        await Api.SettleBet(eventId: eventId, winningSelectionId: selectionId);
        await _page.WaitForTimeoutAsync(10000);

        // Verify balance is still displayed correctly
        await Expect(balanceElement).ToBeVisibleAsync();
        var balanceText = await balanceElement.TextContentAsync();
        Assert.That(balanceText, Is.EqualTo("£125.00"), "Updated balance should be £125.00");

    }
}
