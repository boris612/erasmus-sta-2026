using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class EventsPageShould
{
    [Fact]
    public async Task ShowParticipantsCountAndRegistrationsLinkWhenRegistrationsExist()
    {
        await using var factory = new CustomWebApplicationFactory(ctx =>
        {
            TestDataSeeder.SeedRegistrationsScenario(ctx);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Events");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Spring Games", html);
        Assert.Contains("/Registrations?eventId=100", html);
        Assert.Contains(">2<", html);
    }
}
