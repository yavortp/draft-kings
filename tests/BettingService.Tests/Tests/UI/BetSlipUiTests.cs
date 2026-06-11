using BettingService.Tests.Pages;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BetSlipUiTests : PlaywrightTest
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
    public async Task Market_Selections_And_Odds_Are_Displayed_Correctly()
    {
        // Assert: selections are visible with correct odds from the UI
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        Assert.That(selections, Has.Count.GreaterThanOrEqualTo(1));

        foreach (var selection in selections)
        {
            var (name, odds) = await BetSlip.GetSelectionDataAsync(selection);

            Assert.That(name, Is.Not.Null.And.Not.Empty, "Selection name is missing");
            Assert.That(odds, Is.Not.Null.And.Not.Empty, "Selection odds is missing");
            Assert.That(decimal.TryParse(odds, out _), Is.True, $"Odds '{odds}' is not a valid number");
        }
    }

    [Test]
    public async Task Bet_Slip_Displays_First_Selection_With_Correct_Odds()
    {
        // Click the first selection
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        var (nameText, oddsText) = await BetSlip.GetSelectionDataAsync(selections[0]);

        // Click the selection button
        await selections[0].ClickAsync(); ;

        // Assert: bet slip populates with selection details
        Assert.That(await BetSlip.BetSlipSelectionName.TextContentAsync(), Is.EqualTo(nameText));
        Assert.That(await BetSlip.BetSlipOdds.TextContentAsync(), Is.EqualTo(oddsText));

        // Enter stake and verify payout calculation
        await BetSlip.EnterStakeAsync("10");
        Assert.That(await BetSlip.PotentialPayout.TextContentAsync(), Is.EqualTo(BetSlip.ExpectedPayout(10m, oddsText)));
    }

    [Test]
    public async Task Bet_Slip_Displays_Second_Selection_With_Correct_Odds()
    {
        // Click the first selection
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        var secondSelection = selections[1];
        var selectionName = await secondSelection.QuerySelectorAsync(".selection-name");
        var selectionOdds = await secondSelection.QuerySelectorAsync(".selection-odds");

        var nameText = await selectionName!.TextContentAsync();
        var oddsText = await selectionOdds!.TextContentAsync();

        // Click the selection button
        await secondSelection.ClickAsync();

        // Assert: bet slip populates with selection details
        var betSlipName = await _page.TextContentAsync("#betslip-selection-name");
        var betSlipOdds = await _page.TextContentAsync("#betslip-odds");

        Assert.That(betSlipName, Is.EqualTo(nameText));
        Assert.That(betSlipOdds, Is.EqualTo(oddsText));

        // Enter stake and verify payout calculation
        await BetSlip.EnterStakeAsync("10");
        Assert.That(await BetSlip.PotentialPayout.TextContentAsync(), Is.EqualTo(BetSlip.ExpectedPayout(10m, oddsText)));
    }

    [Test]
    public async Task Bet_Slip_Displays_Third_Selection_With_Correct_Odds()
    {
        // Click the first selection
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        var thirdSelection = selections[2];
        var selectionName = await thirdSelection.QuerySelectorAsync(".selection-name");
        var selectionOdds = await thirdSelection.QuerySelectorAsync(".selection-odds");

        var nameText = await selectionName!.TextContentAsync();
        var oddsText = await selectionOdds!.TextContentAsync();

        // Click the selection button
        await thirdSelection.ClickAsync();

        // Assert: bet slip populates with selection details
        var betSlipName = await _page.TextContentAsync("#betslip-selection-name");
        var betSlipOdds = await _page.TextContentAsync("#betslip-odds");

        Assert.That(betSlipName, Is.EqualTo(nameText));
        Assert.That(betSlipOdds, Is.EqualTo(oddsText));

        // Enter stake and verify payout calculation
        await BetSlip.EnterStakeAsync("10");
        Assert.That(await BetSlip.PotentialPayout.TextContentAsync(), Is.EqualTo(BetSlip.ExpectedPayout(10m, oddsText)));
    }

    [Test]
    public async Task Bet_Slip_Updates_Correctly_When_User_Changes_Selection()
    {
        // Click the first selection
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        var firstSelection = selections[0];
        var thirdSelection = selections[2];
        var selectionName = await firstSelection.QuerySelectorAsync(".selection-name");
        var selectionOdds = await firstSelection.QuerySelectorAsync(".selection-odds");
        var nameText = await selectionName!.TextContentAsync();
        var oddsText = await selectionOdds!.TextContentAsync();
        var selectionName2 = await thirdSelection.QuerySelectorAsync(".selection-name");
        var selectionOdds2 = await thirdSelection.QuerySelectorAsync(".selection-odds");
        var nameText2 = await selectionName2!.TextContentAsync();
        var oddsText2 = await selectionOdds2!.TextContentAsync();

        // Click the selection button
        await firstSelection.ClickAsync();

        // Assert: bet slip populates with selection details
        var betSlipName = await _page.TextContentAsync("#betslip-selection-name");
        var betSlipOdds = await _page.TextContentAsync("#betslip-odds");

        Assert.That(betSlipName, Is.EqualTo(nameText));
        Assert.That(betSlipOdds, Is.EqualTo(oddsText));

        // Enter stake and verify payout calculation
        await _page.FillAsync("#stake-input", "10");
        var expectedPayout = $"£{(10m * decimal.Parse(oddsText!)):F2}";
        var displayedPayout = await _page.TextContentAsync("#potential-payout");
        Assert.That(displayedPayout, Is.EqualTo(expectedPayout));

        // Change the selection 
        await thirdSelection.ClickAsync();

        // Assert: bet slip updates and populates selection details
        var betSlipName2 = await _page.TextContentAsync("#betslip-selection-name");
        var betSlipOdds2 = await _page.TextContentAsync("#betslip-odds");

        Assert.That(betSlipName2, Is.EqualTo(nameText2));
        Assert.That(betSlipOdds2, Is.EqualTo(oddsText2));

        // verify payout calculation
        var expectedPayout2 = $"£{(10m * decimal.Parse(oddsText2!)):F2}";
        var displayedPayout2 = await _page.TextContentAsync("#potential-payout");
        Assert.That(displayedPayout2, Is.EqualTo(expectedPayout2));
    }

    [Test]
    // Is BE fully connected to FE? 
    public async Task Disable_Bet_Slip_Place_Bet_Button_On_Market_Suspension()
    {

        var IdsArray = await Api.CreateMarketIdAndSelectionIdWtihSingleSelection(selectionName: "Team A", odds: 2.50m);
        var marketId = IdsArray[0];
        await _page.ReloadAsync();
        await _page.WaitForSelectorAsync(".selection-btn");

        // Click the first selection
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        var firstSelection = selections[0];
        await firstSelection.ClickAsync();

        // Assert: place bet button is enabled before suspension
        var placeBetButton = _page.Locator("#place-bet-btn");
        await Expect(placeBetButton).ToBeEnabledAsync();

        // Act: suspend the market via API
        await Api.SuspendMarket(marketId: marketId);

        // Assert: place bet button becomes disabled after suspension
        await Expect(placeBetButton).ToBeDisabledAsync();

    }

    [Test]
    public async Task Bet_Slip_Place_Bet_Input_Field_Arrow_Up_Validation()
    {
        // Click the first selection
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        var firstSelection = selections[0];
        var selectionName = await firstSelection.QuerySelectorAsync(".selection-name");
        var selectionOdds = await firstSelection.QuerySelectorAsync(".selection-odds");

        var nameText = await selectionName!.TextContentAsync();
        var oddsText = await selectionOdds!.TextContentAsync();

        // Click the selection button
        await firstSelection.ClickAsync();

        // Assert: bet slip populates with selection details
        var betSlipName = await _page.TextContentAsync("#betslip-selection-name");
        var betSlipOdds = await _page.TextContentAsync("#betslip-odds");
        Assert.That(betSlipName, Is.EqualTo(nameText));
        Assert.That(betSlipOdds, Is.EqualTo(oddsText));

        // Enter stake and verify payout calculation
        await _page.FillAsync("#stake-input", "10");
        var expectedPayout = $"£{(10m * decimal.Parse(oddsText!)):F2}";
        var displayedPayout = await _page.TextContentAsync("#potential-payout");
        Assert.That(displayedPayout, Is.EqualTo(expectedPayout));

        //Click arrow up in input field
        await _page.Keyboard.PressAsync("ArrowUp");
        var expectedPayout2 = $"£{(10.01m * decimal.Parse(oddsText!)):F2}";
        var displayedPayout2 = await _page.TextContentAsync("#potential-payout");
        Assert.That(displayedPayout2, Is.GreaterThan(displayedPayout));
        // Assert.That(displayedPayout2, Is.EqualTo(expectedPayout2));

    }

    [Test]
    public async Task Bet_Slip_Place_Bet_Input_Field_Arrow_Down_Validation()
    {
        // Click the first selection
        var selections = await _page.QuerySelectorAllAsync(".selection-btn");
        var firstSelection = selections[0];
        var selectionName = await firstSelection.QuerySelectorAsync(".selection-name");
        var selectionOdds = await firstSelection.QuerySelectorAsync(".selection-odds");

        var nameText = await selectionName!.TextContentAsync();
        var oddsText = await selectionOdds!.TextContentAsync();

        // Click the selection button
        await firstSelection.ClickAsync();

        // Assert: bet slip populates with selection details
        var betSlipName = await _page.TextContentAsync("#betslip-selection-name");
        var betSlipOdds = await _page.TextContentAsync("#betslip-odds");
        Assert.That(betSlipName, Is.EqualTo(nameText));
        Assert.That(betSlipOdds, Is.EqualTo(oddsText));

        // Enter stake and verify payout calculation
        await _page.FillAsync("#stake-input", "10");
        var expectedPayout = $"£{(10m * decimal.Parse(oddsText!)):F2}";
        var displayedPayout = await _page.TextContentAsync("#potential-payout");
        Assert.That(displayedPayout, Is.EqualTo(expectedPayout));

        //Click arrow up in input field
        await _page.Keyboard.PressAsync("ArrowDown");
        var expectedPayout2 = $"£{(9.99m * decimal.Parse(oddsText!)):F2}";
        var displayedPayout2 = await _page.TextContentAsync("#potential-payout");
        Assert.That(displayedPayout2, Is.EqualTo(expectedPayout2));
    }
}
