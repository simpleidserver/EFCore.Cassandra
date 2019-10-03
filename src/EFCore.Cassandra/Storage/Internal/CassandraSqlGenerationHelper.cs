// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Cassandra.Storage.Internal
{
    public class CassandraSqlGenerationHelper : RelationalSqlGenerationHelper
    {
        public CassandraSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies) : base(dependencies)
        {
        }

        public override string GenerateParameterName(string name) => ":" + name;
        public override void GenerateParameterName(StringBuilder builder, string name) => builder.Append(":").Append(name);
    }
}
