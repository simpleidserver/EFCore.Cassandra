// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.Query
{
    public class CassandraRelationalParameterBasedSqlProcessor : RelationalParameterBasedSqlProcessor
    {
        public CassandraRelationalParameterBasedSqlProcessor(
            [NotNull] RelationalParameterBasedSqlProcessorDependencies dependencies, 
            bool useRelationalNulls) : base(dependencies, useRelationalNulls)
        {
        }

        protected override SelectExpression ProcessSqlNullability(
            [NotNull] SelectExpression selectExpression,
            [NotNull] IReadOnlyDictionary<string, object> parametersValues,
            out bool canCache)
        {
            return new CassandraSqlNullabilityProcessor(Dependencies, UseRelationalNulls).Process(selectExpression, parametersValues, out canCache);
        }
    }
}
