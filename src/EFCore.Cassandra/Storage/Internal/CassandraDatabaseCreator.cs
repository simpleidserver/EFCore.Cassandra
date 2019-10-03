// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraDatabaseCreator : RelationalDatabaseCreator
    {
        private readonly IRelationalConnection _relationalConnection;
        private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;

        public CassandraDatabaseCreator(IRelationalConnection relationalConnection, IRawSqlCommandBuilder rawCommandBuilder, RelationalDatabaseCreatorDependencies dependencies) : base(dependencies)
        {
            _relationalConnection = relationalConnection;
            _rawSqlCommandBuilder = rawCommandBuilder;
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

        protected override bool HasTables()
        {
            var database = Dependencies.Connection.DbConnection.Database;
            var sql = $"SELECT count(*) FROM system_schema.tables WHERE keyspace_name='{database}'";
            return Dependencies.ExecutionStrategyFactory.Create().Execute(_relationalConnection, connection => (int)_rawSqlCommandBuilder.Build(sql).ExecuteScalar(connection) > 0);
        }
    }
}
