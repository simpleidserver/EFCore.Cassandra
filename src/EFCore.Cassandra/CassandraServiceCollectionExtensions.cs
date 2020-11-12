// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using EFCore.Cassandra.Diagnostics.Internal;
using EFCore.Cassandra.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Migrations;
using Microsoft.EntityFrameworkCore.Cassandra.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Query.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Storage;
using Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Update.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CassandraServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkCassandra(this IServiceCollection serviceCollection)
        {
            var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                .TryAdd<IDatabaseProvider, DatabaseProvider<CassandraOptionsExtension>>()
                .TryAdd<IRelationalTypeMappingSource, CassandraTypeMappingSource>()
                .TryAdd<LoggingDefinitions, CassandraLoggingDefinitions>()
                .TryAdd<IMigrationsModelDiffer, CassandraMigrationsModelDiffer>()
                .TryAdd<ICommandBatchPreparer, CassandraCommandBatchPreparer>()
                // .TryAdd<IMemberTranslator, CassandraCompositeMemberTranslator>()
                .TryAdd<IMigrator, CassandraMigrator>()
                .TryAdd<ISqlGenerationHelper, CassandraSqlGenerationHelper>()
                .TryAdd<IMigrationsSqlGenerator, CassandraMigrationsSqlGenerator>()
                .TryAdd<IRelationalDatabaseCreator, CassandraDatabaseCreator>()
                .TryAdd<IModelValidator, CassandraModelValidator>()
                .TryAdd<IMigrationCommandExecutor, CassandraMigrationCommandExecutor>()
                .TryAdd<IModificationCommandBatchFactory, CassandraModificationCommandBatchFactory>()
                .TryAdd<IDatabaseCreator, CassandraDatabaseCreator>()
                // .TryAdd<ICompositeMethodCallTranslator, CassandraCompositeMethodCallTranslator>()
                .TryAdd<IHistoryRepository, CassandraHistoryRepository>()
                .TryAdd<IBatchExecutor, CassandraBatchExecutor>()
                // .TryAdd<IRelationalResultOperatorHandler, CassandraResultOperatorHandler>()
                .TryAdd<IRelationalValueBufferFactoryFactory, CassandraTypedRelationalValueBufferFactory>()
                .TryAdd<IRelationalConnection>(p => p.GetService<ICassandraRelationalConnection>())
                .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, CassandraSqlTranslatingExpressionVisitorFactory>()
                .TryAdd<IQuerySqlGeneratorFactory, CassandraSqlGeneratorFactory>()
                .TryAdd<IUpdateSqlGenerator, CassandraUpdateSqlGenerator>()
                .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, CassandraQueryableMethodTranslatingExpressionVisitorFactory>()
                .TryAddProviderSpecificServices(b => b.TryAddScoped<ICassandraRelationalConnection, CassandraRelationalConnection>())
                .TryAddProviderSpecificServices(b => b.TryAddTransient<ICassandraHistoryRepository, CassandraHistoryRepository>());
            
            builder.TryAddCoreServices();
            serviceCollection.AddSingleton<IServiceCollection>(serviceCollection);
            return serviceCollection;
        }
    }
}
