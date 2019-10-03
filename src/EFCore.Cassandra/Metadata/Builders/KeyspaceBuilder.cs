// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;

namespace Microsoft.EntityFrameworkCore.Cassandra.Metadata.Builders
{
    public class KeyspaceBuilder : IInfrastructure<IMutableModel>, IInfrastructure<InternalEntityTypeBuilder>
    {
        public KeyspaceBuilder(InternalEntityTypeBuilder builder)
        {
            if (builder.Metadata.IsQueryType)
            {
                throw new InvalidOperationException();
            }

            Builder = builder;
        }

        private InternalEntityTypeBuilder Builder { get; }

        InternalEntityTypeBuilder IInfrastructure<InternalEntityTypeBuilder>.Instance => Builder;
        public virtual IMutableEntityType Metadata => Builder.Metadata;
        IMutableModel IInfrastructure<IMutableModel>.Instance => Builder.ModelBuilder.Metadata;
    }
}
