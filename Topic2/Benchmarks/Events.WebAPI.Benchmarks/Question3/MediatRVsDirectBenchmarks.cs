using BenchmarkDotNet.Attributes;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.CommandHandlers;
using Events.WebAPI.Handlers.EF.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Events.WebAPI.Benchmarks.Question3;

[MemoryDiagnoser]
[ArtifactsPath("Topic2/Benchmarks/results")]
public class Question3MediatRVsDirectBenchmarks
{
  private RegistrationBenchmarkInfrastructure infrastructure = null!;
  private RegistrationBenchmarkFixture fixture = null!;
  private ServiceProvider serviceProvider = null!;

  [GlobalSetup]
  public async Task Setup()
  {
    string connectionString = await PostgreSqlBenchmarkSupport.LoadConnectionStringAsync();

    infrastructure = new RegistrationBenchmarkInfrastructure(connectionString, NullLoggerFactory.Instance);
    fixture = await infrastructure.EnsureFixtureAsync(requiredPeople: 1);
    serviceProvider = infrastructure.BuildServiceProvider(useMassTransitHarness: false, useSimplifiedRegistrationValidator: true);
  }

  [GlobalCleanup]
  public void Cleanup()
  {
    serviceProvider.Dispose();
  }

  [Benchmark]
  public async Task<int> MediatR_Pipeline()
  {
    using IServiceScope scope = serviceProvider.CreateScope();
    EventsContext context = scope.ServiceProvider.GetRequiredService<EventsContext>();
    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();
    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    int id = await mediator.Send(new AddCommand<RegistrationDTO, int>(
      infrastructure.CreateRegistrationDto(fixture.DispatchEventId, fixture.SportId, fixture.PersonIds[0])));

    await transaction.RollbackAsync();
    return id;
  }

  [Benchmark(Baseline = true)]
  public async Task<int> Direct_Validator_Then_Handler()
  {
    using IServiceScope scope = serviceProvider.CreateScope();
    EventsContext context = scope.ServiceProvider.GetRequiredService<EventsContext>();
    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();
    var command = new AddCommand<RegistrationDTO, int>(
      infrastructure.CreateRegistrationDto(fixture.DispatchEventId, fixture.SportId, fixture.PersonIds[0]));
    IValidator<AddCommand<RegistrationDTO, int>> validator = scope.ServiceProvider.GetRequiredService<IValidator<AddCommand<RegistrationDTO, int>>>();
    RegistrationsCommandsHandler handler = scope.ServiceProvider.GetRequiredService<RegistrationsCommandsHandler>();

    await validator.ValidateAndThrowAsync(command);
    int id = await handler.Handle(command, CancellationToken.None);

    await transaction.RollbackAsync();
    return id;
  }
}
