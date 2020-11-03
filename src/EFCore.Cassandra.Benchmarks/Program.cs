// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace EFCore.Cassandra.Benchmarks
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
#if DEBUG
            var insertData = new InsertData();
            insertData.AddApplicants();
            insertData.BulkInsertApplicants();
            insertData.AddRangeApplicants();

            var batchedInsertData = new BatchedInsertData();
            await batchedInsertData.BatchedInsertApplicantsAsync();
#else
            BenchmarkRunner.Run<InsertData>();
            BenchmarkRunner.Run<BatchedInsertData>();
#endif
        }
    }
}