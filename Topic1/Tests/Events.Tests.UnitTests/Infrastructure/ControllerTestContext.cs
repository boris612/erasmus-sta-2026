using Events.EFModel.Models;
using Events.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace Events.Tests.UnitTests.Infrastructure;

internal static class ControllerTestContext
{
    public static EventsContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EventsContext(options);
    }

    public static IOptions<PagingSettings> CreatePagingOptions(int pageSize = 10)
    {
        return Options.Create(new PagingSettings { PageSize = pageSize });
    }

    public static SieveModel EmptySieveModel()
    {
        return new SieveModel();
    }

    public static ISieveProcessor CreateSieveProcessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddScoped<ISieveProcessor, SieveProcessor>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISieveProcessor>();
    }

    public static Country CreateCountry(string code = "HR", string alpha3 = "HRV", string name = "Croatia")
    {
        return new Country
        {
            Code = code,
            Alpha3 = alpha3,
            Name = name
        };
    }

    public static Person CreatePerson(int id = 1, string countryCode = "HR", string firstName = "Ivan", string lastName = "Horvat")
    {
        return new Person
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            FirstNameTranscription = firstName,
            LastNameTranscription = lastName,
            AddressLine = "Ilica 1",
            PostalCode = "10000",
            City = "Zagreb",
            AddressCountry = "Croatia",
            Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
            ContactPhone = "+38591111222",
            BirthDate = new DateOnly(1990, 5, 1),
            DocumentNumber = $"DOC-{id}",
            CountryCode = countryCode
        };
    }

    public static Event CreateEvent(int id = 100, string name = "Spring Games")
    {
        return new Event
        {
            Id = id,
            Name = name,
            EventDate = new DateOnly(2026, 4, 15)
        };
    }

    public static Sport CreateSport(int id = 10, string name = "Football")
    {
        return new Sport
        {
            Id = id,
            Name = name
        };
    }
}
