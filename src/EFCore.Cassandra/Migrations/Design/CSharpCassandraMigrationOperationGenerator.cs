// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.EntityFrameworkCore.Migrations.Design
{
    public class CSharpCassandraMigrationOperationGenerator : CSharpMigrationOperationGenerator
    {
        public CSharpCassandraMigrationOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies) : base(dependencies)
        {
        }

        private ICSharpHelper Code => Dependencies.CSharpHelper;

        public override void Generate(string builderName, IReadOnlyList<MigrationOperation> operations, IndentedStringBuilder builder)
        {
            var first = true;
            foreach (var operation in operations)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder
                        .AppendLine()
                        .AppendLine();
                }

                builder.Append(builderName);
                Generate((dynamic)operation, builder);
                builder.Append(";");
            }
        }

        protected override void Generate(CreateTableOperation operation, IndentedStringBuilder builder)
        {
            builder.AppendLine(".CreateTable(");

            using (builder.Indent())
            {
                builder
                    .Append("name: ")
                    .Append(Code.Literal(operation.Name));

                if (operation.Schema != null)
                {
                    builder
                        .AppendLine(",")
                        .Append("schema: ")
                        .Append(Code.Literal(operation.Schema));
                }

                builder
                    .AppendLine(",")
                    .AppendLine("columns: table => new")
                    .AppendLine("{");

                var map = new Dictionary<string, string>();
                using (builder.Indent())
                {
                    var scope = new List<string>();
                    for (var i = 0; i < operation.Columns.Count; i++)
                    {
                        var column = operation.Columns[i];
                        var propertyName = Code.Identifier(column.Name, scope);
                        map.Add(column.Name, propertyName);

                        builder
                            .Append(propertyName)
                            .Append(" = table.Column<")
                            .Append(Code.Reference(column.ClrType))
                            .Append(">(");

                        if (propertyName != column.Name)
                        {
                            builder
                                .Append("name: ")
                                .Append(Code.Literal(column.Name))
                                .Append(", ");
                        }

                        if (column.ColumnType != null)
                        {
                            builder
                                .Append("type: ")
                                .Append(Code.Literal(column.ColumnType))
                                .Append(", ");
                        }

                        if (column.IsUnicode == false)
                        {
                            builder.Append("unicode: false, ");
                        }

                        if (column.IsFixedLength == true)
                        {
                            builder.Append("fixedLength: true, ");
                        }

                        if (column.MaxLength.HasValue)
                        {
                            builder
                                .Append("maxLength: ")
                                .Append(Code.Literal(column.MaxLength.Value))
                                .Append(", ");
                        }

                        if (column.IsRowVersion)
                        {
                            builder.Append("rowVersion: true, ");
                        }

                        builder.Append("nullable: ")
                            .Append(Code.Literal(column.IsNullable));

                        if (column.DefaultValueSql != null)
                        {
                            builder
                                .Append(", defaultValueSql: ")
                                .Append(Code.Literal(column.DefaultValueSql));
                        }
                        else if (column.ComputedColumnSql != null)
                        {
                            builder
                                .Append(", computedColumnSql: ")
                                .Append(Code.Literal(column.ComputedColumnSql));
                        }
                        else if (column.DefaultValue != null)
                        {
                            builder
                                .Append(", defaultValue: ")
                                .Append(Code.UnknownLiteral(column.DefaultValue));
                        }

                        if (column.Comment != null)
                        {
                            builder
                                .Append(", comment: ")
                                .Append(Code.Literal(column.Comment));
                        }

                        builder.Append(")");

                        using (builder.Indent())
                        {
                            Annotations(column.GetAnnotations(), builder);
                        }

                        if (i != operation.Columns.Count - 1)
                        {
                            builder.Append(",");
                        }

                        builder.AppendLine();
                    }
                }

                builder
                    .AppendLine("},")
                    .AppendLine("constraints: table =>")
                    .AppendLine("{");

                using (builder.Indent())
                {
                    if (operation.PrimaryKey != null)
                    {
                        builder
                            .Append("table.PrimaryKey(")
                            .Append(Code.Literal(operation.PrimaryKey.Name))
                            .Append(", ")
                            .Append(Code.Lambda(operation.PrimaryKey.Columns.Select(c => map[c]).ToList()))
                            .Append(")");

                        using (builder.Indent())
                        {
                            Annotations(operation.PrimaryKey.GetAnnotations(), builder);
                        }

                        builder.AppendLine(";");
                    }

                    foreach (var uniqueConstraint in operation.UniqueConstraints)
                    {
                        builder
                            .Append("table.UniqueConstraint(")
                            .Append(Code.Literal(uniqueConstraint.Name))
                            .Append(", ")
                            .Append(Code.Lambda(uniqueConstraint.Columns.Select(c => map[c]).ToList()))
                            .Append(")");

                        using (builder.Indent())
                        {
                            Annotations(uniqueConstraint.GetAnnotations(), builder);
                        }

                        builder.AppendLine(";");
                    }

                    foreach (var checkConstraints in operation.CheckConstraints)
                    {
                        builder
                            .Append("table.CheckConstraint(")
                            .Append(Code.Literal(checkConstraints.Name))
                            .Append(", ")
                            .Append(Code.Literal(checkConstraints.Sql))
                            .Append(")");

                        using (builder.Indent())
                        {
                            Annotations(checkConstraints.GetAnnotations(), builder);
                        }

                        builder.AppendLine(";");
                    }

                    foreach (var foreignKey in operation.ForeignKeys)
                    {
                        builder.AppendLine("table.ForeignKey(");

                        using (builder.Indent())
                        {
                            builder
                                .Append("name: ")
                                .Append(Code.Literal(foreignKey.Name))
                                .AppendLine(",")
                                .Append(
                                    foreignKey.Columns.Length == 1
                                        ? "column: "
                                        : "columns: ")
                                .Append(Code.Lambda(foreignKey.Columns.Select(c => map[c]).ToList()));

                            if (foreignKey.PrincipalSchema != null)
                            {
                                builder
                                    .AppendLine(",")
                                    .Append("principalSchema: ")
                                    .Append(Code.Literal(foreignKey.PrincipalSchema));
                            }

                            builder
                                .AppendLine(",")
                                .Append("principalTable: ")
                                .Append(Code.Literal(foreignKey.PrincipalTable))
                                .AppendLine(",");

                            if (foreignKey.PrincipalColumns.Length == 1)
                            {
                                builder
                                    .Append("principalColumn: ")
                                    .Append(Code.Literal(foreignKey.PrincipalColumns[0]));
                            }
                            else
                            {
                                builder
                                    .Append("principalColumns: ")
                                    .Append(Code.Literal(foreignKey.PrincipalColumns));
                            }

                            if (foreignKey.OnUpdate != ReferentialAction.NoAction)
                            {
                                builder
                                    .AppendLine(",")
                                    .Append("onUpdate: ")
                                    .Append(Code.Literal(foreignKey.OnUpdate));
                            }

                            if (foreignKey.OnDelete != ReferentialAction.NoAction)
                            {
                                builder
                                    .AppendLine(",")
                                    .Append("onDelete: ")
                                    .Append(Code.Literal(foreignKey.OnDelete));
                            }

                            builder.Append(")");

                            Annotations(foreignKey.GetAnnotations(), builder);
                        }

                        builder.AppendLine(";");
                    }
                }

                builder.Append("}");

                if (operation.Comment != null)
                {
                    builder
                        .AppendLine(",")
                        .Append("comment: ")
                        .Append(Code.Literal(operation.Comment));
                }

                builder.Append(")");

                Annotations(operation.GetAnnotations(), builder);
            }
        }

        protected virtual void Generate(CreateUserDefinedTypeOperation operation, IndentedStringBuilder builder)
        {
            builder.AppendLine(".CreateUserDefinedType(");
            using (builder.Indent())
            {
                builder
                    .Append("name: ")
                    .Append(Code.Literal(operation.Name));
                if (!string.IsNullOrWhiteSpace(operation.Schema))
                {
                    builder
                        .AppendLine(",")
                        .Append("schema: ")
                        .Append(Code.Literal(operation.Schema));
                }

                builder
                    .AppendLine(",")
                    .AppendLine("columns: table => new")
                    .AppendLine("{");
                var map = new Dictionary<string, string>();
                using (builder.Indent())
                {
                    var scope = new List<string>();
                    for (int i = 0; i < operation.Columns.Count(); i++)
                    {
                        var column = operation.Columns[i];
                        var propertyName = Code.Identifier(column.Name, scope);
                        map.Add(column.Name, propertyName);
                        builder
                            .Append(propertyName)
                            .Append(" = table.Column<")
                            .Append(Code.Reference(column.ClrType))
                            .Append(">(");
                        builder.Append("nullable: ")
                            .Append(Code.Literal(column.IsNullable));
                        builder.Append(")");
                        if (i != operation.Columns.Count - 1)
                        {
                            builder.Append(",");
                        }

                        builder.AppendLine();
                    }
                }
                builder.Append("}");
                builder.Append(")");
            }
        }

        protected virtual void Generate(DropUserDefinedTypeOperation operation, IndentedStringBuilder builder)
        {
            builder.AppendLine(".DropUserDefinedType(");

            using (builder.Indent())
            {
                builder
                    .Append("name: ")
                    .Append(Code.Literal(operation.Name));

                if (operation.Schema != null)
                {
                    builder
                        .AppendLine(",")
                        .Append("schema: ")
                        .Append(Code.Literal(operation.Schema));
                }

                builder.Append(")");

                Annotations(operation.GetAnnotations(), builder);
            }
        }
    }
}
