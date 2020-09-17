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
