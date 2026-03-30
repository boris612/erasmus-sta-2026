using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class SportsCrudShould
{
    [Fact]
    public async Task CreateSport()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Sports",
            "/Sports/Create",
            [new("Name", "Volleyball")]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Volleyball", html);
    }

    [Fact]
    public async Task ReturnValidationErrorWhenCreatingSportWithoutName()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Sports",
            "/Sports/Create",
            [new("Name", string.Empty)]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The Name field is required.", html);
        Assert.DoesNotContain("was added successfully", html);
    }

    [Fact]
    public async Task EditSport()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedSports);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/Sports",
            "/Sports/Edit/1",
            [new("Id", "1"), new("Name", "Handball")]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Handball", html);
    }

    [Fact]
    public async Task DeleteSport()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedSports);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(client, "/Sports", "/Sports/Delete/1", []);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Football", html);
    }
}
