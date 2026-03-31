using BenchmarkDotNet.Running;
using Events.WebAPI.Benchmarks;
string[] effectiveArgs = ExpandQuestionAlias(args);

var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
switcher.Run(effectiveArgs);

return 0;

static string[] ExpandQuestionAlias(string[] args)
{
  if (args.Length != 1)
    return args;

  return args[0] switch
  {
    "1" => ["--filter", "*Question1ProjectionBenchmarks*"],
    "2" => ["--filter", "*Question2InsertBenchmarks*"],
    "2a" => ["--filter", "*Question2AHttpInsertBenchmarks*"],
    "3" => ["--filter", "*Question3MediatRVsDirectBenchmarks*"],
    "3a" => ["--filter", "*Question3AInMemoryBenchmarks*"],
    "4" => ["--filter", "*Question4MessageBusBenchmarks*"],
    "4a" => ["--filter", "*Question4ARabbitMqBenchmarks*"],
    _ => args
  };
}
