using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class RegistrationsPageShould
{
    [Fact]
    public async Task ReturnEventSpecificRegistrationsForSelectedEvent()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedRegistrationsScenario);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Registrations?eventId=100");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Registrations for: Spring Games", html);
        Assert.Contains("Ivan Horvat", html);
        Assert.Contains("Johann Schmidt", html);
        Assert.Contains("Croatia", html);
        Assert.Contains("Germany", html);
    }

    [Fact]
    public async Task ReturnFilteredPartialForHtmxRequestWithCountryFilter()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedRegistrationsScenario);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");

        var response = await client.GetAsync("/Registrations?eventId=100&filters=CountryCode==HR");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"registrations-panel\"", html);
        Assert.Contains("Ivan Horvat", html);
        Assert.DoesNotContain("Johann Schmidt", html);
        Assert.DoesNotContain("<html", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReturnOnlyMatchingCountrySuggestionsForPersonLookup()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedRegistrationsScenario);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Registrations/PersonSuggestions?personLookup=jo&countryFilter=DE");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Johann Schmidt", html);
        Assert.Contains("Johann", html);
        Assert.DoesNotContain("Ivan Horvat", html);
        Assert.Contains("data-person-suggestion", html);
    }
}
