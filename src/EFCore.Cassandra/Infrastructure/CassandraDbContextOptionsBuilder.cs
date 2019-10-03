// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    public class CassandraDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<CassandraDbContextOptionsBuilder, CassandraOptionsExtension>
    {
        public CassandraDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder) : base(optionsBuilder)
        {
        }
    }
}
