using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class MarketSuspensionTests : PlaywrightTest
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
    public async Task Suspended_Market_Rejects_New_Bets()
    {
        var userId = _userId;
        var selectionName = "Team A";
        var odds = 2.50m;
        var stake = 10.00m;

        // Arrange: create market
        var IdsArray = await Api.CreateMarketIdAndSelectionIdWtihSingleSelection(selectionName: selectionName, odds: odds);
        var marketId = IdsArray[0];
        var selectionId = IdsArray[1];

        // Act: suspend the market
        await Api.SuspendMarket(marketId: marketId);

        // Wait until market is actually suspended (async propagation)
        var isSuspended = await PollUntilAsync(async () =>
        {
            var state = await _api.GetAsync($"/api/markets/{marketId}");
            var data = await state.JsonAsync();
            return data.Value.GetProperty("state").GetString() == "Suspended";
        }, timeoutMs: 5000);

        var marketStatus = await Api.GetMarketStatus(marketId: marketId);
        Assert.That(marketStatus.Equals("Suspended"));

        // Act: attempt to place a bet on suspended market
        var betResponse = await Api.PlaceSingleBet(userId: userId, selectionId: selectionId, stake: stake);
        
        // Assert: bet should be rejected
        Assert.That(betResponse.Status, Is.EqualTo(409));

        //Assert user balance is still 100.00
        var userBalance = await Api.GetUserBalance(userId: userId);
        Assert.That(userBalance.Equals(100.00m));    }

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
