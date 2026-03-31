namespace Events.WebAPI.Contract.DTOs;

public class IdName<T>
{
  public T Id { get; set; } = default!;

  public string Name { get; set; } = string.Empty;

  public string? Description { get; set; }
}
