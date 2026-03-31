```

BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.4 (24G517) [Darwin 24.6.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 8.0.20 (8.0.2025.41914), Arm64 RyuJIT AdvSIMD


```
| Method                             | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|----------------------------------- |------------:|----------:|----------:|------:|--------:|---------:|--------:|--------:|-----------:|------------:|
| MediatR_With_RabbitMq_MessageBus   | 13,347.0 μs | 257.57 μs | 240.93 μs |  1.00 |    0.02 | 281.2500 | 93.7500 | 31.2500 | 3760.93 KB |       1.005 |
| MediatR_With_RabbitMq_Publish_Only |    739.4 μs |  13.71 μs |  12.16 μs |  0.06 |    0.00 |  19.5313 |       - |       - |  160.99 KB |       0.043 |
| JustPublishMessage                 |    171.1 μs |   2.69 μs |   2.25 μs |  0.01 |    0.00 |   1.4648 |       - |       - |   12.09 KB |       0.003 |
| MediatR_Without_MessageBus         | 13,319.9 μs | 255.01 μs | 238.54 μs |  1.00 |    0.02 | 265.6250 | 93.7500 | 31.2500 | 3742.75 KB |       1.000 |
