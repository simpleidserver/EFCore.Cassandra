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
        static void Main(string[] args)
        {
            using (var dbContext = new FakeDbContext())
            {
                Console.WriteLine("Add applicant");
                var timeUuid = TimeUuid.NewId();
                dbContext.Applicants.Add(BuildApplicant());
                dbContext.SaveChanges();

                Console.WriteLine($"Number of applicants : {dbContext.Applicants.LongCount()}");

                Console.WriteLine("Update the applicant");
                var applicant = dbContext.Applicants.First();
                applicant.Decimal = 10;
                applicant.Dic = new Dictionary<string, string>
                {
                    { "toto", "toto" }
                };
                dbContext.SaveChanges();

                Console.WriteLine("Remove the applicant");
                applicant = dbContext.Applicants.First();
                dbContext.Applicants.Remove(applicant);
                dbContext.SaveChanges();

                Console.WriteLine($"Number of applicants : {dbContext.Applicants.LongCount()}");
            }
        }

        private static Applicant BuildApplicant()
        {
            var timeUuid = TimeUuid.NewId();
            return new Applicant
            {
                Id = Guid.NewGuid(),
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
