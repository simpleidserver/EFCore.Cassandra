// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using EFCore.Cassandra;
using Microsoft.EntityFrameworkCore.Cassandra.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Cassandra.Query.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Microsoft.EntityFrameworkCore.Query
{
    public class CassandraQuerySqlGenerator : QuerySqlGenerator
    {
        private static readonly Regex _composableSql
            = new Regex(@"^\s*?SELECT\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(value: 1000.0));
        private readonly CassandraOptions _opts;
        private readonly ISqlGenerationHelper _sqlGenerationHelper;

        private static readonly Dictionary<ExpressionType, string> _operatorMap = new Dictionary<ExpressionType, string>
        {
            { ExpressionType.Equal, " = " },
            { ExpressionType.NotEqual, " <> " },
            { ExpressionType.GreaterThan, " > " },
            { ExpressionType.GreaterThanOrEqual, " >= " },
            { ExpressionType.LessThan, " < " },
            { ExpressionType.LessThanOrEqual, " <= " },
            { ExpressionType.AndAlso, " AND " },
            { ExpressionType.OrElse, " OR " },
            { ExpressionType.Add, " + " },
            { ExpressionType.Subtract, " - " },
            { ExpressionType.Multiply, " * " },
            { ExpressionType.Divide, " / " },
            { ExpressionType.Modulo, " % " },
            { ExpressionType.And, " & " },
            { ExpressionType.Or, " | " }
        };

        /// <summary>
        ///     Creates a new instance of the <see cref="QuerySqlGenerator" /> class.
        /// </summary>
        /// <param name="dependencies"> Parameter object containing dependencies for this class. </param>
        public CassandraQuerySqlGenerator(CassandraOptions opts, QuerySqlGeneratorDependencies dependencies) : base(dependencies)
        {
            _opts = opts;
            _sqlGenerationHelper = dependencies.SqlGenerationHelper;
        }

        private bool IsNonComposedSetOperation(SelectExpression selectExpression)
            => selectExpression.Offset == null
                && selectExpression.Limit == null
                && !selectExpression.IsDistinct
                && selectExpression.Predicate == null
                && selectExpression.Having == null
                && selectExpression.Orderings.Count == 0
                && selectExpression.GroupBy.Count == 0
                && selectExpression.Tables.Count == 1
                && selectExpression.Tables[0] is SetOperationBase setOperation
                && selectExpression.Projection.Count == setOperation.Source1.Projection.Count
                && selectExpression.Projection.Select(
                        (pe, index) => pe.Expression is ColumnExpression column
                            && string.Equals(column.Table.Alias, setOperation.Alias, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(
                                column.Name, setOperation.Source1.Projection[index].Alias, StringComparison.OrdinalIgnoreCase))
                    .All(e => e);

        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }

        protected override Expression VisitSelect(SelectExpression selectExpression)
        {
            if (IsNonComposedSetOperation(selectExpression))
            {
                GenerateSetOperation((SetOperationBase)selectExpression.Tables[0]);

                return selectExpression;
            }

            IDisposable subQueryIndent = null;

            if (selectExpression.Alias != null)
            {
                Sql.AppendLine("(");
                subQueryIndent = Sql.Indent();
            }

            Sql.Append("SELECT ");

            if (selectExpression.IsDistinct)
            {
                Sql.Append("DISTINCT ");
            }

            // GenerateTop(selectExpression);

            if (selectExpression.Projection.Any())
            {
                GenerateList(selectExpression.Projection, e => Visit(e));
            }
            else
            {
                Sql.Append("1");
            }

            if (selectExpression.Tables.Any())
            {
                Sql.AppendLine().Append("FROM ");

                GenerateList(selectExpression.Tables, e => Visit(e), sql => sql.AppendLine());
            }

            if (selectExpression.Predicate != null)
            {
                Sql.AppendLine().Append("WHERE ");
                var cassandraBinaryExpression = selectExpression.Predicate as CassandraAllowFilteringBinaryExpression;
                if (cassandraBinaryExpression != null)
                {
                    Visit(cassandraBinaryExpression.BinaryExpression);
                    Sql.AppendLine(" ALLOW FILTERING");
                }
                else
                {
                    Visit(selectExpression.Predicate);
                }
            }

            if (selectExpression.GroupBy.Count > 0)
            {
                Sql.AppendLine().Append("GROUP BY ");

                GenerateList(selectExpression.GroupBy, e => Visit(e));
            }

            if (selectExpression.Having != null)
            {
                Sql.AppendLine().Append("HAVING ");

                Visit(selectExpression.Having);
            }

            GenerateOrderings(selectExpression);
            GenerateLimitOffset(selectExpression);

            if (selectExpression.Alias != null)
            {
                subQueryIndent.Dispose();

                Sql.AppendLine()
                    .Append(")" + AliasSeparator + _sqlGenerationHelper.DelimitIdentifier(selectExpression.Alias));
            }

            return selectExpression;
        }

        protected override Expression VisitSqlParameter(SqlParameterExpression sqlParameterExpression)
        {
            // Sql.Append(" ? ");
            // return sqlParameterExpression;
            var parameterNameInCommand = _sqlGenerationHelper.GenerateParameterName(sqlParameterExpression.Name).Replace("_", "").TrimStart(':');

            if (Sql.Parameters.All(p => p.InvariantName != sqlParameterExpression.Name))
            {
                Sql.AddParameter(
                    sqlParameterExpression.Name,
                    parameterNameInCommand,
                    sqlParameterExpression.TypeMapping,
                    sqlParameterExpression.Type.IsNullableType());
            }

            // Sql.Append(_sqlGenerationHelper.GenerateParameterNamePlaceholder(sqlParameterExpression.Name));
            Sql.Append($" :{parameterNameInCommand} ");
            return sqlParameterExpression;
        }

        protected override void GenerateLimitOffset(SelectExpression selectExpression)
        {
            if (selectExpression.Limit != null)
            {
                Sql.AppendLine()
                    .Append("LIMIT ");

                Visit(selectExpression.Limit);
            }
        }

        protected override Expression VisitColumn(ColumnExpression columnExpression)
        {
            Sql
                // .Append(_sqlGenerationHelper.DelimitIdentifier(columnExpression.Table.Alias))
                // .Append(".")
                .Append(_sqlGenerationHelper.DelimitIdentifier(columnExpression.Name));

            return columnExpression;
        }

        protected override Expression VisitTable(TableExpression tableExpression)
        {
            var schema = _opts.DefaultKeyspace;
            Sql
                .Append(_sqlGenerationHelper.DelimitIdentifier(tableExpression.Name, schema));
                // .Append(AliasSeparator)
                // .Append(_sqlGenerationHelper.DelimitIdentifier(tableExpression.Alias));

            return tableExpression;
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
        {
            if (sqlFunctionExpression.IsBuiltIn)
            {
                if (sqlFunctionExpression.Instance != null)
                {
                    Visit(sqlFunctionExpression.Instance);
                    Sql.Append(".");
                }

                Sql.Append(sqlFunctionExpression.Name);
            }
            else
            {
                if (!string.IsNullOrEmpty(sqlFunctionExpression.Schema))
                {
                    Sql
                        .Append(_sqlGenerationHelper.DelimitIdentifier(sqlFunctionExpression.Schema))
                        .Append(".");
                }

                Sql
                    .Append(_sqlGenerationHelper.DelimitIdentifier(sqlFunctionExpression.Name));
            }

            if (!sqlFunctionExpression.IsNiladic)
            {
                Sql.Append("(");
                GenerateList(sqlFunctionExpression.Arguments, e => Visit(e));
                Sql.Append(")");
            }

            return sqlFunctionExpression;
        }

        protected override Expression VisitFromSql(FromSqlExpression fromSqlExpression)
        {
            // _relationalCommandBuilder.AppendLine("(");

            if (!_composableSql.IsMatch(fromSqlExpression.Sql))
            {
                throw new InvalidOperationException(RelationalStrings.FromSqlNonComposable);
            }

            using (Sql.Indent())
            {
                GenerateFromSql(fromSqlExpression);
            }

            // _relationalCommandBuilder.Append(")")
            //     .Append(AliasSeparator)
            //     .Append(_sqlGenerationHelper.DelimitIdentifier(fromSqlExpression.Alias));

            return fromSqlExpression;
        }

        private void GenerateFromSql(FromSqlExpression fromSqlExpression)
        {
            var sql = fromSqlExpression.Sql;
            string[] substitutions = null;

            switch (fromSqlExpression.Arguments)
            {
                case ConstantExpression constantExpression
                    when constantExpression.Value is CompositeRelationalParameter compositeRelationalParameter:
                    {
                        var subParameters = compositeRelationalParameter.RelationalParameters;
                        substitutions = new string[subParameters.Count];
                        for (var i = 0; i < subParameters.Count; i++)
                        {
                            substitutions[i] = _sqlGenerationHelper.GenerateParameterNamePlaceholder(subParameters[i].InvariantName);
                        }

                        Sql.AddParameter(compositeRelationalParameter);

                        break;
                    }

                case ConstantExpression constantExpression
                    when constantExpression.Value is object[] constantValues:
                    {
                        substitutions = new string[constantValues.Length];
                        for (var i = 0; i < constantValues.Length; i++)
                        {
                            var value = constantValues[i];
                            if (value is RawRelationalParameter rawRelationalParameter)
                            {
                                substitutions[i] = _sqlGenerationHelper.GenerateParameterNamePlaceholder(rawRelationalParameter.InvariantName);
                                Sql.AddParameter(rawRelationalParameter);
                            }
                            else if (value is SqlConstantExpression sqlConstantExpression)
                            {
                                substitutions[i] = sqlConstantExpression.TypeMapping.GenerateSqlLiteral(sqlConstantExpression.Value);
                            }
                        }

                        break;
                    }
            }

            if (substitutions != null)
            {
                // ReSharper disable once CoVariantArrayConversion
                // InvariantCulture not needed since substitutions are all strings
                sql = string.Format(sql, substitutions);
            }

            Sql.AppendLines(sql);
        }

        private void GenerateList<T>(
            IReadOnlyList<T> items,
            Action<T> generationAction,
            Action<IRelationalCommandBuilder> joinAction = null)
        {
            joinAction ??= (isb => isb.Append(", "));

            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    joinAction(Sql);
                }

                generationAction(items[i]);
            }
        }
    }
}
