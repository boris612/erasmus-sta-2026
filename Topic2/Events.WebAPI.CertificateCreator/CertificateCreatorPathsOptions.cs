using System.ComponentModel.DataAnnotations;

namespace Events.WebAPI.CertificateCreator;

public class CertificateCreatorPathsOptions
{
  [Required]
  public string Certificates { get; set; } = string.Empty;
}
