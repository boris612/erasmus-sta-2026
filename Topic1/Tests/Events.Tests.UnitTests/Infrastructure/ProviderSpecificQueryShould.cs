using Events.Tests.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Events.Tests.UnitTests.Infrastructure;

public class ProviderSpecificQueryShould
{
    [Fact]
    public async Task ReturnMatchingRowsWhenUsingILikeWithInMemoryProvider()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.Add(ControllerTestContext.CreatePerson());
        await ctx.SaveChangesAsync();

        var people = await ctx.People
            .Where(person => EF.Functions.ILike(person.FirstName, "%iv%"))
            .ToListAsync();

        Assert.Single(people);
        Assert.Equal("Ivan", people[0].FirstName);
    }

    [Fact]
    public async Task ThrowInvalidOperationExceptionWhenUsingILikeWithInMemoryProvider()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.Add(ControllerTestContext.CreatePerson());
        await ctx.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await ctx.People
                .Where(person => EF.Functions.ILike(person.FirstName, "%iv%"))
                .ToListAsync());

        Assert.Contains("ILike", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteILikeWhenUsingPostgreSqlProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<ProviderSpecificQueryShould>(optional: true)
            .Build();

        var productionConnectionString = configuration.GetConnectionString("EventDB-Test");
        Assert.False(
            string.IsNullOrWhiteSpace(productionConnectionString),
            "The EventDB-Test connection string must be available so the PostgreSQL-backed provider test can connect to the PostgreSQL copy.");

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(productionConnectionString)
        {
            SslMode = SslMode.Disable
        };

        var options = new DbContextOptionsBuilder<Events.EFModel.Models.EventsContext>()
            .UseNpgsql(connectionStringBuilder.ConnectionString)
            .Options;

        await using var ctx = new Events.EFModel.Models.EventsContext(options);

        var people = await ctx.People
            .Where(person => EF.Functions.ILike(person.FirstName, "%iv%"))
            .Take(20)
            .ToListAsync();

        Assert.NotEmpty(people);
        Assert.All(
            people,
            person => Assert.Contains(
                "iv",
                person.FirstName,
                StringComparison.OrdinalIgnoreCase));
    }
}
