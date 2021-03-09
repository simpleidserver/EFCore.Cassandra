// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;

namespace Microsoft.EntityFrameworkCore.Migrations.Design
{
    public class CSharpCassandraSnapshotGenerator : CSharpSnapshotGenerator
    {
        private ICSharpHelper Code => Dependencies.CSharpHelper;

        public CSharpCassandraSnapshotGenerator(CSharpSnapshotGeneratorDependencies dependencies) : base(dependencies)
        {
        }

        protected override void GenerateEntityType(
            string builderName,
            IEntityType entityType,
            IndentedStringBuilder stringBuilder)
        {
            var isUserType = entityType.IsUserDefinedType();
            var ownership = entityType.FindOwnership();
            var ownerNavigation = ownership?.PrincipalToDependent.Name;

            stringBuilder
                .Append(builderName)
                .Append(
                    ownerNavigation != null
                        ? ownership.IsUnique ? ".OwnsOne(" : ".OwnsMany("
                        : ".Entity(")
                .Append(Code.Literal(entityType.Name));

            if (ownerNavigation != null)
            {
                stringBuilder
                    .Append(", ")
                    .Append(ownerNavigation);
            }

            if (builderName.StartsWith("b", StringComparison.Ordinal))
            {
                var counter = 1;
                if (builderName.Length > 1
                    && int.TryParse(builderName.Substring(1), out counter))
                {
                    counter++;
                }

                builderName = "b" + (counter == 0 ? "" : counter.ToString());
            }
            else
            {
                builderName = "b";
            }

            stringBuilder
                .Append(", ")
                .Append(builderName)
                .AppendLine(" =>");
            using (stringBuilder.Indent())
            {
                stringBuilder.Append("{");

                var properties = entityType.GetDeclaredProperties();
                var assms = AppDomain.CurrentDomain.GetAssemblies();
                using (stringBuilder.Indent())
                {
                    GenerateBaseType(builderName, entityType.BaseType, stringBuilder);
                    GenerateProperties(builderName, entityType.GetDeclaredProperties(), stringBuilder);
                    GenerateKeys(builderName, entityType.GetDeclaredKeys(), entityType.FindDeclaredPrimaryKey(), stringBuilder);
                    GenerateEntityTypeAnnotations(builderName, entityType, stringBuilder);
                    GenerateCheckConstraints(builderName, entityType, stringBuilder);
                    if (ownerNavigation != null)
                    {
                        GenerateRelationships(builderName, entityType, stringBuilder);
                    }

                    GenerateData(builderName, entityType.GetProperties(), entityType.GetSeedData(providerValues: true), stringBuilder);
                }

                stringBuilder
                    .AppendLine("});");
            }
        }

        protected override void GenerateEntityTypeRelationships(string builderName, IEntityType entityType, IndentedStringBuilder stringBuilder)
        {

        }
    }
}
