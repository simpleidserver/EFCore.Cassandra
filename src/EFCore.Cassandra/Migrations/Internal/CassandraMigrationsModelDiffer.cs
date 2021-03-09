// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using EFCore.Cassandra.Extensions;
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Migrations.Internal
{
    public class CassandraMigrationsModelDiffer : MigrationsModelDiffer
    {
        private readonly CassandraOptionsExtension _cassandraOptionsExtension;

        public CassandraMigrationsModelDiffer(RelationalConnectionDependencies relationalConnectionDependencies, IRelationalTypeMappingSource typeMappingSource, IMigrationsAnnotationProvider migrationsAnnotations, IChangeDetector changeDetector, IUpdateAdapterFactory updateAdapterFactory, CommandBatchPreparerDependencies commandBatchPreparerDependencies) : base(typeMappingSource, migrationsAnnotations, changeDetector, updateAdapterFactory, commandBatchPreparerDependencies)
        {
            _cassandraOptionsExtension = CassandraOptionsExtension.Extract(relationalConnectionDependencies.ContextOptions);
        }

        #region Public methods

        public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel source, IRelationalModel target)
        {
            // source = CleanModel(source);
            // target = CleanModel(target);
            var diffContext = new DiffContext();
            var result = Diff(source, target, diffContext).ToList();
            return Sort(result, diffContext);
        }

        #endregion

        #region Protected methods

        protected override IEnumerable<MigrationOperation> Diff(
            IRelationalModel source, 
            IRelationalModel target, 
            DiffContext diffContext)
        {
            var operations = Enumerable.Empty<MigrationOperation>();
            operations = operations.Concat(new[] 
            {
                new EnsureSchemaOperation
                {
                    Name = _cassandraOptionsExtension.DefaultKeyspace
                } 
            });
            if (source != null && target != null)
            {
                var sourceMigrationsAnnotations = source.GetAnnotations();
                var targetMigrationsAnnotations = target.GetAnnotations();
                if (source.Collation != target.Collation
                     || HasDifferences(sourceMigrationsAnnotations, targetMigrationsAnnotations))
                {
                    var alterDatabaseOperation = new AlterDatabaseOperation
                    {
                        Collation = target.Collation,
                        OldDatabase = { Collation = source.Collation }
                    };

                    alterDatabaseOperation.AddAnnotations(targetMigrationsAnnotations);
                    alterDatabaseOperation.OldDatabase.AddAnnotations(sourceMigrationsAnnotations);
                    operations.Concat(new [] { alterDatabaseOperation });
                }

                operations = operations.Concat(Diff(GetSchemas(source), GetSchemas(target), diffContext))
                   .Concat(Diff(source.Tables, target.Tables, diffContext));
            }
            else
            {
                operations = operations.Concat(target != null
                       ? Add(target, diffContext)
                       : source != null
                           ? Remove(source, diffContext)
                           : Enumerable.Empty<MigrationOperation>());
            }

            return operations.Concat(GetDataOperations(source, target, diffContext));
        }

        protected override IEnumerable<MigrationOperation> Add(
            [NotNull] IRelationalModel target, 
            [NotNull] DiffContext diffContext)
        {
            var result = DiffAnnotations(null, target)
                .Concat(GetSchemas(target).SelectMany(t => Add(t, diffContext)))
                .Concat(target.Tables.SelectMany(t => Add(t, diffContext)))
                // .Concat(target.Sequences.SelectMany(t => Add(t, diffContext)))
                .Concat(target.Tables.SelectMany(t => t.ForeignKeyConstraints).SelectMany(k => Add(k, diffContext)));
            return result;
        }

        protected override IEnumerable<MigrationOperation> Add(
            [NotNull] ITable target,
            [NotNull] DiffContext diffContext)
        {
            var result = new List<MigrationOperation>();
            if (target.IsExcludedFromMigrations)
            {
                return result;
            }

            var schema = _cassandraOptionsExtension.DefaultKeyspace;
            var entityType = target.EntityTypeMappings.ElementAt(0).EntityType;
            if (!entityType.IsUserDefinedType())
            {
                var createTableOperation = new CreateTableOperation
                {
                    Schema = schema,
                    Name = target.Name,
                    Comment = target.Comment
                };
                createTableOperation.AddAnnotations(target.GetAnnotations());
                createTableOperation.Columns.AddRange(
                    GetSortedColumns(target, _cassandraOptionsExtension.DefaultKeyspace)
                    .SelectMany(p => Add(p, diffContext, inline: true)
                ).Cast<AddColumnOperation>());
                var primaryKey = target.PrimaryKey;
                if (primaryKey != null)
                {
                    createTableOperation.PrimaryKey = Add(primaryKey, diffContext).Cast<AddPrimaryKeyOperation>().Single();
                }

                createTableOperation.UniqueConstraints.AddRange(
                    target.UniqueConstraints.Where(c => !c.GetIsPrimaryKey()).SelectMany(c => Add(c, diffContext))
                        .Cast<AddUniqueConstraintOperation>());
                createTableOperation.CheckConstraints.AddRange(
                    target.CheckConstraints.SelectMany(c => Add(c, diffContext))
                        .Cast<AddCheckConstraintOperation>());
                diffContext.AddCreate(target, createTableOperation);
                result.Add(createTableOperation);
                foreach (var operation in target.Indexes.SelectMany(i => Add(i, diffContext)))
                {
                    result.Add(operation);
                }
            }
            else
            {
                var createUserDefinedOperation = new CreateUserDefinedTypeOperation
                {
                    Schema = target.Schema,
                    Name = target.Name,
                };
                createUserDefinedOperation.Columns.AddRange(
                    GetSortedColumns(target, _cassandraOptionsExtension.DefaultKeyspace)
                    .SelectMany(p => Add(p, diffContext, inline: true)
                ).Cast<AddColumnOperation>());
                result.Add(createUserDefinedOperation);
                foreach (var operation in target.Indexes.SelectMany(i => Add(i, diffContext)))
                {
                    result.Add(operation);
                }
            }

            return result;
        }

        protected override IEnumerable<MigrationOperation> Add(
            IColumn target, 
            DiffContext diffContext, 
            bool inline = false)
        {
            var table = target.Table;
            var operation = new AddColumnOperation
            {
                Schema = table.Schema,
                Table = table.Name,
                Name = target.Name
            };

            var targetMapping = target.PropertyMappings.First();
            var targetTypeMapping = targetMapping.TypeMapping;
            Initialize(
                operation, target, targetTypeMapping, target.IsNullable,
                target.GetAnnotations(), inline);
            yield return operation;
        }

        protected override IEnumerable<MigrationOperation> Remove(
            [NotNull] ITable source, 
            [NotNull] DiffContext diffContext)
        {
            if (source.IsExcludedFromMigrations)
            {
                yield break;
            }

            var schema = _cassandraOptionsExtension.DefaultKeyspace;
            var entityType = source.EntityTypeMappings.ElementAt(0).EntityType;
            MigrationOperation operation;
            if (!entityType.IsUserDefinedType())
            {
                var dropOperation = new DropTableOperation { Schema = schema, Name = source.Name };
                diffContext.AddDrop(source, dropOperation);
                operation = dropOperation;
            }
            else
            {
                operation = new DropUserDefinedTypeOperation { Schema = schema, Name = source.Name };
            }

            operation.AddAnnotations(MigrationsAnnotations.ForRemove(source));
            yield return operation;
        }

        #endregion

        #region Private methods

        private IEnumerable<MigrationOperation> DiffAnnotations(
            IRelationalModel source,
            IRelationalModel target)
        {
            var targetMigrationsAnnotations = target?.GetAnnotations().ToList();

            if (source == null)
            {
                if (targetMigrationsAnnotations?.Count > 0)
                {
                    var alterDatabaseOperation = new AlterDatabaseOperation();
                    alterDatabaseOperation.AddAnnotations(targetMigrationsAnnotations);
                    yield return alterDatabaseOperation;
                }

                yield break;
            }

            if (target == null)
            {
                var sourceMigrationsAnnotationsForRemoved = MigrationsAnnotations.ForRemove(source).ToList();
                if (sourceMigrationsAnnotationsForRemoved.Count > 0)
                {
                    var alterDatabaseOperation = new AlterDatabaseOperation();
                    alterDatabaseOperation.OldDatabase.AddAnnotations(sourceMigrationsAnnotationsForRemoved);
                    yield return alterDatabaseOperation;
                }

                yield break;
            }

            var sourceMigrationsAnnotations = source?.GetAnnotations().ToList();
            if (HasDifferences(sourceMigrationsAnnotations, targetMigrationsAnnotations))
            {
                var alterDatabaseOperation = new AlterDatabaseOperation();
                alterDatabaseOperation.AddAnnotations(targetMigrationsAnnotations);
                alterDatabaseOperation.OldDatabase.AddAnnotations(sourceMigrationsAnnotations);
                yield return alterDatabaseOperation;
            }
        }

        private void Initialize(
            ColumnOperation columnOperation,
            IColumn column,
            RelationalTypeMapping typeMapping,
            bool isNullable,
            IEnumerable<IAnnotation> migrationsAnnotations,
            bool inline = false)
        {
            var propertyInfo = column.PropertyMappings.ElementAt(0).Property.PropertyInfo;
            var clrType = propertyInfo.PropertyType;
            string storeType = column.StoreType;
            var ct = column.PropertyMappings.ElementAt(0).Property.GetConfiguredColumnType();
            if (!string.IsNullOrWhiteSpace(ct))
            {
                storeType = ct;
            }
            else if (clrType.IsList() && !CassandraTypeMappingSource.CLR_TYPE_MAPPINGS.ContainsKey(clrType))
            {
                Type genericType;
                if (clrType.IsGenericType)
                {
                    genericType = clrType.GenericTypeArguments.First();
                }
                else
                {
                    genericType = clrType.GetElementType();
                }

                if (!CassandraTypeMappingSource.CLR_TYPE_MAPPINGS.ContainsKey(genericType))
                {
                    var et = column.Table.Model.Model.FindEntityType(genericType);
                    storeType = storeType.Replace(genericType.Name, et.GetTableName());
                }
            }

            if (column.DefaultValue == DBNull.Value)
            {
                throw new InvalidOperationException(
                    RelationalStrings.DefaultValueUnspecified(
                        column.Name,
                        (column.Table.Name, column.Table.Schema).FormatTable()));
            }

            if (column.DefaultValueSql?.Length == 0)
            {
                throw new InvalidOperationException(
                    RelationalStrings.DefaultValueSqlUnspecified(
                        column.Name,
                        (column.Table.Name, column.Table.Schema).FormatTable()));
            }

            if (column.ComputedColumnSql?.Length == 0)
            {
                throw new InvalidOperationException(
                    RelationalStrings.ComputedColumnSqlUnspecified(
                        column.Name,
                        (column.Table.Name, column.Table.Schema).FormatTable()));
            }

            var property = column.PropertyMappings.First().Property;
            var valueConverter = GetValueConverter(property, typeMapping);
            columnOperation.ClrType
                = (valueConverter?.ProviderClrType
                    ?? typeMapping.ClrType).UnwrapNullableType();

            columnOperation.ColumnType = storeType;
            columnOperation.MaxLength = column.MaxLength;
            columnOperation.Precision = column.Precision;
            columnOperation.Scale = column.Scale;
            columnOperation.IsUnicode = column.IsUnicode;
            columnOperation.IsFixedLength = column.IsFixedLength;
            columnOperation.IsRowVersion = column.IsRowVersion;
            columnOperation.IsNullable = isNullable;
            columnOperation.DefaultValue = column.DefaultValue
                ?? (inline || isNullable
                    ? null
                    : GetDefaultValue(columnOperation.ClrType));
            columnOperation.DefaultValueSql = column.DefaultValueSql;
            columnOperation.ComputedColumnSql = column.ComputedColumnSql;
            columnOperation.IsStored = column.IsStored;
            columnOperation.Comment = column.Comment;
            columnOperation.Collation = column.Collation;
            columnOperation.AddAnnotations(migrationsAnnotations);
        }
        private ValueConverter GetValueConverter(IProperty property, RelationalTypeMapping typeMapping = null)
            => property.GetValueConverter() ?? (property.FindRelationalTypeMapping() ?? typeMapping)?.Converter;

        #endregion

        #region Static methods

        public static bool CheckProperty(Assembly[] assms, IProperty property)
        {
            var type = assms.Select(a => a.GetType(property.DeclaringType.Name)).FirstOrDefault(t => t != null);
            if (type == null)
            {
                return false;
            }

            if (!type.GetProperties().Any(p => p.Name == property.Name))
            {
                return false;
            }

            return true;
        }

        private static IEntityType GetMainType(ITable table)
            => table.EntityTypeMappings.FirstOrDefault(t => t.IsSharedTablePrincipal).EntityType;

        private static IRelationalModel CleanModel(IRelationalModel model)
        {
            if (model == null)
            {
                return null;
            }

            var result = new Model();
            foreach(var annotation in model.GetAnnotations())
            {
                result.AddAnnotation(annotation.Name, annotation.Value);
            }

            var assms = AppDomain.CurrentDomain.GetAssemblies();
            var entityTypes = new List<EntityType>(); //  model.GetEntityTypes().Cast<EntityType>();
            foreach (var entityType in entityTypes)
            {
                var newEntityType = entityType.ClrType != null ? result.AddEntityType(entityType.ClrType, ConfigurationSource.Explicit) : result.AddEntityType(entityType.Name, ConfigurationSource.Explicit);
                foreach (var property in entityType.GetProperties())
                {
                    if (!CheckProperty(assms, property))
                    {
                        continue;
                    }

                    var prop = newEntityType.AddProperty(property.Name, property.ClrType, ConfigurationSource.Explicit, ConfigurationSource.Explicit);
                    prop.SetColumnName(property.GetColumnName());
                    prop.SetColumnType(property.GetColumnType());
                }

                foreach (var annotation in entityType.GetAnnotations())
                {
                    newEntityType.AddAnnotation(annotation.Name, annotation.Value);
                }

                foreach (var kvp in entityType.GetKeys())
                {
                    var properties = kvp.Properties.Select(_ => newEntityType.FindProperty(_.Name)).ToList();
                    foreach(var prop in properties)
                    {
                        prop.IsNullable = false;
                    }

                    var key = newEntityType.AddKey(properties, ConfigurationSource.Explicit);
                    key.SetName(kvp.GetName());
                }

                var primaryKey = entityType.FindPrimaryKey();
                if (primaryKey != null)
                {
                    var properties = primaryKey.Properties.Select(_ => newEntityType.FindProperty(_.Name)).ToList();
                    newEntityType.SetPrimaryKey(properties, ConfigurationSource.Convention);
                }
            }

            return new RelationalModel(result);
        }

        private static IEnumerable<IColumn> GetSortedColumns(ITable table, string schema)
        {
            var columns = table.Columns.ToHashSet();
            var sortedColumns = new List<IColumn>(columns.Count);
            foreach (var property in GetSortedProperties(GetMainType(table).GetRootType(), table, schema))
            {
                var column = table.FindColumn(property);
                if (columns.Remove(column))
                {
                    sortedColumns.Add(column);
                }
            }

            return sortedColumns;
        }

        private static IEnumerable<IProperty> GetSortedProperties(IEntityType entityType, ITable table, string schema)
        {
            var shadowProperties = new List<IProperty>();
            var shadowPrimaryKeyProperties = new List<IProperty>();
            var primaryKeyPropertyGroups = new Dictionary<PropertyInfo, IProperty>();
            var groups = new Dictionary<PropertyInfo, List<IProperty>>();
            var unorderedGroups = new Dictionary<PropertyInfo, SortedDictionary<int, IProperty>>();
            var types = new Dictionary<Type, SortedDictionary<int, PropertyInfo>>();
            var model = entityType.Model;
            var declaredProperties = entityType.GetDeclaredProperties().ToList();
            declaredProperties.AddRange(entityType.ClrType.GetProperties().Where(p =>
            {
                var et = model.FindEntityType(p.PropertyType);
                if (et == null || !et.IsUserDefinedType())
                {
                    return false;
                }

                return true;
            }).Select(p => new Property(p.Name, p.PropertyType, null, null, entityType as EntityType, ConfigurationSource.Convention, null)).ToList());
            foreach (var property in declaredProperties)
            {
                var clrProperty = property.PropertyInfo;
                if (clrProperty == null)
                {
                    if (property.IsPrimaryKey())
                    {
                        shadowPrimaryKeyProperties.Add(property);

                        continue;
                    }

                    var foreignKey = property.GetContainingForeignKeys()
                        .FirstOrDefault(fk => fk.DependentToPrincipal?.PropertyInfo != null);
                    if (foreignKey == null)
                    {
                        shadowProperties.Add(property);

                        continue;
                    }

                    clrProperty = foreignKey.DependentToPrincipal.PropertyInfo;
                    var groupIndex = foreignKey.Properties.IndexOf(property);

                    unorderedGroups.GetOrAddNew(clrProperty).Add(groupIndex, property);
                }
                else
                {
                    if (property.IsPrimaryKey())
                    {
                        primaryKeyPropertyGroups.Add(clrProperty, property);
                    }

                    groups.Add(
                        clrProperty, new List<IProperty> { property });
                }

                var clrType = clrProperty.DeclaringType;
                var index = clrType.GetTypeInfo().DeclaredProperties
                    .IndexOf(clrProperty, PropertyInfoEqualityComparer.Instance);

                types.GetOrAddNew(clrType)[index] = clrProperty;
            }

            foreach (var group in unorderedGroups)
            {
                groups.Add(group.Key, group.Value.Values.ToList());
            }

            foreach (var definingForeignKey in entityType.GetDeclaredReferencingForeignKeys()
                .Where(
                    fk => fk.DeclaringEntityType.GetRootType() != entityType.GetRootType()
                        && fk.DeclaringEntityType.GetTableName() == entityType.GetTableName()
                        && fk.DeclaringEntityType.GetSchema() == schema
                        && fk
                        == fk.DeclaringEntityType
                            .FindForeignKey(
                                fk.DeclaringEntityType.FindPrimaryKey().Properties,
                                entityType.FindPrimaryKey(),
                                entityType)))
            {
                var clrProperty = definingForeignKey.PrincipalToDependent?.PropertyInfo;
                var properties = GetSortedProperties(definingForeignKey.DeclaringEntityType, table, schema).ToList();
                if (clrProperty == null)
                {
                    shadowProperties.AddRange(properties);

                    continue;
                }

                groups.Add(clrProperty, properties);

                var clrType = clrProperty.DeclaringType;
                var index = clrType.GetTypeInfo().DeclaredProperties
                    .IndexOf(clrProperty, PropertyInfoEqualityComparer.Instance);

                types.GetOrAddNew(clrType)[index] = clrProperty;
            }

            var graph = new Multigraph<Type, object>();
            graph.AddVertices(types.Keys);

            foreach (var left in types.Keys)
            {
                var found = false;
                foreach (var baseType in left.GetBaseTypes())
                {
                    foreach (var right in types.Keys)
                    {
                        if (right == baseType)
                        {
                            graph.AddEdge(right, left, null);
                            found = true;

                            break;
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }
            }

            var sortedPropertyInfos = graph.TopologicalSort().SelectMany(e => types[e].Values).ToList();

            return sortedPropertyInfos
                .Select(pi => primaryKeyPropertyGroups.ContainsKey(pi) ? primaryKeyPropertyGroups[pi] : null)
                .Where(e => e != null)
                .Concat(shadowPrimaryKeyProperties)
                .Concat(sortedPropertyInfos.Where(pi => !primaryKeyPropertyGroups.ContainsKey(pi)).SelectMany(p => groups[p]))
                .Concat(shadowProperties)
                .Concat(entityType.GetDirectlyDerivedTypes().SelectMany(_ => GetSortedProperties(_, table, schema)));
        }

        #endregion

        private class PropertyInfoEqualityComparer : IEqualityComparer<PropertyInfo>
        {
            private PropertyInfoEqualityComparer()
            {
            }

            public static readonly PropertyInfoEqualityComparer Instance = new PropertyInfoEqualityComparer();

            public bool Equals(PropertyInfo x, PropertyInfo y)
                => x.IsSameAs(y);

            public int GetHashCode(PropertyInfo obj)
                => throw new NotImplementedException();
        }
    }
}
