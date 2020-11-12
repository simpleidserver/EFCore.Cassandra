// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;

namespace EFCore.Cassandra.Query.Sql.Internal
{
    public class CassandraSqlGeneratorFactory : IQuerySqlGeneratorFactory
    {
        private readonly CassandraOptions _opts;
        private readonly QuerySqlGeneratorDependencies _dependencies;

        public CassandraSqlGeneratorFactory(IOptions<CassandraOptions> opts, QuerySqlGeneratorDependencies dependencies)
        {
            _opts = opts.Value;
            _dependencies = dependencies;
        }

        public virtual QuerySqlGenerator Create() => new CassandraQuerySqlGenerator(_opts, _dependencies);
    }
}
