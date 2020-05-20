// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal
{
    public class CassandraOptionsExtension : RelationalOptionsExtension
    {
        public CassandraOptionsExtension() { }

        protected CassandraOptionsExtension(CassandraOptionsExtension copyFrom) : base(copyFrom) { }

        public override DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

        public override void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkCassandra();
        }

        protected override RelationalOptionsExtension Clone() => new CassandraOptionsExtension(this);

        private sealed class ExtensionInfo : RelationalExtensionInfo
        {
            private string _logFragment;

            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            private new CassandraOptionsExtension Extension
                => (CassandraOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => true;

            public override string LogFragment
            {
                get
                {
                    if (_logFragment == null)
                    {
                        var builder = new StringBuilder();
                        builder.Append(base.LogFragment);
                        _logFragment = builder.ToString();
                    }

                    return _logFragment;
                }
            }

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
                => debugInfo["Cassandra"] = "1";
        }
    }
}