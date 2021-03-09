// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.Query.Internal
{
    public class CassandraRelationalParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
    {
        private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public CassandraRelationalParameterBasedSqlProcessorFactory(
            [NotNull] RelationalParameterBasedSqlProcessorDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
            => new CassandraRelationalParameterBasedSqlProcessor(_dependencies, useRelationalNulls);
    }
}
