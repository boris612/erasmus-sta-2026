using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class HomePageShould
{
    [Fact]
    public async Task ReturnSuccessAndContainEnglishDescription()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("This sample demonstrates an ASP.NET Core MVC application", html);
    }
}
