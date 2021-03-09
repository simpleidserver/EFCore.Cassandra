using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure
{
    public class CassandraConventionSetBuilder : RelationalConventionSetBuilder
    {
        public CassandraConventionSetBuilder(
            [NotNull] ProviderConventionSetBuilderDependencies dependencies,
            [NotNull] RelationalConventionSetBuilderDependencies relationalDependencies) : base(dependencies, relationalDependencies)
        {
        }

        public override ConventionSet CreateConventionSet()
        {
            return base.CreateConventionSet();
        }
    }
}
