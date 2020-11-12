// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Cassandra;
using System.Linq;
using EFCore.Cassandra;

namespace Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal
{
    public class CassandraOptionsExtension : RelationalOptionsExtension
    {
        private Action<Builder> _callback;
        private string _defaultKeyspace;

        public CassandraOptionsExtension() { }

        protected CassandraOptionsExtension(CassandraOptionsExtension copyFrom) : base(copyFrom) { }

        public override DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

        public virtual Action<Builder> ClusterBuilder => _callback;
        public virtual string DefaultKeyspace => _defaultKeyspace;

        public override void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkCassandra();
            services.Configure<CassandraOptions>((_) =>
            {
                _.ClusterBuilder = ClusterBuilder;
                _.DefaultKeyspace = DefaultKeyspace;
            });
        }

        public CassandraOptionsExtension WithCallbackClusterBuilder(Action<Builder> callback)
        {
            var clone = (CassandraOptionsExtension)Clone();
            clone._callback = callback;
            return clone;
        }

        public CassandraOptionsExtension WithDefaultKeyspace(string keyspace)
        {
            var clone = (CassandraOptionsExtension)Clone();
            clone._defaultKeyspace = keyspace;
            return clone;
        }


        protected override RelationalOptionsExtension Clone() => new CassandraOptionsExtension(this)
        {
            _callback = _callback,
            _defaultKeyspace = _defaultKeyspace
        };

        public new static CassandraOptionsExtension Extract(IDbContextOptions options)
        {
            var relationalOptionsExtensions
                = options.Extensions
                    .OfType<CassandraOptionsExtension>()
                    .ToList();
            return relationalOptionsExtensions[0];
        }

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