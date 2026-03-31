using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Controllers.Generic;
using Events.WebAPI.Contract.Queries.Generic;
using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.Util.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Events.WebAPI.Controllers;

public class RegistrationsController : CrudController<RegistrationDTO, int>
{
  [HttpGet("{id}/Certificate")]
  [ProducesResponseType(typeof(PhysicalFileResult), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> DownloadCertificate(
    int id,
    [FromServices] IMediator mediator,
    [FromServices] IRegistrationCertificateService certificateService,
    CancellationToken cancellationToken)
  {
    var registration = await mediator.Send(new GetSingleItemQuery<RegistrationDTO, int>(id), cancellationToken);
    if (registration == null)
    {
      return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
    }

    var result = await this.ReturnCertificate(certificateService, registration.EventId, registration.PersonId, cancellationToken);
    if (result == null)
    {
      await certificateService.SynchronizeCertificateAsync(registration.EventId, registration.PersonId, cancellationToken);
      result = await this.ReturnCertificate(certificateService, registration.EventId, registration.PersonId, cancellationToken);
    }

    if (result == null)
    {
      return Problem(statusCode: StatusCodes.Status404NotFound, detail: "Certificate could not be generated.");
    }

    return result;
  }
}
