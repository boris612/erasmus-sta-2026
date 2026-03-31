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

namespace Events.WebAPI.Benchmarks.Question2A;

internal sealed class Question2ABenchmarkWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Benchmarking");

    builder.ConfigureServices(services =>
    {
      services.RemoveAll<DbContextOptions<EventsContext>>();
      services.RemoveAll<EventsContext>();
      services.RemoveAll<IDbContextOptionsConfiguration<EventsContext>>();

      var npgsqlDescriptorsToRemove = services
        .Where(IsNpgsqlDescriptor)
        .ToList();

      foreach (var descriptor in npgsqlDescriptorsToRemove)
        services.Remove(descriptor);

      var descriptorsToRemove = services
        .Where(descriptor =>
          descriptor.ServiceType == typeof(IHostedService) ||
          (descriptor.ImplementationType?.Namespace?.StartsWith("MassTransit", StringComparison.Ordinal) ?? false) ||
          (descriptor.ServiceType.Namespace?.StartsWith("MassTransit", StringComparison.Ordinal) ?? false))
        .ToList();

      foreach (var descriptor in descriptorsToRemove)
        services.Remove(descriptor);

      services.AddDbContext<EventsContext>(options => options.UseNpgsql(connectionString));
    });
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
