```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.4 (24G517) [Darwin 24.6.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD


```
| Method                                          | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------ |---------:|--------:|--------:|------:|--------:|--------:|-------:|----------:|------------:|
| Post_Registration_With_EF_AutoMapper            | 652.0 μs | 8.88 μs | 7.87 μs |  1.75 |    0.03 | 11.7188 | 1.9531 | 109.02 KB |        1.21 |
| Post_Registration_With_EF_AutoMapper_NoRollback | 302.7 μs | 2.65 μs | 2.48 μs |  0.81 |    0.01 | 12.6953 | 0.9766 |  104.8 KB |        1.16 |
| Post_Registration_With_AdoNet                   | 372.4 μs | 5.23 μs | 4.89 μs |  1.00 |    0.02 |  9.7656 | 1.9531 |  90.08 KB |        1.00 |
| Post_Registration_With_AdoNet_NoRollback        | 245.0 μs | 4.08 μs | 3.62 μs |  0.66 |    0.01 | 10.7422 | 0.9766 |  88.89 KB |        0.99 |
