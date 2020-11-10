using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace EFCore.Cassandra.Benchmarks
{
    [Config(typeof(CassandraBenchmarkConfig))]
    public class CassandraBenchmarkConfig : ManualConfig
    {
        public CassandraBenchmarkConfig()
        {
            AddJob(
              Job.Default
                .WithPlatform(Platform.X64)
                .WithJit(Jit.RyuJit)
                .WithRuntime(CoreRuntime.Core31)
                .WithWarmupCount(1)
                .WithIterationCount(3)
                .WithId("Cassandra benchmark config"));

            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}