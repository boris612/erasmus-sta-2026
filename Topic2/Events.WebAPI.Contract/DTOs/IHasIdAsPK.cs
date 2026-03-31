namespace Events.WebAPI.Contract.DTOs;

public interface IHasIdAsPK<T> where T : IEquatable<T>
{
  T Id { get; }
}
