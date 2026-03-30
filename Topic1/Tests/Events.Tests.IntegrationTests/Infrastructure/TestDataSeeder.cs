using Events.EFModel.Models;

namespace Events.Tests.IntegrationTests.Infrastructure;

internal static class TestDataSeeder
{
    public static void SeedSports(EventsContext ctx)
    {
        ctx.Sports.AddRange(
            new Sport { Id = 1, Name = "Football" },
            new Sport { Id = 2, Name = "Basketball" });
    }

    public static void SeedPeople(EventsContext ctx)
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
    }

    public static void SeedEvents(EventsContext ctx)
    {
        ctx.Events.AddRange(
            new Event { Id = 100, Name = "Spring Games", EventDate = new DateOnly(2026, 4, 15) },
            new Event { Id = 200, Name = "Summer Cup", EventDate = new DateOnly(2026, 6, 20) });
    }

    public static void SeedRegistrationsScenario(EventsContext ctx)
    {
        ctx.Countries.AddRange(
            new Country
            {
                Code = "HR",
                Alpha3 = "HRV",
                Name = "Croatia"
            },
            new Country
            {
                Code = "DE",
                Alpha3 = "DEU",
                Name = "Germany"
            });

        ctx.People.AddRange(
            new Person
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
            },
            new Person
            {
                Id = 2,
                FirstName = "Johann",
                LastName = "Schmidt",
                FirstNameTranscription = "Johann",
                LastNameTranscription = "Schmidt",
                AddressLine = "Unter den Linden 5",
                PostalCode = "10117",
                City = "Berlin",
                AddressCountry = "Germany",
                Email = "johann.schmidt@example.com",
                ContactPhone = "+49170111222",
                BirthDate = new DateOnly(1988, 3, 12),
                DocumentNumber = "DOC-2",
                CountryCode = "DE"
            });

        ctx.Sports.AddRange(
            new Sport { Id = 10, Name = "Football" },
            new Sport { Id = 20, Name = "Basketball" });

        ctx.Events.AddRange(
            new Event { Id = 100, Name = "Spring Games", EventDate = new DateOnly(2026, 4, 15) },
            new Event { Id = 200, Name = "Summer Cup", EventDate = new DateOnly(2026, 6, 20) });

        ctx.Registrations.AddRange(
            new Registration
            {
                Id = 1000,
                EventId = 100,
                PersonId = 1,
                SportId = 10,
                RegisteredAt = new DateTime(2026, 3, 1, 9, 30, 0, DateTimeKind.Utc)
            },
            new Registration
            {
                Id = 1001,
                EventId = 100,
                PersonId = 2,
                SportId = 20,
                RegisteredAt = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc)
            },
            new Registration
            {
                Id = 1002,
                EventId = 200,
                PersonId = 1,
                SportId = 20,
                RegisteredAt = new DateTime(2026, 3, 3, 11, 0, 0, DateTimeKind.Utc)
            });
    }
}
