using Events.WebAPI.Contract.Services;
using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.Contract.Services.EventRegistrationsExcel;
using Microsoft.AspNetCore.Mvc;

namespace Events.WebAPI.Util.Extensions;

public static class ControllerCertificateExtensions
{
  private const string PdfContentType = "application/pdf";
  private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

  public static async Task<PhysicalFileResult?> ReturnCertificate(
    this ControllerBase controller,
    IRegistrationCertificateService certificateService,
    int eventId,
    int personId,
    CancellationToken cancellationToken)
  {
    GeneratedFileReference? certificate = await certificateService.GetCertificateAsync(eventId, personId, cancellationToken);
    if (certificate == null)
      return null;

    return controller.PhysicalFile(certificate.PhysicalPath, PdfContentType, certificate.FileName);
  }

  public static async Task<PhysicalFileResult?> ReturnEventRegistrationsExcel(
    this ControllerBase controller,
    IEventRegistrationsExcelService excelService,
    int eventId,
    CancellationToken cancellationToken)
  {
    GeneratedFileReference? excel = await excelService.GetAsync(eventId, cancellationToken);
    if (excel == null)
      return null;

    return controller.PhysicalFile(excel.PhysicalPath, XlsxContentType, excel.FileName);
  }
}
