```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.4 (24G517) [Darwin 24.6.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD


```
| Method                        | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| MediatR_Pipeline              | 588.0 μs | 11.16 μs | 13.70 μs |  1.00 |    0.03 |  9.7656 |      - |  91.79 KB |        1.01 |
| Direct_Validator_Then_Handler | 589.3 μs | 11.02 μs | 10.31 μs |  1.00 |    0.02 | 10.7422 | 1.9531 |  90.59 KB |        1.00 |
