# Benchmark results

To run benchmarks, use `dotnet run -c release`

## Insert
Cassandra was hosted in Docker on the same machine.

```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
AMD Ryzen 5 2600X, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100-rc.2.20479.15
  [Host]                     : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT
  Cassandra benchmark config : .NET Core 3.1.8 (CoreCLR 4.700.20.41105, CoreFX 4.700.20.41903), X64 RyuJIT

Job=Cassandra benchmark config  Jit=RyuJit  Platform=X64
Runtime=.NET Core 3.1  IterationCount=3  WarmupCount=1

|               Method | Iterations |     Mean |    Error |   StdDev |       Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|--------------------- |----------- |---------:|---------:|---------:|------------:|-----------:|----------:|----------:|
|      Add_SaveChanges |       1000 |  2.875 s | 2.0218 s | 0.1108 s |  15000.0000 |  1000.0000 |         - |  71.93 MB |
| AddRange_SaveChanges |       1000 |  2.730 s | 0.3026 s | 0.0166 s |  16000.0000 |  1000.0000 |         - |  71.91 MB |
|      Add_SaveChanges |      10000 | 27.420 s | 2.6518 s | 0.1454 s | 170000.0000 | 50000.0000 | 2000.0000 | 718.05 MB |
| AddRange_SaveChanges |      10000 | 27.291 s | 0.3736 s | 0.0205 s | 170000.0000 | 52000.0000 | 2000.0000 | 717.85 MB |
```