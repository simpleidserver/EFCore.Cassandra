# Benchmark results

## InsertData

```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 5 2600X, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100-rc.2.20479.15
  [Host]                     : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT
  Cassandra benchmark config : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT

Job=Cassandra benchmark config  Jit=RyuJit  Platform=X64
Runtime=.NET Core 3.1  IterationCount=3  WarmupCount=1

|               Method |     Mean |     Error |   StdDev | Gen 0 | Gen 1 | Gen 2 |  Allocated |
|--------------------- |---------:|----------:|---------:|------:|------:|------:|-----------:|
|        AddApplicants | 64.82 ms | 53.485 ms | 2.932 ms |     - |     - |     - | 1632.24 KB |
|   AddRangeApplicants | 65.13 ms | 46.057 ms | 2.525 ms |     - |     - |     - | 1634.05 KB |
| BulkInsertApplicants | 29.42 ms |  5.828 ms | 0.319 ms |     - |     - |     - |  556.35 KB |
```


# BarchedInsertData

```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 5 2600X, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100-rc.2.20479.15
  [Host]                     : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT
  Cassandra benchmark config : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT

Job=Cassandra benchmark config  Jit=RyuJit  Platform=X64
Runtime=.NET Core 3.1  IterationCount=3  WarmupCount=1

|                       Method | Iterations | BatchSize |         Mean |      Error |    StdDev |      Gen 0 |     Gen 1 | Gen 2 |    Allocated |
|----------------------------- |----------- |---------- |-------------:|-----------:|----------:|-----------:|----------:|------:|-------------:|
| BatchedInsertApplicantsAsync |          1 |        10 |     17.98 ms |   8.822 ms |  0.484 ms |          - |         - |     - |    282.18 KB |
| BatchedInsertApplicantsAsync |          1 |       100 |    134.66 ms | 133.414 ms |  7.313 ms |          - |         - |     - |   2731.55 KB |
| BatchedInsertApplicantsAsync |         10 |        10 |    138.43 ms |  78.095 ms |  4.281 ms |          - |         - |     - |   2795.91 KB |
| BatchedInsertApplicantsAsync |         10 |       100 |  1,186.80 ms |  99.393 ms |  5.448 ms |  5000.0000 | 1000.0000 |     - |  25882.28 KB |
| BatchedInsertApplicantsAsync |        100 |        10 |  1,227.66 ms |  75.714 ms |  4.150 ms |  6000.0000 | 1000.0000 |     - |  26384.59 KB |
| BatchedInsertApplicantsAsync |        100 |       100 | 11,802.71 ms | 582.269 ms | 31.916 ms | 63000.0000 | 2000.0000 |     - | 258819.98 KB |
```