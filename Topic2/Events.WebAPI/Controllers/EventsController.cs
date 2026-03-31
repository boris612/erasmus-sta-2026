using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Queries.Generic;
using Events.WebAPI.Contract.Services.EventRegistrationsExcel;
using Events.WebAPI.Controllers.Generic;
using Events.WebAPI.Util.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Events.WebAPI.Controllers;

public class EventsController : CrudController<EventDTO, int>
{
  [HttpGet("{id}/RegistrationsExcel")]
  [ProducesResponseType(typeof(PhysicalFileResult), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> DownloadRegistrationsExcel(
    int id,
    [FromServices] IMediator mediator,
    [FromServices] IEventRegistrationsExcelService excelService,
    CancellationToken cancellationToken)
  {
    var item = await mediator.Send(new GetSingleItemQuery<EventDTO, int>(id), cancellationToken);
    if (item == null)
    {
      return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
    }

    var result = await this.ReturnEventRegistrationsExcel(excelService, id, cancellationToken);
    if (result == null)
    {
      await excelService.SynchronizeAsync(id, cancellationToken);
      result = await this.ReturnEventRegistrationsExcel(excelService, id, cancellationToken);
    }

    if (result == null)
    {
      return Problem(statusCode: StatusCodes.Status404NotFound, detail: "Registrations Excel could not be generated.");
    }

    return result;
  }
}
