namespace Events.WebAPI.Benchmarks.Question1;

internal sealed record ProjectionFixture(int EventId, int SportId, int[] PersonIds, DateTime Marker);
