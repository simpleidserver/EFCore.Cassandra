﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Migrations;
using Microsoft.EntityFrameworkCore.Cassandra.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Query;
using Microsoft.EntityFrameworkCore.Cassandra.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Storage;
using Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Update.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CassandraServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkCassandra(this IServiceCollection serviceCollection)
        {
            var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                .TryAdd<IDatabaseProvider, DatabaseProvider<CassandraOptionsExtension>>()
                .TryAdd<IRelationalTypeMappingSource, CassandraTypeMappingSource>()
                .TryAdd<IMemberTranslator, CassandraCompositeMemberTranslator>()
                .TryAdd<IMigrator, CassandraMigrator>()
                .TryAdd<ISqlGenerationHelper, CassandraSqlGenerationHelper>()
                .TryAdd<IMigrationsSqlGenerator, CassandraMigrationsSqlGenerator>()
                .TryAdd<IRelationalDatabaseCreator, CassandraDatabaseCreator>()
                .TryAdd<IMigrationCommandExecutor, CassandraMigrationCommandExecutor>()
                .TryAdd<IModificationCommandBatchFactory, CassandraModificationCommandBatchFactory>()
                .TryAdd<IUpdateSqlGenerator, CassandraUpdateSqlGenerator>()
                .TryAdd<IDatabaseCreator, CassandraDatabaseCreator>()
                .TryAdd<ICompositeMethodCallTranslator, CassandraCompositeMethodCallTranslator>()
                .TryAdd<IHistoryRepository, CassandraHistoryRepository>()
                .TryAdd<IBatchExecutor, CassandraBatchExecutor>()
                .TryAdd<IRelationalResultOperatorHandler, CassandraResultOperatorHandler>()
                .TryAdd<IRelationalValueBufferFactoryFactory, CassandraTypedRelationalValueBufferFactory>()
                .TryAdd<IRelationalConnection>(p => p.GetService<ICassandraRelationalConnection>())
                .TryAdd<IQuerySqlGeneratorFactory, CassandraQuerySqlGeneratorFactory>()
                .TryAddProviderSpecificServices(b => b.TryAddScoped<ICassandraRelationalConnection, CassandraRelationalConnection>())
                .TryAddProviderSpecificServices(b => b.TryAddTransient<ICassandraHistoryRepository, CassandraHistoryRepository>());
            
            builder.TryAddCoreServices();
            return serviceCollection;
        }
    }
}
