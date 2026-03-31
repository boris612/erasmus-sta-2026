using System.ComponentModel.DataAnnotations;

namespace Events.WebAPI.Util.Settings;

public class RabbitMqSettings
{
  [Required]
  public string Host { get; set; } = string.Empty;

  [Required]
  public string Username { get; set; } = string.Empty;

  [Required]
  public string Password { get; set; } = string.Empty;
}
