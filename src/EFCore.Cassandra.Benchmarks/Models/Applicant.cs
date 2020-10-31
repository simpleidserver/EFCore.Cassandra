// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;

namespace EFCore.Cassandra.Benchmarks.Models
{
    public class Applicant
    {
        public Guid Id { get; set; }
        public int Order { get; set; }
        public Guid ApplicantId { get; set; }
        public string LastName { get; set; }
        public long Long { get; set; }
        public bool Bool { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public int Integer { get; set; }
        public short SmallInt { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public TimeUuid TimeUuid { get; set; }
        public sbyte Sbyte { get; set; }
        public BigInteger BigInteger { get; set; }
        public byte[] Blob { get; set; }
        public LocalDate LocalDate { get; set; }
        public IPAddress Ip { get; set; }
        public LocalTime LocalTime { get; set; }
        public IList<string> Lst { get; set; }
        public IList<int> LstInt { get; set; }
        public IDictionary<string, string> Dic { get; set; }
        public ApplicantAddress Address { get; set; }
    }
}