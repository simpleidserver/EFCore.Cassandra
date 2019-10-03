// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal
{
    public class CassandraOptionsExtension : RelationalOptionsExtension, IDbContextOptionsExtensionWithDebugInfo
    {
        public CassandraOptionsExtension()
        {
        }

        protected CassandraOptionsExtension(CassandraOptionsExtension copyFrom) : base(copyFrom) { }

        public override bool ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkCassandra();
            return true;
        }

        public void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Cassandra"] = "1";
        }

        protected override RelationalOptionsExtension Clone() => new CassandraOptionsExtension(this);
    }
}