using Sieve.Attributes;

namespace Events.WebAPI.Contract.DTOs;

public class EventDTO : IHasIdAsPK<int>
{
  [Sieve(CanSort = true)]
  public int Id { get; set; }

  [Sieve(CanFilter = true, CanSort = true)]
  public string Name { get; set; } = string.Empty;

  [Sieve(CanFilter = true, CanSort = true)]
  public DateOnly EventDate { get; set; }

  [Sieve(CanSort = true)]
  public int RegistrationsCount { get; set; }
}
