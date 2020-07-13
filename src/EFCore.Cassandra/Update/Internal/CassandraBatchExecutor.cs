// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Microsoft.EntityFrameworkCore.Cassandra.Update.Internal
{
    public class CassandraBatchExecutor : IBatchExecutor
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public CassandraBatchExecutor(ICurrentDbContext currentContext, IExecutionStrategyFactory executionStrategyFactory)
        {
            CurrentContext = currentContext;
            ExecutionStrategyFactory = executionStrategyFactory;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual ICurrentDbContext CurrentContext { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected virtual IExecutionStrategyFactory ExecutionStrategyFactory { get; }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual int Execute(
            IEnumerable<ModificationCommandBatch> commandBatches,
            IRelationalConnection connection)
            => CurrentContext.Context.Database.AutoTransactionsEnabled
                ? ExecutionStrategyFactory.Create().Execute((commandBatches, connection), Execute, null)
                : Execute(CurrentContext.Context, (commandBatches, connection));

        private int Execute(DbContext _, (IEnumerable<ModificationCommandBatch>, IRelationalConnection) parameters)
        {
            var commandBatches = parameters.Item1;
            var connection = parameters.Item2;
            var rowsAffected = 0;
            try
            {
                connection.Open();
                foreach (var batch in commandBatches)
                {
                    batch.Execute(connection);
                    rowsAffected += batch.ModificationCommands.Count;
                }
            }
            finally
            {
                connection.Close();
            }

            return rowsAffected;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual Task<int> ExecuteAsync(
            IEnumerable<ModificationCommandBatch> commandBatches,
            IRelationalConnection connection,
            CancellationToken cancellationToken = default)
            => CurrentContext.Context.Database.AutoTransactionsEnabled
                ? ExecutionStrategyFactory.Create().ExecuteAsync((commandBatches, connection), ExecuteAsync, null, cancellationToken)
                : ExecuteAsync(CurrentContext.Context, (commandBatches, connection), cancellationToken);

        private async Task<int> ExecuteAsync(
            DbContext _,
            (IEnumerable<ModificationCommandBatch>, IRelationalConnection) parameters,
            CancellationToken cancellationToken = default)
        {
            var commandBatches = parameters.Item1;
            var connection = parameters.Item2;
            var rowsAffected = 0;
            try
            {
                await connection.OpenAsync(cancellationToken);
                foreach (var batch in commandBatches)
                {
                    await batch.ExecuteAsync(connection, cancellationToken);
                    rowsAffected += batch.ModificationCommands.Count;
                }
            }
            finally
            {
                connection.Close();
            }

            return rowsAffected;
        }
    }
}
