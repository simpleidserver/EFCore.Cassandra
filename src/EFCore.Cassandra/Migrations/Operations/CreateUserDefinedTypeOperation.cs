// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.EntityFrameworkCore.Migrations.Operations
{
    [DebuggerDisplay("CREATE TYPE {Name}")]
    public class CreateUserDefinedTypeOperation : MigrationOperation
    {
        public virtual string Name { get; set; }
        public virtual string Schema { get; set; }
        public virtual List<AddColumnOperation> Columns { get; } = new List<AddColumnOperation>();
    }
}
