using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Events.WebAPI.Controllers;

[ApiController]
[Route("_benchmarks/registrations")]
[ApiExplorerSettings(IgnoreApi = true)]
public class BenchmarkRegistrationsController : ControllerBase
{
  [HttpPost("ef-automapper")]
  [AllowAnonymous]
  public async Task<ActionResult<int>> CreateWithEfAutoMapper(
    RegistrationDTO model,
    [FromServices] EventsContext context,
    [FromServices] IMapper mapper,
    CancellationToken cancellationToken)
  {
    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

    Registration entity = mapper.Map<Registration>(model);
    entity.RegisteredAt = model.RegisteredAt;

    context.Registrations.Add(entity);
    await context.SaveChangesAsync(cancellationToken);
    await transaction.RollbackAsync(cancellationToken);

    return StatusCode(StatusCodes.Status201Created, entity.Id);
  }

  [HttpPost("ef-automapper-no-rollback")]
  [AllowAnonymous]
  public async Task<ActionResult<int>> CreateWithEfAutoMapperNoRollback(
    RegistrationDTO model,
    [FromServices] EventsContext context,
    [FromServices] IMapper mapper,
    CancellationToken cancellationToken)
  {
    Registration entity = mapper.Map<Registration>(model);
    entity.RegisteredAt = model.RegisteredAt;

    context.Registrations.Add(entity);
    await context.SaveChangesAsync(cancellationToken);

    return StatusCode(StatusCodes.Status201Created, entity.Id);
  }

  [HttpPost("adonet")]
  [AllowAnonymous]
  public async Task<ActionResult<int>> CreateWithAdoNet(
    RegistrationDTO model,
    [FromServices] EventsContext context,
    CancellationToken cancellationToken)
  {
    string connectionString = context.Database.GetConnectionString()
      ?? throw new InvalidOperationException("Connection string is not configured.");

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    await using var command = new NpgsqlCommand(
      """
      insert into registration (person_id, sport_id, event_id, registered_at)
      values (@person_id, @sport_id, @event_id, @registered_at)
      returning id;
      """,
      connection,
      transaction);

    command.Parameters.AddWithValue("person_id", model.PersonId);
    command.Parameters.AddWithValue("sport_id", model.SportId);
    command.Parameters.AddWithValue("event_id", model.EventId);
    command.Parameters.AddWithValue("registered_at", model.RegisteredAt);
    await command.PrepareAsync(cancellationToken);

    object? result = await command.ExecuteScalarAsync(cancellationToken);
    await transaction.RollbackAsync(cancellationToken);
    int id = Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    return StatusCode(StatusCodes.Status201Created, id);
  }

  [HttpPost("adonet-no-rollback")]
  [AllowAnonymous]
  public async Task<ActionResult<int>> CreateWithAdoNetNoRollback(
    RegistrationDTO model,
    [FromServices] EventsContext context,
    CancellationToken cancellationToken)
  {
    string connectionString = context.Database.GetConnectionString()
      ?? throw new InvalidOperationException("Connection string is not configured.");

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync(cancellationToken);

    await using var command = new NpgsqlCommand(
      """
      insert into registration (person_id, sport_id, event_id, registered_at)
      values (@person_id, @sport_id, @event_id, @registered_at)
      returning id;
      """,
      connection);

    command.Parameters.AddWithValue("person_id", model.PersonId);
    command.Parameters.AddWithValue("sport_id", model.SportId);
    command.Parameters.AddWithValue("event_id", model.EventId);
    command.Parameters.AddWithValue("registered_at", model.RegisteredAt);
    await command.PrepareAsync(cancellationToken);

    object? result = await command.ExecuteScalarAsync(cancellationToken);
    int id = Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    return StatusCode(StatusCodes.Status201Created, id);
  }
}
