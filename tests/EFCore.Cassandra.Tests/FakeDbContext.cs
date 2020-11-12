// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace EFCore.Cassandra.Tests
{
    public class FakeDbContext : DbContext
    {
        private readonly Action<ModelBuilder> _modelCreatingCallback;
        private readonly Action<CassandraDbContextOptionsBuilder> _cassandraOptionsAction;

        public FakeDbContext(Action<CassandraDbContextOptionsBuilder> cassandraOptionsAction = null)
        {
            _cassandraOptionsAction = cassandraOptionsAction;
        }

        public FakeDbContext(Action<ModelBuilder> modelCreatingCallback)
        {
            _modelCreatingCallback = modelCreatingCallback;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseCassandra("connectionstring", "cv", _cassandraOptionsAction);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (_modelCreatingCallback != null)
            {
                _modelCreatingCallback(modelBuilder);
            }
        }
    }
}