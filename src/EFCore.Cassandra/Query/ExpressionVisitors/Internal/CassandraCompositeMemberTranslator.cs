// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Microsoft.EntityFrameworkCore.Cassandra.Query.ExpressionVisitors.Internal
{
    public class CassandraCompositeMemberTranslator : RelationalCompositeMemberTranslator
    {
        public CassandraCompositeMemberTranslator(RelationalCompositeMemberTranslatorDependencies dependencies) : base(dependencies)
        {
        }
    }
}
