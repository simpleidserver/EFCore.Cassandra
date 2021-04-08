// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Cassandra;
using Cassandra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.Cassandra.Storage.Internal
{
    public class EFCassandraDbConnection : CqlConnection
    {
        private readonly static ConcurrentDictionary<string, Cluster> _clusters = new ConcurrentDictionary<string, Cluster>();
        private CassandraConnectionStringBuilder _connectionStringBuilder;
        private readonly CassandraOptionsExtension _cassandraOptionsExtension;
        private readonly ICurrentDbContext _currentDbContext;

        public EFCassandraDbConnection(ICurrentDbContext currentDbContext, string connectionString, RelationalConnectionDependencies dependencies) : base(connectionString)
        {
            _currentDbContext = currentDbContext;
            _cassandraOptionsExtension = CassandraOptionsExtension.Extract(dependencies.ContextOptions);
            _connectionStringBuilder = new CassandraConnectionStringBuilder(connectionString);
        }

        protected override void OnBuildingCluster(Builder builder)
        {
            if (_cassandraOptionsExtension.ClusterBuilder != null)
            {
                _cassandraOptionsExtension.ClusterBuilder(builder);
            }
        }

        protected override Cluster CreateCluster(CassandraConnectionStringBuilder connectionStringBuilder)
        {
            if (!_clusters.TryGetValue(_connectionStringBuilder.ClusterName, out Cluster cluster))
            {
                Builder builder;
                if (_cassandraOptionsExtension.IsConnectionSecured)
                {
                    builder = Cluster.Builder();
                }
                else
                {
                    builder = _connectionStringBuilder.MakeClusterBuilder();
                }

                OnBuildingCluster(builder);
                cluster = builder.Build();
                _clusters.TryAdd(_connectionStringBuilder.ClusterName, cluster);
            }

            return cluster;
        }

        protected override ISession CreatedSession(string keyspace)
        {
            Cluster cluster;
            if (!_clusters.TryGetValue(_connectionStringBuilder.ClusterName, out cluster))
            {
                return null;
            }

            var assms = AppDomain.CurrentDomain.GetAssemblies();
            var session = cluster.Connect(keyspace ?? string.Empty);
            var context = _currentDbContext.Context;
            var entityTypes = context.Model.GetEntityTypes();
            foreach(var entityType in entityTypes)
            {
                if (entityType.IsUserDefinedType())
                {
                    var arg = entityType.ClrType;
                    var udtMap = typeof(UdtMap);
                    var member = udtMap.GetMethod("For").MakeGenericMethod(arg);
                    var map = member.Invoke(udtMap, new object[] { entityType.GetTableName(), _cassandraOptionsExtension.DefaultKeyspace });
                    var genericUdtMap = typeof(UdtMap<>).MakeGenericType(arg);
                    var properties = typeof(EntityType).GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic);
                    var props = properties.GetValue(entityType) as SortedDictionary<string, Property>;
                    foreach (var prop in props)
                    {
                        /*
                        if (!CassandraMigrationsModelDiffer.CheckProperty(assms, prop.Value))
                        {
                            continue;
                        }
                        */

                        var mapMethod = genericUdtMap.GetMethod("Map").MakeGenericMethod(prop.Value.ClrType);
                        var variable = Expression.Variable(arg, "v");
                        var p = Expression.Property(variable, prop.Key);
                        var lambda = Expression.Lambda(p, variable);
                        var colName = prop.Value.GetColumnName();
                        colName = string.IsNullOrWhiteSpace(colName) ? prop.Value.Name : colName;
                        map = mapMethod.Invoke(map, new object[] { lambda, colName });
                    }

                    try
                    {
                        session.UserDefinedTypes.Define((UdtMap)map);
                    }
                    catch(InvalidTypeException)
                    {
                        continue;
                    }
                }
            }

            return session;
        }
    }
}
