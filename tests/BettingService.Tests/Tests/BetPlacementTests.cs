using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace BettingService.Tests.Tests;

[TestFixture]
public class BetPlacementTests : PlaywrightTest
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
    public async Task Placing_A_Bet_Creates_Active_Bet_And_Deducts_Balance()
    {
        var userId = _userId;
        var selectionName = "Team A";
        var odds = 2.50m;
        var stake = 10.00m;
        
        // Arrange: create a market with a selection
        var selectionId = await Api.CreateMarketWtihSingleSelection(selectionName: selectionName, odds: odds);

        // Act: place a bet
        var betResponse = await Api.PlaceSingleBet(userId: userId, selectionId: selectionId, stake: stake);
        Assert.That(betResponse.Status, Is.EqualTo(202));

        // Assert: poll until bet state becomes Active (async processing)
        var betIsActive = await PollUntilAsync(async () =>
        {
            var betsResponse = await _api.GetAsync($"/api/users/{userId}/bets");
            var bets = await betsResponse.JsonAsync();
            var betsArray = bets.Value.EnumerateArray().ToList();
            return betsArray.Count == 1 &&
                   betsArray[0].GetProperty("state").GetString() == "Active";
        }, timeoutMs: 5000);
        Assert.That(betIsActive, Is.True, "Bet should transition to Active state");

        // Assert bet details are correct in bet slip
        var userBetDetails = await Api.GetUserBetsList(userId: userId);
        Assert.That(selectionName.Equals(userBetDetails.SelectionName));
        Assert.That(stake.Equals(userBetDetails.Stake));
        Assert.That(odds.Equals(userBetDetails.Odds));
        Assert.That((userBetDetails.State).Equals("Active"));

        // Assert: balance should be deducted
        var userBalance = await Api.GetUserBalance(userId: userId);
        Assert.That(userBalance, Is.EqualTo(90.00m));
    }

    [Test]
    public async Task Placing_A_Bet_With_Unsufficient_Balance_Returns_400()
    {
        var userId = _userId;
        var selectionName = "Team A";
        var odds = 2.50m;
        var stake = 101.00m;
        
        // Arrange: create a market with a selection
        var selectionId = await Api.CreateMarketWtihSingleSelection(selectionName: selectionName, odds: odds);

        // Act: place a bet
        var betResponse = await Api.PlaceSingleBet(userId: userId, selectionId: selectionId, stake: stake);
        Assert.That(betResponse.Status, Is.EqualTo(400));
    }

    [Test]
    //Defect - not handled correctly, no console error, no UI message
    public async Task Placing_A_Bet_With_Negative_Stake_Returns_400()
    {
        var userId = _userId;
        var selectionName = "Team A";
        var odds = 2.50m;
        var stake = -10.00m;
        
        // Arrange: create a market with a selection
        var selectionId = await Api.CreateMarketWtihSingleSelection(selectionName: selectionName, odds: odds);

        // Act: place a bet
        var betResponse = await Api.PlaceSingleBet(userId: userId, selectionId: selectionId, stake: stake);
        Assert.That(betResponse.Status, Is.EqualTo(400));
    }

    [Test]
    //Defect - Bet went through
    public async Task Placing_A_Bet_With_Stake_Less_Than_1_cent_Returns_400()
    {
        var userId = _userId;
        var selectionName = "Team A";
        var odds = 2.50m;
        var stake = 0.001m;
        
        // Arrange: create a market with a selection
        var selectionId = await Api.CreateMarketWtihSingleSelection(selectionName: selectionName, odds: odds);

        // Act: place a bet
        var betResponse = await Api.PlaceSingleBet(userId: userId, selectionId: selectionId, stake: stake);
        Assert.That(betResponse.Status, Is.EqualTo(400));
    }

    [Test]
    //Defect - Input field accepts letter "e"
    public async Task Bet_Input_Field_Should_Return_400_If_Letters_Or_Special_Chars_Are_Entered()
    {
        var userId = _userId;
        var selectionName = "Team A";
        var odds = 2.50m;
        // var stake = "e";
        var stake = 10.00m;
        
        // Arrange: create a market with a selection
        var selectionId = await Api.CreateMarketWtihSingleSelection(selectionName: selectionName, odds: odds);

        // Act: place a bet
        var betResponse = await Api.PlaceSingleBet(userId: userId, selectionId: selectionId, stake: stake);
        Assert.That(betResponse.Status, Is.EqualTo(400));
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
