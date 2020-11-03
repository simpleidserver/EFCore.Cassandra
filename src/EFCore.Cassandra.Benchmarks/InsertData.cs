// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using BenchmarkDotNet.Attributes;
using EFCore.Cassandra.Benchmarks.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFCore.Cassandra.Benchmarks
{
    [Config(typeof(CassandraBenchmarkConfig))]
    public class InsertData : IDisposable
    {
        private readonly FakeDbContext _dbContext;
        private readonly Applicant[] _applicants;

        public InsertData()
        {
            _dbContext = new FakeDbContext();
            _applicants = Enumerable.Range(1, 20).Select(_ => BuildApplicant()).ToArray();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Benchmark]
        public void AddApplicants()
        {
            foreach (var applicant in _applicants)
            {
                _dbContext.Applicants.Add(applicant);
            }

            _dbContext.SaveChanges();
        }

        [Benchmark]
        public void AddRangeApplicants()
        {
            _dbContext.Applicants.AddRange(_applicants);
            _dbContext.SaveChanges();
        }

        [Benchmark]
        public void BulkInsertApplicants()
        {
            _dbContext.BulkInsert(_applicants.ToList());
        }

        private static Applicant BuildApplicant()
        {
            return new Applicant
            {
                Id = Guid.NewGuid(),
                Order = 0,
                Dic = new Dictionary<string, string>(),
                Lst = new List<string>(),
                LstInt = new List<int>()
            };
        }
    }
}
