namespace EFCore.Cassandra.Benchmarks
{
  internal class Program
  {
    private static void Main(string[] args)
    {
#if DEBUG
      var benchmarks = new InsertFixture();
      benchmarks.Add_SaveChanges();
      benchmarks.AddRange_SaveChanges();
#else
      var summary = BenchmarkRunner.Run<InsertFixture>();
#endif
    }
  }
}