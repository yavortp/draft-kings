using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework.Internal;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace BettingService.Tests.Tests;

public class ApiHelpers
{
    private readonly IAPIRequestContext _api;

    public ApiHelpers(IAPIRequestContext api)
    {
        _api = api;
    }

   
    public async Task<string> CreateUserAsync(string name, decimal balance)
    {
        var response = await _api.PostAsync("/api/users", new()
        {
            DataObject = new { name, balance }
        });

        Assert.That(response.Ok, Is.True, $"CreateUser failed: {response.Status}");

        var body = await response.TextAsync();
        var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("id").GetString()
            ?? throw new Exception($"User ID not found in response. Body: {body}");
    }

    public async Task<string> CreateMarketWtihSingleSelection(string selectionName, decimal odds)
    {
        var eventId = Guid.NewGuid();
        var marketName = "Match Winner";
        var eventName = "Test Match";

        var response = await _api.PostAsync("/api/markets", new()
        {
            DataObject = new
            {
                name = marketName,
                eventId,
                eventName,
                selections = new[] { new { name = selectionName, odds } }
            }
        });
        var body = await response.TextAsync();
        var json = JsonDocument.Parse(body);

        return json.RootElement.GetProperty("selections")[0].GetProperty("id").GetString()
            ?? throw new Exception("Selection ID not found in response");
    }

    public async Task<String[]> CreateMarketIdAndSelectionIdWtihSingleSelection(string selectionName, decimal odds)
    {
        var eventId = Guid.NewGuid();
        var marketName = "Match Winner";
        var eventName = "Test Match";

        var response = await _api.PostAsync("/api/markets", new()
        {
            DataObject = new
            {
                name = marketName,
                eventId,
                eventName,
                selections = new[] { new { name = selectionName, odds } }
            }
        });
        var body = await response.TextAsync();
        var json = JsonDocument.Parse(body);
        var marketId = json.RootElement.GetProperty("id").GetString();
        var selectionId = json.RootElement.GetProperty("selections")[0].GetProperty("id").GetString();
        String [] IdsArray = new String[3] {marketId!, selectionId!, eventId.ToString()};
        return IdsArray;
    }

    public async Task<IAPIResponse> PlaceSingleBet(string userId, string selectionId, decimal stake)
    {
        return await _api.PostAsync($"/api/users/{userId}/bets", new()
        {
            DataObject = new { selectionId, stake }
        });
    }

    public async Task<BetResponse> GetUserBetsList(string userId)
    {
        var response = await _api.GetAsync($"/api/users/{userId}/bets");
        Assert.That(response.Status.Equals(200));
        var body = await response.TextAsync();
        var json = JsonDocument.Parse(body);
        var bet = json.RootElement[0];
        return new BetResponse
        {
            SelectionName = bet.GetProperty("selectionName").GetString()!,
            Odds = bet.GetProperty("odds").GetDecimal(),
            Stake = bet.GetProperty("stake").GetDecimal(),
            State = bet.GetProperty("state").GetString()!
        };
    }

    public async Task<decimal> GetUserBalance(string userId)
    {
        var response = await _api.GetAsync($"/api/users/{userId}/balance");
        var balance = await response.JsonAsync();
        return balance.Value.GetProperty("amount").GetDecimal();
    }

    public async Task SuspendMarket(string marketId)
    {
        await _api.PostAsync($"/api/markets/{marketId}/suspend", new() { });
    }

    public async Task ResumeMarket(string marketId)
    {
        await _api.PostAsync($"/api/markets/{marketId}/resume", new() { });
    }

    public async Task<string> GetMarketStatus(string marketId)
    {
        var response = await _api.GetAsync($"/api/markets/{marketId}");
        var body = await response.TextAsync();
        var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("state").GetString();
    }

    public async Task<IAPIResponse> GetBetsList(string userId)
    {
        var response = await _api.GetAsync($"/api/users/{userId}/bets");
        return response;
    }

    public async Task SettleBet(string eventId, string winningSelectionId)
    {
        await _api.PostAsync($"/api/events/{eventId}/result",  new()
        {
            DataObject = new { winningSelectionId }
        });
    }

    public async Task<IAPIResponse> GetEventDataAfterSettlement(string eventId)
    {
        var response = await _api.GetAsync($"/api/events/{eventId}");
        return response;
    }
}

public record UserResponse(string Id, string Name, decimal Balance);