using System.Net;
using Events.EFModel.Models;
using Events.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Events.Tests.IntegrationTests;

public class RegistrationsGuardShould
{
    [Fact]
    public async Task RedirectToSportsWhenRegistrationsPageIsRequestedWithoutSports()
    {
        await using var factory = new CustomWebApplicationFactory(ctx =>
        {
            ctx.Countries.Add(new Country
            {
                Code = "HR",
                Alpha3 = "HRV",
                Name = "Croatia"
            });
            ctx.People.Add(new Person
            {
                Id = 1,
                FirstName = "Ivan",
                LastName = "Horvat",
                FirstNameTranscription = "Ivan",
                LastNameTranscription = "Horvat",
                AddressLine = "Ilica 1",
                PostalCode = "10000",
                City = "Zagreb",
                AddressCountry = "Croatia",
                Email = "ivan.horvat@example.com",
                ContactPhone = "+38591111222",
                BirthDate = new DateOnly(1990, 5, 1),
                DocumentNumber = "DOC-1",
                CountryCode = "HR"
            });
            ctx.Events.Add(new Event
            {
                Id = 100,
                Name = "Spring Games",
                EventDate = new DateOnly(2026, 4, 15)
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Registrations");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Sports", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RedirectToPeopleWhenRegistrationsPageIsRequestedWithoutPeople()
    {
        await using var factory = new CustomWebApplicationFactory(ctx =>
        {
            ctx.Sports.Add(new Sport { Id = 10, Name = "Football" });
            ctx.Events.Add(new Event
            {
                Id = 100,
                Name = "Spring Games",
                EventDate = new DateOnly(2026, 4, 15)
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Registrations");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/People", response.Headers.Location?.OriginalString);
    }
}
