// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Diagnostics;

namespace Microsoft.EntityFrameworkCore.Migrations.Operations
{
    [DebuggerDisplay("DROP TYPE {Name}")]
    public class DropUserDefinedTypeOperation : MigrationOperation
    {
        public virtual string Name { get; set; }
        public virtual string Schema { get; set; }
    }
}
