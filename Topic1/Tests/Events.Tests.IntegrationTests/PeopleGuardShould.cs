using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Events.Tests.IntegrationTests;

public class PeopleGuardShould
{
    [Fact]
    public async Task RedirectToCountriesWhenPeoplePageIsRequestedWithoutCountries()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/People");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Countries", response.Headers.Location?.OriginalString);
    }
}
