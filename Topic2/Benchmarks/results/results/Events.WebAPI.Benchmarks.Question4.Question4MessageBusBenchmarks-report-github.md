```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.4 (24G517) [Darwin 24.6.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD


```
| Method                     | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0     | Gen1     | Allocated | Alloc Ratio |
|--------------------------- |---------:|---------:|---------:|---------:|------:|--------:|---------:|---------:|----------:|------------:|
| MediatR_With_MessageBus    | 13.48 ms | 0.265 ms | 0.436 ms | 13.26 ms |  1.05 |    0.04 | 250.0000 | 250.0000 |   3.68 MB |        1.00 |
| MediatR_Without_MessageBus | 12.85 ms | 0.252 ms | 0.310 ms | 12.92 ms |  1.00 |    0.03 | 250.0000 |  62.5000 |   3.66 MB |        1.00 |
