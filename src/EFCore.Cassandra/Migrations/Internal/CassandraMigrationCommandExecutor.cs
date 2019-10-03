// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Cassandra.Migrations.Internal
{
    public class CassandraMigrationCommandExecutor : IMigrationCommandExecutor
    {
        public void ExecuteNonQuery(IEnumerable<MigrationCommand> migrationCommands, IRelationalConnection connection)
        {
            connection.Open();
            try
            {
                foreach (var command in migrationCommands)
                {
                    command.ExecuteNonQuery(connection);
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task ExecuteNonQueryAsync(IEnumerable<MigrationCommand> migrationCommands, IRelationalConnection connection, CancellationToken cancellationToken = default)
        {
            connection.Open();
            try
            {
                foreach (var command in migrationCommands)
                {
                    await command.ExecuteNonQueryAsync(connection);
                }
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
