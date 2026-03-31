using BenchmarkDotNet.Attributes;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.CommandHandlers;
using Events.WebAPI.Handlers.EF.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Events.WebAPI.Benchmarks.Question3A;

[MemoryDiagnoser]
[ArtifactsPath("Topic2/Benchmarks/results")]
public class Question3AInMemoryBenchmarks
{
  private Question3AInMemoryInfrastructure infrastructure = null!;
  private Question3AFixture fixture = null!;

  [GlobalSetup]
  public async Task Setup()
  {
    infrastructure = new Question3AInMemoryInfrastructure();
    fixture = await infrastructure.SeedAsync();
  }

  [GlobalCleanup]
  public void Cleanup()
  {
    infrastructure.ServiceProvider.Dispose();
  }

  [Benchmark]
  public async Task<int> MediatR_Pipeline_InMemory()
  {
    await infrastructure.ResetRegistrationsAsync();

    using IServiceScope scope = infrastructure.ServiceProvider.CreateScope();
    IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    return await mediator.Send(new AddCommand<RegistrationDTO, int>(
      infrastructure.CreateRegistrationDto(fixture)));
  }

  [Benchmark(Baseline = true)]
  public async Task<int> Direct_Validator_Then_Handler_InMemory()
  {
    await infrastructure.ResetRegistrationsAsync();

    using IServiceScope scope = infrastructure.ServiceProvider.CreateScope();
    var command = new AddCommand<RegistrationDTO, int>(infrastructure.CreateRegistrationDto(fixture));
    IValidator<AddCommand<RegistrationDTO, int>> validator = scope.ServiceProvider.GetRequiredService<IValidator<AddCommand<RegistrationDTO, int>>>();
    RegistrationsCommandsHandler handler = scope.ServiceProvider.GetRequiredService<RegistrationsCommandsHandler>();

    await validator.ValidateAndThrowAsync(command);
    return await handler.Handle(command, CancellationToken.None);
  }
}
