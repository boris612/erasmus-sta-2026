using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Events.WebAPI.Util.Settings;

namespace Events.WebAPI.Benchmarks.Question4;

internal sealed class RabbitMqBenchmarkContainer : IAsyncDisposable
{
  private const int AmqpPort = 5672;
  private const string DefaultImage = "rabbitmq:4-management-alpine";
  private const string Username = "benchmark";
  private const string Password = "benchmark";
  private readonly IContainer container;

  public RabbitMqBenchmarkContainer(string image = DefaultImage)
  {
    container = new ContainerBuilder()
      .WithImage(image)
      .WithPortBinding(AmqpPort, assignRandomHostPort: true)
      .WithEnvironment("RABBITMQ_DEFAULT_USER", Username)
      .WithEnvironment("RABBITMQ_DEFAULT_PASS", Password)
      .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(AmqpPort))
      .WithCleanUp(true)
      .Build();
  }

  public RabbitMqSettings Settings => new()
  {
    Host = $"rabbitmq://localhost:{container.GetMappedPublicPort(AmqpPort)}",
    Username = Username,
    Password = Password
  };

  public Task StartAsync(CancellationToken cancellationToken = default)
  {
    return container.StartAsync(cancellationToken);
  }

  public ValueTask DisposeAsync()
  {
    return container.DisposeAsync();
  }
}
