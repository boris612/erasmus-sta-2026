using System.Net;
using Events.Tests.IntegrationTests.Infrastructure;

namespace Events.Tests.IntegrationTests;

public class PeoplePageShould
{
    [Fact]
    public async Task ReturnPageWithPeopleListWhenCountryAndPersonExist()
    {
        await using var factory = new CustomWebApplicationFactory(TestDataSeeder.SeedPeople);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/People");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("People list", html);
        Assert.Contains("Ivan Horvat", html);
        Assert.Contains("Croatia", html);
        Assert.Contains("ivan.horvat@example.com", html);
    }

    [Fact]
    public async Task RenderTranscribedFullNameAsTooltipOnPersonName()
    {
        await using var factory = new CustomWebApplicationFactory(ctx =>
        {
            ctx.Countries.Add(new Events.EFModel.Models.Country
            {
                Code = "UA",
                Alpha3 = "UKR",
                Name = "Ukraine"
            });

            ctx.People.Add(new Events.EFModel.Models.Person
            {
                Id = 1,
                FirstName = "Олексій",
                LastName = "Шевченко",
                FirstNameTranscription = "Oleksii",
                LastNameTranscription = "Shevchenko",
                AddressLine = "Khreshchatyk 1",
                PostalCode = "01001",
                City = "Kyiv",
                AddressCountry = "Ukraine",
                Email = "oleksii.shevchenko@example.com",
                ContactPhone = "+38050111222",
                BirthDate = new DateOnly(1991, 6, 1),
                DocumentNumber = "DOC-1",
                CountryCode = "UA"
            });
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/People");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-bs-toggle=\"tooltip\"", html);
        Assert.Contains("data-bs-title=\"Oleksii Shevchenko\"", html);
    }
}
