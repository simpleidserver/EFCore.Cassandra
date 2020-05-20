// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Update;

namespace Microsoft.EntityFrameworkCore.Cassandra.Update.Internal
{
    public class CassandraModificationCommandBatch : AffectedCountModificationCommandBatch
    {
        public CassandraModificationCommandBatch(ModificationCommandBatchFactoryDependencies dependencies) : base(dependencies) { }

        protected override bool CanAddCommand(ModificationCommand modificationCommand) => ModificationCommands.Count == 0;

        protected override bool IsCommandTextValid() => true;
    }
}
