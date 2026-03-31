using System.ComponentModel.DataAnnotations;

namespace Events.WebAPI.ExcelExporter;

public class ExcelExporterPathsOptions
{
  [Required]
  public string Certificates { get; set; } = string.Empty;
}
