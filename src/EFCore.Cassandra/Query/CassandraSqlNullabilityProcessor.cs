// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.Query
{
    public class CassandraSqlNullabilityProcessor : SqlNullabilityProcessor
    {
        public CassandraSqlNullabilityProcessor(
            [NotNull] RelationalParameterBasedSqlProcessorDependencies dependencies, 
            bool useRelationalNulls) : base(dependencies, useRelationalNulls)
        {
        }

        protected override SqlExpression VisitCustomSqlExpression(SqlExpression sqlExpression, bool allowOptimizedExpansion, out bool nullable)
        {
            nullable = false;
            return sqlExpression;
        }
    }
}
