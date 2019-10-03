// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;

namespace EFCore.Cassandra.Tests.Models
{
    public class Applicant
    {
        public Guid Id { get; set; }
        public string LastName { get; set; }
        public string ApplicationName { get; set; }
    }
}