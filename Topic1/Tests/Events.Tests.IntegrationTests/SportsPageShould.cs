using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class SportsPageShould
{
    [Fact]
    public async Task ReturnPageWithSportsListWhenSportsExist()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedSports);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Sports");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Sports list", html);
        Assert.Contains("Football", html);
        Assert.Contains("Basketball", html);
    }

    [Fact]
    public async Task ReturnOnlySportsPartialForHtmxRequest()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedSports);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");

        var response = await client.GetAsync("/Sports");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"sports-list\"", html);
        Assert.DoesNotContain("<html", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("navbar", html, StringComparison.OrdinalIgnoreCase);
    }
}
