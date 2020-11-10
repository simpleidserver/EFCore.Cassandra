using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EFCore.Cassandra.Benchmarks.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Cassandra.Benchmarks
{
    [Config(typeof(CassandraBenchmarkConfig))]
    public class BatchedInsertData : IDisposable
    {
        private readonly FakeDbContext _dbContext;
        private Applicant[][] _applicants;

        public BatchedInsertData()
        {
            _dbContext = new FakeDbContext();
        }

        [ParamsSource(nameof(IterationsSource))]
        public int Iterations { get; set; } = 1000;

        public IEnumerable<int> IterationsSource => new[] {1, 10, 100};

        [ParamsSource(nameof(BatchSizeSource))]
        public int BatchSize { get; set; } = 10;

        public IEnumerable<int> BatchSizeSource => new[] {10, 100};

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [GlobalSetup]
        public void SetupFixture()
        {
            _applicants = Enumerable.Range(0, Iterations).Select(_ => BuildApplicants()).ToArray();
        }

        [Benchmark]
        public async Task BatchedInsertApplicantsAsync()
        {
            var a = Enumerable.Range(0, Iterations).Select(i => _dbContext.BulkInsertAsync(_applicants[i].ToList()));
            await Task.WhenAll(a);
        }

        private Applicant[] BuildApplicants()
        {
            return Enumerable.Range(0, BatchSize).Select(_ => BuildApplicant()).ToArray();
        }

        private static Applicant BuildApplicant() =>
            new Applicant
            {
                Id = Guid.NewGuid(),
                Order = 0,
                Dic = new Dictionary<string, string>(),
                Lst = new List<string>(),
                LstInt = new List<int>()
            };
    }
}