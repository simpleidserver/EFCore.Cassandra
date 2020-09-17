using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Migrations.Internal
{
    public class CassandraMigrationsModelDiffer : MigrationsModelDiffer
    {
        public CassandraMigrationsModelDiffer(IRelationalTypeMappingSource typeMappingSource, IMigrationsAnnotationProvider migrationsAnnotations, IChangeDetector changeDetector, IUpdateAdapterFactory updateAdapterFactory, CommandBatchPreparerDependencies commandBatchPreparerDependencies) : base(typeMappingSource, migrationsAnnotations, changeDetector, updateAdapterFactory, commandBatchPreparerDependencies)
        {
        }

        protected override IEnumerable<MigrationOperation> Remove(TableMapping source, DiffContext diffContext)
        {
            var type = source.GetRootType();
            MigrationOperation operation;
            if (!type.IsUserDefinedType())
            {
                var dropOperation = new DropTableOperation { Schema = source.Schema, Name = source.Name };
                diffContext.AddDrop(source, dropOperation);
                operation = dropOperation;
            }
            else
            {
                operation = new DropUserDefinedTypeOperation { Schema = source.Schema, Name = source.Name };
            }

            operation.AddAnnotations(MigrationsAnnotations.ForRemove(source.EntityTypes[0]));
            yield return operation;
        }


        protected override IEnumerable<MigrationOperation> Add(TableMapping target, DiffContext diffContext)
        {
            var result = new List<MigrationOperation>();
            var type = target.GetRootType();
            if (!type.IsUserDefinedType())
            {
                var entityType = target.EntityTypes[0];
                var createTableOperation = new CreateTableOperation
                {
                    Schema = target.Schema,
                    Name = target.Name,
                    Comment = target.GetComment()
                };
                createTableOperation.AddAnnotations(MigrationsAnnotations.For(entityType));
                createTableOperation.Columns.AddRange(GetSortedProperties(target).SelectMany(p => Add(p, diffContext, inline: true)).Cast<AddColumnOperation>());
                var primaryKey = target.EntityTypes[0].FindPrimaryKey();
                if (primaryKey != null)
                {
                    createTableOperation.PrimaryKey = Add(primaryKey, diffContext).Cast<AddPrimaryKeyOperation>().Single();
                }

                createTableOperation.UniqueConstraints.AddRange(
                    target.GetKeys().Where(k => !k.IsPrimaryKey()).SelectMany(k => Add(k, diffContext))
                        .Cast<AddUniqueConstraintOperation>());
                createTableOperation.CheckConstraints.AddRange(
                    target.GetCheckConstraints().SelectMany(c => Add(c, diffContext))
                        .Cast<CreateCheckConstraintOperation>());

                foreach (var targetEntityType in target.EntityTypes)
                {
                    diffContext.AddCreate(targetEntityType, createTableOperation);
                }

                result.Add(createTableOperation);
                foreach (var operation in target.GetIndexes().SelectMany(i => Add(i, diffContext)))
                {
                    result.Add(operation);
                }

                return result.ToArray();
            }

            var createUserDefinedOperation = new CreateUserDefinedTypeOperation
            {
                Schema = target.Schema,
                Name = target.Name,
            };
            createUserDefinedOperation.Columns.AddRange(type.GetProperties().SelectMany(p => Add(p, diffContext, inline: true)).Cast<AddColumnOperation>());
            result.Add(createUserDefinedOperation);
            foreach (var operation in target.GetIndexes().SelectMany(i => Add(i, diffContext)))
            {
                result.Add(operation);
            }

            return result.ToArray();
        }

        protected override IEnumerable<MigrationOperation> Add(IProperty target, DiffContext diffContext, bool inline = false)
        {
            var targetEntityType = target.DeclaringEntityType.GetRootType();
            var et = targetEntityType.Model.FindEntityType(target.ClrType);
            Type clrType;
            IEntityType userDefinedType = null;
            if (et == null || !et.IsUserDefinedType())
            {
                var typeMapping = TypeMappingSource.GetMapping(target);
                clrType = typeMapping.Converter?.ProviderClrType ?? (typeMapping.ClrType).UnwrapNullableType();
            }
            else
            {
                clrType = target.ClrType;
                userDefinedType = et;
            }

            var operation = new AddColumnOperation
            {
                Schema = targetEntityType.GetSchema(),
                Table = targetEntityType.GetTableName(),
                Name = target.GetColumnName()
            };


            Initialize(
                operation, target, clrType, target.IsColumnNullable(),
                MigrationsAnnotations.For(target), inline, userDefinedType);

            yield return operation;
        }

        private void Initialize(
            ColumnOperation columnOperation,
            IProperty property,
            Type clrType,
            bool isNullable,
            IEnumerable<IAnnotation> migrationsAnnotations,
            bool inline = false,
            IEntityType userDefinedType = null)
        {
            columnOperation.ClrType = clrType;
            columnOperation.ColumnType = userDefinedType != null ? userDefinedType.GetTableName() : property.GetConfiguredColumnType();
            columnOperation.MaxLength = property.GetMaxLength();
            columnOperation.IsUnicode = property.IsUnicode();
            columnOperation.IsFixedLength = property.IsFixedLength();
            columnOperation.IsRowVersion = property.ClrType == typeof(byte[])
                && property.IsConcurrencyToken
                && property.ValueGenerated == ValueGenerated.OnAddOrUpdate;
            columnOperation.IsNullable = isNullable;

            var defaultValue = userDefinedType != null ? null : GetDefaultValue(property);
            columnOperation.DefaultValue = (defaultValue == DBNull.Value ? null : defaultValue)
                ?? (inline || isNullable
                    ? null
                    : userDefinedType != null ? null : GetDefaultValue(columnOperation.ClrType));

            columnOperation.DefaultValueSql = property.GetDefaultValueSql();
            columnOperation.ComputedColumnSql = property.GetComputedColumnSql();
            columnOperation.Comment = property.GetComment();
            columnOperation.AddAnnotations(migrationsAnnotations);
        }

        private object GetDefaultValue(IProperty property)
        {
            var value = property.GetDefaultValue();
            var converter = GetValueConverter(property);
            return converter != null
                ? converter.ConvertToProvider(value)
                : value;
        }

        private ValueConverter GetValueConverter(IProperty property) => TypeMappingSource.GetMapping(property).Converter;

        private static IEnumerable<IProperty> GetSortedProperties(TableMapping target)
            => GetSortedProperties(target.GetRootType()).Distinct((x, y) => x.GetColumnName() == y.GetColumnName());

        private static IEnumerable<IProperty> GetSortedProperties(IEntityType entityType)
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
                        && fk.DeclaringEntityType.GetSchema() == entityType.GetSchema()
                        && fk
                        == fk.DeclaringEntityType
                            .FindForeignKey(
                                fk.DeclaringEntityType.FindPrimaryKey().Properties,
                                entityType.FindPrimaryKey(),
                                entityType)))
            {
                var clrProperty = definingForeignKey.PrincipalToDependent?.PropertyInfo;
                var properties = GetSortedProperties(definingForeignKey.DeclaringEntityType).ToList();
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
                .Concat(entityType.GetDirectlyDerivedTypes().SelectMany(GetSortedProperties));
        }

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
