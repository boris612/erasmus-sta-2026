using Sieve.Attributes;

namespace Events.WebAPI.Contract.DTOs;

public class SportDTO : IHasIdAsPK<int>
{
  [Sieve(CanSort = true)]
  public int Id { get; set; }

  [Sieve(CanFilter = true, CanSort = true)]
  public string Name { get; set; } = string.Empty;
}
