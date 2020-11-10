// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using Cassandra.Data;
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EFCore.Cassandra.Storage.Internal
{
    public class EFCassandraDbConnection : CqlConnection
    {
        private readonly CassandraOptionsExtension _cassandraOptionsExtension;

        public EFCassandraDbConnection(string connectionString, RelationalConnectionDependencies dependencies) : base(connectionString)
        {
            _cassandraOptionsExtension = CassandraOptionsExtension.Extract(dependencies.ContextOptions);
        }

        protected override void OnBuildingCluster(Builder builder)
        {
            if (_cassandraOptionsExtension.ClusterBuilder != null)
            {
                _cassandraOptionsExtension.ClusterBuilder(builder);
            }
        }
    }
}
