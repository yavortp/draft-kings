using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class SettlementTests : PlaywrightTest
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
    public async Task Settlement_Pays_Winning_Bet_Correctly()
    {
        var userId = _userId;
        var selectionName = "Team A";
        var odds = 2.50m;
         // Arrange: create market
        var IdsArray = await Api.CreateMarketIdAndSelectionIdWtihSingleSelection(selectionName: selectionName, odds: odds);
        var winningSelectionId = IdsArray[1];
        var eventId = IdsArray[2];

        // Arrange: place bet and wait for it to become Active
        await Api.PlaceSingleBet(userId: userId, selectionId: winningSelectionId, stake: 10.00m);
        var betActive = await PollUntilAsync(async () =>
        {
            var bets = await _api.GetAsync($"/api/users/{userId}/bets");
            var data = await bets.JsonAsync();
            return data.Value.EnumerateArray().Any(b => b.GetProperty("state").GetString() == "Active");
        }, timeoutMs: 5000);
        Assert.That(betActive, Is.True, "Bet must be Active before settlement");

        // Act: post result — Team A wins
        var resultResponse = Api.SettleBet(eventId: eventId, winningSelectionId: winningSelectionId);


        // Assert: poll until bet is settled as Won with correct payout
        var settled = await PollUntilAsync(async () =>
        {
            var bets = await _api.GetAsync($"/api/users/{userId}/bets");
            var data = await bets.JsonAsync();
            var bet = data.Value.EnumerateArray().FirstOrDefault();
            return bet.GetProperty("state").GetString() == "Won" &&
                   bet.GetProperty("payout").GetDecimal() == 25.00m;
        }, timeoutMs: 5000);

        Assert.That(settled, Is.True, "Bet should settle as Won with payout = stake × odds");

        // Assert: balance updated
        var balanceResponse = await _api.GetAsync($"/api/users/{userId}/balance");
        var balance = await balanceResponse.JsonAsync();
        Assert.That(balance.Value.GetProperty("amount").GetDecimal(), Is.EqualTo(115.00m));
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
