// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace Microsoft.EntityFrameworkCore.Migrations.Operations.Builders
{
    public class CassandraUserDefinedTypeColumnsBuilder
    {
        private readonly CreateUserDefinedTypeOperation _createUserDefinedTypeOperation;

        public CassandraUserDefinedTypeColumnsBuilder(CreateUserDefinedTypeOperation createUserDefinedTypeOperation)
        {
            _createUserDefinedTypeOperation = createUserDefinedTypeOperation;
        }

        public virtual OperationBuilder<AddColumnOperation> Column<T>(bool nullable = false)
        {
            var operation = new AddColumnOperation
            {
                Schema = _createUserDefinedTypeOperation.Schema,
                Table = _createUserDefinedTypeOperation.Name,
                ClrType = typeof(T),
                IsNullable = nullable
            };
            _createUserDefinedTypeOperation.Columns.Add(operation);
            return new OperationBuilder<AddColumnOperation>(operation);
        }
    }
}
