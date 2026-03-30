using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class RegistrationsCrudShould
{
    [Fact]
    public async Task CreateRegistration()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedRegistrationsScenario);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Registrations?eventId=100",
            "/Registrations/Create",
            [new("EventId", "100"), new("PersonId", "1"), new("SportId", "20"), new("PersonLookup", "Ivan Horvat")]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Basketball", html);
    }

    [Fact]
    public async Task EditRegistration()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedRegistrationsScenario);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Registrations?eventId=100",
            "/Registrations/Edit/1000",
            [
                new("Id", "1000"),
                new("EventId", "100"),
                new("PersonId", "2"),
                new("SportId", "20"),
                new("PersonLookup", "Johann Schmidt")
            ]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Johann Schmidt", html);
        Assert.Contains("Basketball", html);
    }

    [Fact]
    public async Task DeleteRegistration()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedRegistrationsScenario);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(client, "/Registrations?eventId=100", "/Registrations/Delete/1000?eventId=100", []);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(">1000<", html);
    }
}
