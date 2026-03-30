using System.Net;
using Events.EFModel.Models;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class PeopleCrudShould
{
    [Fact]
    public async Task CreatePerson()
    {
        await using var factory = new CustomWebApplicationFactory(ctx => ctx.Countries.Add(new Country { Code = "HR", Alpha3 = "HRV", Name = "Croatia" }));
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/People",
            "/People/Create",
            [
                new("FirstName", "Ana"),
                new("LastName", "Kovac"),
                new("FirstNameTranscription", "Ana"),
                new("LastNameTranscription", "Kovac"),
                new("Email", "ana.kovac@example.com"),
                new("ContactPhone", "+38591123456"),
                new("BirthDate", "1995-01-01"),
                new("CountryCode", "HR"),
                new("DocumentNumber", "DOC-2"),
                new("AddressLine", "Main Street 1"),
                new("PostalCode", "10000"),
                new("City", "Zagreb"),
                new("AddressCountry", "Croatia")
            ]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ana Kovac", html);
    }

    [Fact]
    public async Task EditPerson()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedPeople);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(
            client,
            "/People",
            "/People/Edit/1",
            [
                new("Id", "1"),
                new("FirstName", "Ivan"),
                new("LastName", "Kovac"),
                new("FirstNameTranscription", "Ivan"),
                new("LastNameTranscription", "Kovac"),
                new("Email", "ivan.kovac@example.com"),
                new("ContactPhone", "+38591111222"),
                new("BirthDate", "1990-05-01"),
                new("CountryCode", "HR"),
                new("DocumentNumber", "DOC-1"),
                new("AddressLine", "Ilica 1"),
                new("PostalCode", "10000"),
                new("City", "Zagreb"),
                new("AddressCountry", "Croatia")
            ]);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ivan Kovac", html);
    }

    [Fact]
    public async Task DeletePerson()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedPeople);
        using var client = factory.CreateClient();

        var response = await AntiforgeryRequestHelper.PostFormAsync(client, "/People", "/People/Delete/1", []);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Ivan Horvat", html);
    }
}
