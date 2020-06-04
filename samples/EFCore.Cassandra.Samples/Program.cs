// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using EFCore.Cassandra.Samples.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace EFCore.Cassandra.Samples
{
    class Program
    {
        private static Guid ApplicantPartitionId = Guid.Parse("be2106c5-791f-45d2-890a-50fc221f96e8");
        private static Guid ApplicantId = Guid.Parse("09e0f68e-8818-452a-9a47-3c8ca2c941c8");

        static void Main(string[] args)
        {
            using (var dbContext = new FakeDbContext())
            {
                Console.WriteLine("Add applicant");
                var timeUuid = TimeUuid.NewId();
                dbContext.Applicants.Add(BuildApplicant());
                dbContext.SaveChanges();
                Console.WriteLine("Applicant is added");

                var appls = dbContext.Applicants.ToList();
                Console.WriteLine($"Number of applicants '{dbContext.Applicants.LongCount()}'");

                Console.WriteLine("Get applicants by partition key");
                var filteredApplicants = dbContext.Applicants.Where(_ => _.Id == ApplicantPartitionId, false).ToList();
                Console.WriteLine($"Number of applicants '{filteredApplicants.Count}'");

                Console.WriteLine("Get applicants (ALLOW FILTERING)");
                var allowedFilteredApplicants = dbContext.Applicants.Where(_ => _.LastName == "lastname", true).ToList();
                Console.WriteLine($"Number of applicants {allowedFilteredApplicants.Count}");

                Console.WriteLine("Order applicants by 'order'");
                var orderedApplicants = dbContext.Applicants.Where(_ => _.Id == ApplicantPartitionId)
                    .OrderBy(_ => _.Order).ToList();
                Console.WriteLine($"Number of applicants {orderedApplicants.Count}");

                Console.WriteLine("Update the applicant");
                var applicant = dbContext.Applicants.First();
                applicant = dbContext.Applicants.First();
                applicant.Decimal = 10;
                applicant.Dic = new Dictionary<string, string>
                {
                    { "toto", "toto" }
                };
                dbContext.SaveChanges();
                Console.WriteLine("Applicant is updated");

                Console.WriteLine("Remove the applicant");
                applicant = dbContext.Applicants.First();
                dbContext.Applicants.Remove(applicant);
                dbContext.SaveChanges();
                Console.WriteLine("Applicant is removed");

                Console.WriteLine($"Number of applicants '{dbContext.Applicants.LongCount()}'");
                Console.ReadLine();
            }
        }

        private static Applicant BuildApplicant()
        {
            var timeUuid = TimeUuid.NewId();
            return new Applicant
            {
                Id = ApplicantPartitionId,
                ApplicantId = ApplicantId,
                Order = 0,
                Lst = new List<string>
                {
                    "1",
                    "2"
                },
                LstInt = new List<int>
                {
                    1, 2, 3
                },
                Dic = new Dictionary<string, string>
                {
                    { "coucou", "coucou" }
                },
                LastName = "lastname",
                BigInteger = 10,
                Bool = false,
                Decimal = 1,
                Double = 2,
                Integer = 3,
                Float = 4,
                Sbyte = 0,
                TimeUuid = timeUuid,
                DateTimeOffset = DateTimeOffset.Now,
                Long = 22,
                SmallInt = 11,
                Blob = new byte[] { 1, 2 },
                LocalDate = new LocalDate(2019, 10, 2),
                Ip = IPAddress.Loopback,
                LocalTime = new LocalTime(2, 3, 4, 5)
            };
        }
    }
}
