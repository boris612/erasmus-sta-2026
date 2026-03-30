using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class EventsCrudShould
{
    [Fact]
    public async Task CreateEvent()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Events",
            "/Events/Create",
            [new("Name", "Autumn Cup"), new("EventDate", "2026-09-10")]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Autumn Cup", html);
    }

    [Fact]
    public async Task EditEvent()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedEvents);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Events",
            "/Events/Edit/100",
            [new("Id", "100"), new("Name", "Updated Games"), new("EventDate", "2026-04-20")]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Updated Games", html);
    }

    [Fact]
    public async Task DeleteEvent()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedEvents);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(client, "/Events", "/Events/Delete/100", []);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Spring Games", html);
    }
}
