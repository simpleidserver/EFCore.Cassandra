// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Migrations
{
    public static class CassandraMigrationBuilderExtensions
    {
        public static CassandraUserDefinedTypeBuilder<TColumns> CreateUserDefinedType<TColumns>(this MigrationBuilder migrationBuilder, string name, Func<CassandraUserDefinedTypeColumnsBuilder, TColumns> columns, string schema = null)
        {
            var createUserDefinedOperation = new CreateUserDefinedTypeOperation
            {
                Name = name,
                Schema = schema
            };
            var columnsBuilder = new CassandraUserDefinedTypeColumnsBuilder(createUserDefinedOperation);
            var columnsObject = columns(columnsBuilder);
            var columnMap = new Dictionary<PropertyInfo, AddColumnOperation>();
            foreach (var property in typeof(TColumns).GetTypeInfo().DeclaredProperties)
            {
                var addColumnOperation = ((IInfrastructure<AddColumnOperation>)property.GetMethod.Invoke(columnsObject, null)).Instance;
                if (addColumnOperation.Name == null)
                {
                    addColumnOperation.Name = property.Name;
                }

                columnMap.Add(property, addColumnOperation);
            }

            var builder = new CassandraUserDefinedTypeBuilder<TColumns>(createUserDefinedOperation, columnMap);
            migrationBuilder.Operations.Add(createUserDefinedOperation);
            return builder;
        }

        public static void DropUserDefinedType(this MigrationBuilder migrationBuilder, string name, string schema = null)
        {
            var dropUserDefinedOperation = new DropUserDefinedTypeOperation
            {
                Name = name,
                Schema = schema
            };

            migrationBuilder.Operations.Add(dropUserDefinedOperation);
        }
    }
}
