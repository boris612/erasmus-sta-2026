```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.4 (24G517) [Darwin 24.6.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD


```
| Method                                 | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|--------------------------------------- |---------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| MediatR_Pipeline_InMemory              | 49.55 μs | 1.866 μs | 5.502 μs |  1.10 |    0.12 | 11.7188 | 1.2207 |  96.66 KB |        1.01 |
| Direct_Validator_Then_Handler_InMemory | 44.88 μs | 0.712 μs | 0.666 μs |  1.00 |    0.02 | 11.4746 | 1.2207 |  95.62 KB |        1.00 |
