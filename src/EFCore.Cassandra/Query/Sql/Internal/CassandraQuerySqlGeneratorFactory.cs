// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;

namespace Microsoft.EntityFrameworkCore.Cassandra.Query.Sql.Internal
{
    public class CassandraQuerySqlGeneratorFactory : QuerySqlGeneratorFactoryBase
    {
        public CassandraQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies) : base(dependencies)
        {
        }

        public override IQuerySqlGenerator CreateDefault(SelectExpression selectExpression) => new CassandraQuerySqlGenerator(Dependencies, selectExpression);
    }
}
