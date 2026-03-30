using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class CountriesCrudShould
{
    [Fact]
    public async Task CreateCountry()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Countries",
            "/Countries/Create",
            [
                new("Code", "DE"),
                new("Alpha3", "DEU"),
                new("Name", "Germany"),
                new("Translations[0].LanguageCode", "hr"),
                new("Translations[0].Name", "Germany")
            ]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Germany", html);
    }

    [Fact]
    public async Task EditCountry()
    {
        await using var factory = new CustomWebApplicationFactory(ctx => ctx.Countries.Add(TestDataSeederCountry()));
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Countries",
            "/Countries/Edit/HR",
            [
                new("Code", "HR"),
                new("Alpha3", "HRV"),
                new("Name", "Republic of Croatia"),
                new("Translations[0].LanguageCode", ""),
                new("Translations[0].Name", "")
            ]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Republic of Croatia", html);
    }

    [Fact]
    public async Task DeleteCountry()
    {
        await using var factory = new CustomWebApplicationFactory(ctx => ctx.Countries.Add(TestDataSeederCountry()));
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(client, "/Countries", "/Countries/Delete/HR", []);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Croatia", html);
    }

    private static Events.EFModel.Models.Country TestDataSeederCountry()
    {
        return new()
        {
            Code = "HR",
            Alpha3 = "HRV",
            Name = "Croatia"
        };
    }
}
