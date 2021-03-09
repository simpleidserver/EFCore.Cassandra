// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Cassandra.Query.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore.Cassandra.Query.Sql.Internal
{
    public class CassandraSqlTranslatingExpressionVisitorFactory : IRelationalSqlTranslatingExpressionVisitorFactory
    {
        private readonly RelationalSqlTranslatingExpressionVisitorDependencies _dependencies;

        public CassandraSqlTranslatingExpressionVisitorFactory(RelationalSqlTranslatingExpressionVisitorDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public RelationalSqlTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext, QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
        {
            return new CassandraProjectionBindingExpressionVisitor(_dependencies,
                queryCompilationContext,
                queryableMethodTranslatingExpressionVisitor);
        }
    }
}
