// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using Cassandra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace EFCore.Cassandra.Bulk
{
    internal class SqlBulkOperation
    {
        public static void Insert<T>(DbContext dbContext, IList<T> entities)
        {
            var result = BuildBatch(dbContext, entities);
            result.session.Execute(result.batchStatement);
        }

        public static async Task InsertAsync<T>(DbContext dbContext, IList<T> entities)
        {
            var result = BuildBatch(dbContext, entities);
            await result.session.ExecuteAsync(result.batchStatement);
        }

        private static (BatchStatement batchStatement, ISession session) BuildBatch<T>(DbContext dbContext, IList<T> entities)
        {
            var service = dbContext.GetService<ICommandBatchPreparer>();
            var sqlGenerationHelper = dbContext.GetService<ISqlGenerationHelper>();
            var relationalConnectionDependencies = dbContext.GetService<RelationalConnectionDependencies>();
            var cassandraOptionsExtension = CassandraOptionsExtension.Extract(relationalConnectionDependencies.ContextOptions);
            var database = dbContext.Database.GetDbConnection() as CqlConnection;
            if (database.State != ConnectionState.Open)
            {
                database.Open();
            }

            var prop = typeof(CqlConnection).GetField("ManagedConnection", BindingFlags.NonPublic | BindingFlags.Instance);
            var session = (ISession)prop.GetValue(database);
            var batch = new BatchStatement();
            foreach (var entity in entities)
            {
                var name = entity.GetType().FullName;
                var entityType = dbContext.Model.FindEntityType(name);
                var propertyNames = new List<string>();
                var propertyValues = new List<object>();
                foreach (var property in entityType.GetProperties())
                {
                    propertyNames.Add(sqlGenerationHelper.DelimitIdentifier(property.GetColumnName()));
                    var propValue = property.PropertyInfo.GetValue(entity);
                    propValue = GetValue(property, propValue);
                    propertyValues.Add(propValue);
                }

                var tableName = entityType.GetTableName();
                var schema = cassandraOptionsExtension.DefaultKeyspace;
                var cqlQuery = $"INSERT INTO \"{schema}\".\"{tableName}\" ({string.Join(',', propertyNames)}) VALUES ({string.Join(',', Enumerable.Repeat(1, propertyNames.Count()).Select(_ => "?"))})";
                var smt = session.Prepare(cqlQuery);
                batch.Add(smt.Bind(propertyValues.ToArray()));
            }

            return (batch, session);
        }

        private static object GetValue(IProperty property, object value)
        {
            if(property.ClrType == typeof(IPAddress))
            {
                if (value == null)
                {
                    return new byte[0];
                }

                return ((IPAddress)value).GetAddressBytes();
            }

            return value;
        }
    }
}
