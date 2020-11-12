// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraDatabaseCreator : RelationalDatabaseCreator
    {
        private readonly IRelationalConnection _relationalConnection;
        private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
        private readonly RelationalConnectionDependencies _relationalConnectionDependencies;

        public CassandraDatabaseCreator(IRelationalConnection relationalConnection, IRawSqlCommandBuilder rawCommandBuilder, RelationalConnectionDependencies relationalConnectionDependencies, RelationalDatabaseCreatorDependencies dependencies) : base(dependencies)
        {
            _relationalConnection = relationalConnection;
            _rawSqlCommandBuilder = rawCommandBuilder;
            _relationalConnectionDependencies = relationalConnectionDependencies;
        }

        public override void Create() { }

        public override void Delete() { }

        public override bool Exists()
        {
            try
            {
                return _relationalConnection.Open();
            }
            catch(Exception)
            {
                return false;
            }
            finally
            {
                _relationalConnection.Close();
            }
        }

        public override bool HasTables()
        {
            var optionsExtensions = CassandraOptionsExtension.Extract(_relationalConnectionDependencies.ContextOptions);
            var sql = $"SELECT count(*) FROM system_schema.tables WHERE keyspace_name='{optionsExtensions.DefaultKeyspace}'";
            var result = Dependencies.ExecutionStrategyFactory.Create().Execute(_relationalConnection, connection => (long)_rawSqlCommandBuilder.Build(sql).ExecuteScalar(
                new RelationalCommandParameterObject(connection, null, null, null, null)
                ) > 0);
            return result;
        }
    }
}
