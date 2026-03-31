using System.Linq;
using Events.WebAPI;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Tests.ApplicationFactories.Common;

public abstract class PostgreSqlWebApiFactoryBase : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18")
    .WithDatabase("events_tests")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("IntegrationTests");

    builder.ConfigureServices(services =>
    {
      services.RemoveAll<DbContextOptions<EventsContext>>();
      services.RemoveAll<EventsContext>();
      services.RemoveAll<IDbContextOptionsConfiguration<EventsContext>>();

      var npgsqlDescriptorsToRemove = services
        .Where(IsNpgsqlDescriptor)
        .ToList();

      foreach (var descriptor in npgsqlDescriptorsToRemove)
      {
        services.Remove(descriptor);
      }

      var descriptorsToRemove = services
        .Where(descriptor =>
          descriptor.ServiceType == typeof(IHostedService) ||
          (descriptor.ImplementationType?.Namespace?.StartsWith("MassTransit", StringComparison.Ordinal) ?? false) ||
          (descriptor.ServiceType.Namespace?.StartsWith("MassTransit", StringComparison.Ordinal) ?? false))
        .ToList();

      foreach (var descriptor in descriptorsToRemove)
      {
        services.Remove(descriptor);
      }

      services.AddDbContext<EventsContext>(options => options.UseNpgsql(container.GetConnectionString()));

      ConfigureAuthentication(services);

      using var serviceProvider = services.BuildServiceProvider();
      using var scope = serviceProvider.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<EventsContext>();

      db.Database.EnsureDeleted();
      db.Database.EnsureCreated();
      ResetDatabase(db);
    });
  }

  protected abstract void ConfigureAuthentication(IServiceCollection services);

  public async Task InitializeAsync()
  {
    await container.StartAsync();
  }

  async Task IAsyncLifetime.DisposeAsync()
  {
    await container.DisposeAsync();
    Dispose();
  }

  public void ResetDatabase()
  {
    using var scope = Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EventsContext>();
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
    ResetDatabase(db);
  }

  private static void ResetDatabase(EventsContext db)
  {
    db.Countries.RemoveRange(db.Countries);
    db.Registrations.RemoveRange(db.Registrations);
    db.People.RemoveRange(db.People);
    db.Events.RemoveRange(db.Events);
    db.Sports.RemoveRange(db.Sports);
    db.SaveChanges();

    db.Countries.AddRange(
      new Country { Code = "HR", Name = "Croatia", Alpha3 = "HRV" },
      new Country { Code = "MK", Name = "Macedonia", Alpha3 = "MKD" },
      new Country { Code = "SI", Name = "Slovenia", Alpha3 = "SVN" });

    db.Sports.AddRange(
      new Sport { Name = "Athletics" },
      new Sport { Name = "Swimming" },
      new Sport { Name = "Football" });

    db.SaveChanges();
  }

  private static bool IsNpgsqlDescriptor(ServiceDescriptor descriptor)
  {
    return ContainsNpgsql(descriptor.ServiceType) ||
           ContainsNpgsql(descriptor.ImplementationType) ||
           ContainsNpgsql(descriptor.ImplementationInstance?.GetType()) ||
           ContainsNpgsql(descriptor.ImplementationFactory?.Method.ReturnType) ||
           ContainsNpgsql(descriptor.ImplementationFactory?.Method.DeclaringType);
  }

  private static bool ContainsNpgsql(Type? type)
  {
    if (type == null)
      return false;

    string? fullName = type.FullName;
    string? assemblyName = type.Assembly.GetName().Name;

    return (fullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false) ||
           string.Equals(assemblyName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);
  }
}
