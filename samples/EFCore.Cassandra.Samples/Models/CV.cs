// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;

namespace EFCore.Cassandra.Samples.Models
{
    public class CV
    {
        public Guid Id { get; set; }
        public Guid CvId { get; set; }
        public string Name { get; set; }
    }
}
