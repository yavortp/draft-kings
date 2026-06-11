using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BetSurvivesSuspensionTests : PlaywrightTest
{
    private IAPIRequestContext _api = null!;
    protected ApiHelpers Api;
    private string _userId = null!;
    private readonly string _baseUrl = "http://localhost:5000";

    [SetUp]
    public async Task SetUp()
    {
        _api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = _baseUrl
        });
        Api = new ApiHelpers(_api);

        //Create user
        var name = $"user_{Guid.NewGuid():N}";
        _userId = await Api.CreateUserAsync(name: name, balance: 100.00m);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _api.DisposeAsync();
    }

    [Test]
    public async Task Active_Bet_Remains_After_Market_Is_Suspended()
    {
        var userId = _userId;
        var selectionName = "Team A";
        var odds = 2.50m;
        var stake = 10.00m;
        var initialUSerBalance = await Api.GetUserBalance(userId: userId);

        // Arrange: create market
        var IdsArray = await Api.CreateMarketIdAndSelectionIdWtihSingleSelection(selectionName: selectionName, odds: odds);
        var marketId = IdsArray[0];
        var selectionId = IdsArray[1];
        var eventId = IdsArray[2];

        // Arrange: place bet and wait for processing
        var betResponse = await Api.PlaceSingleBet(userId: userId, selectionId: selectionId, stake: stake);
        var betIsActive = await PollUntilAsync(async () =>
        {
            var betsResponse = await _api.GetAsync($"/api/users/{userId}/bets");
            var bets = await betsResponse.JsonAsync();
            var betsArray = bets.Value.EnumerateArray().ToList();
            return betsArray.Count == 1 &&
                   betsArray[0].GetProperty("state").GetString() == "Active";
        }, timeoutMs: 5000);
        Assert.That(betIsActive, Is.True, "Bet should transition to Active state");
        var getBetStatus = await Api.GetUserBetsList(userId: userId);
        Assert.That(getBetStatus.State.Equals("Active"));

        // Act: suspend the market
        await Api.SuspendMarket(marketId: marketId);
        var isSuspended = await PollUntilAsync(async () =>
        {
            var state = await _api.GetAsync($"/api/markets/{marketId}");
            var data = await state.JsonAsync();
            return data.Value.GetProperty("state").GetString() == "Suspended";
        }, timeoutMs: 5000);

        // Assert: bet should still exist after suspension
        var getBetStatusAfterSuspension = await Api.GetUserBetsList(userId: userId);
        Assert.That(getBetStatusAfterSuspension.State.Equals("Active"));

        //Resume market
        await Api.ResumeMarket(marketId: marketId);
        var isResumed = await PollUntilAsync(async () =>
        {
            var state = await _api.GetAsync($"/api/markets/{marketId}");
            var data = await state.JsonAsync();
            return data.Value.GetProperty("state").GetString() == "Open";
        }, timeoutMs: 5000);
        var marketStatusResumed = await Api.GetMarketStatus(marketId: marketId);
        Assert.That(marketStatusResumed.Equals("Open"));

        //Settle bet and assert outcome details
        await Api.SettleBet(eventId: eventId, winningSelectionId: selectionId);
        var isBetSettled = await PollUntilAsync(async () =>
        {
            var settleResponse = await _api.GetAsync($"/api/events/{eventId}");
            var body = await settleResponse.TextAsync();
            var settleState = JsonDocument.Parse(body);
            return settleState.RootElement.GetProperty("state").GetString() == "Settled";
        }, timeoutMs: 5000);
        var settleResponse = await Api.GetEventDataAfterSettlement(eventId: eventId);
        var body = await settleResponse.TextAsync();
        var json = JsonDocument.Parse(body);

        var selection = json.RootElement
            .GetProperty("markets")[0]
            .GetProperty("selections")[0];

        var settleState = json.RootElement.GetProperty("state").GetString();
        var returnedSelectionName = selection.GetProperty("name").GetString();
        var returnedOdds = selection.GetProperty("odds").GetDecimal();
        Assert.That(returnedSelectionName, Is.EqualTo(selectionName));
        Assert.That((returnedOdds * stake).Equals(25.00m));

        var newUserBalance = await Api.GetUserBalance(userId: userId);
        var balanceAfterPlaceBet = initialUSerBalance - stake;
        var payout = returnedOdds * stake;
        Assert.That((initialUSerBalance - stake + returnedOdds * stake).Equals(newUserBalance));
    }

    private static async Task<bool> PollUntilAsync(Func<Task<bool>> condition, int timeoutMs, int intervalMs = 250)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition()) return true;
            await Task.Delay(intervalMs);
        }
        return false;
    }
}
