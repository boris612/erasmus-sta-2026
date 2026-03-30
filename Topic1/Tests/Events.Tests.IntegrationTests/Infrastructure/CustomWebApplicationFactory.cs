using Events.EFModel.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Events.Tests.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<EventsContext>? seed;
    private readonly InMemoryDatabaseRoot databaseRoot = new();
    private readonly string databaseName = Guid.NewGuid().ToString();

    public CustomWebApplicationFactory(Action<EventsContext>? seed = null)
    {
        this.seed = seed;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<EventsContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<EventsContext>));
            services.RemoveAll(typeof(EventsContext));

            services.AddDbContext<EventsContext>(options =>
                options
                    .UseInMemoryDatabase(databaseName, databaseRoot)
                    .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();
            ctx.Database.EnsureCreated();
            seed?.Invoke(ctx);
            ctx.SaveChanges();
        });
    }
}
