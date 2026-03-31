namespace Events.WebAPI.Benchmarks.Question2;

internal sealed record InsertFixture(int EventId, int SportId, int[] PersonIds, DateTime Marker, string ProcedureName);
