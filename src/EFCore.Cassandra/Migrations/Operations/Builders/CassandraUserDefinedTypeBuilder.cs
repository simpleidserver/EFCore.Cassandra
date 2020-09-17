// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Migrations.Operations.Builders
{
    public class CassandraUserDefinedTypeBuilder<TColumns> : OperationBuilder<CreateUserDefinedTypeOperation>
    {
        private readonly IReadOnlyDictionary<PropertyInfo, AddColumnOperation> _columnMap;

        public CassandraUserDefinedTypeBuilder(CreateUserDefinedTypeOperation operation,
            IReadOnlyDictionary<PropertyInfo, AddColumnOperation> columnMap) : base(operation)
        {
            _columnMap = columnMap;
        }
    }
}
