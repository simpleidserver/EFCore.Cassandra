// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Cassandra.Query.Internal
{
    public class CassandraQueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
    {
        private readonly RelationalSqlTranslatingExpressionVisitor _sqlTranslator;
        private readonly WeakEntityExpandingExpressionVisitor _weakEntityExpandingExpressionVisitor;
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        public CassandraQueryableMethodTranslatingExpressionVisitor(
            [NotNull] QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
            [NotNull] RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] SqlExpressionFactoryDependencies sqlExpressionFactoryDependencies)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
            var sqlExpressionFactory = relationalDependencies.SqlExpressionFactory;
            _sqlTranslator = relationalDependencies.RelationalSqlTranslatingExpressionVisitorFactory.Create(queryCompilationContext, this);
            _weakEntityExpandingExpressionVisitor = new WeakEntityExpandingExpressionVisitor(_sqlTranslator, sqlExpressionFactory);
            _typeMappingSource = sqlExpressionFactoryDependencies.TypeMappingSource;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(RelationalQueryableExtensions) && methodCallExpression.Method.Name == "FromSqlOnQueryable")
            {
                return Visit(methodCallExpression.Arguments[0]);
            }

            var method = methodCallExpression.Method;
            if (method.DeclaringType == typeof(CassandraQueryable))
            {
                var source = Visit(methodCallExpression.Arguments[0]);
                if (source is ShapedQueryExpression shapedQueryExpression)
                {
                    var genericMethod = method.IsGenericMethod ? method.GetGenericMethodDefinition() : null;
                    switch (method.Name)
                    {
                        case nameof(CassandraQueryable.Where)
                            when genericMethod == CassandraWhere:
                            LambdaExpression GetLambdaExpressionFromArgument(int argumentIndex) => UnwrapLambdaFromQuote(methodCallExpression.Arguments[argumentIndex]);
                            return TranslateCassandraWhere(shapedQueryExpression, GetLambdaExpressionFromArgument(1));
                    }
                }
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        private ShapedQueryExpression TranslateCassandraWhere(ShapedQueryExpression source, LambdaExpression predicate)
        {
            var translation = TranslateLambdaExpression(source, predicate);
            if (translation == null)
            {
                return null;
            }

            var mapping = _typeMappingSource.FindMapping(typeof(bool));
            var expression = new CassandraAllowFilteringBinaryExpression(mapping, typeof(bool), translation);
            ((SelectExpression)source.QueryExpression).ApplyPredicate(expression);

            return source;
        }

        private SqlExpression TranslateLambdaExpression(
            ShapedQueryExpression shapedQueryExpression,
            LambdaExpression lambdaExpression)
            => TranslateExpression(RemapLambdaBody(shapedQueryExpression, lambdaExpression));

        private SqlExpression TranslateExpression(Expression expression)
        {
            var translation = _sqlTranslator.Translate(expression);
            if (translation == null && _sqlTranslator.TranslationErrorDetails != null)
            {
                AddTranslationErrorDetails(_sqlTranslator.TranslationErrorDetails);
            }

            return translation;
        }

        private Expression RemapLambdaBody(ShapedQueryExpression shapedQueryExpression, LambdaExpression lambdaExpression)
        {
            var lambdaBody = ReplacingExpressionVisitor.Replace(
                lambdaExpression.Parameters.Single(), shapedQueryExpression.ShaperExpression, lambdaExpression.Body);

            return ExpandWeakEntities((SelectExpression)shapedQueryExpression.QueryExpression, lambdaBody);
        }

        internal Expression ExpandWeakEntities(SelectExpression selectExpression, Expression lambdaBody)
            => _weakEntityExpandingExpressionVisitor.Expand(selectExpression, lambdaBody);

        private static LambdaExpression UnwrapLambdaFromQuote(Expression expression)
            => (LambdaExpression)(expression is UnaryExpression unary && expression.NodeType == ExpressionType.Quote
                ? unary.Operand
                : expression);

        private static MethodInfo CassandraWhere = GetCassandraQueryableMethods().Single(
            mi => mi.Name == nameof(Queryable.Where)
                    && mi.GetParameters().Length == 3);

        private static List<MethodInfo> GetCassandraQueryableMethods() => typeof(CassandraQueryable).GetTypeInfo()
               .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).ToList();

        private static ShapedQueryExpression CreateShapedQueryExpression(IEntityType entityType, SelectExpression selectExpression)
            => new ShapedQueryExpression(
                selectExpression,
                new RelationalEntityShaperExpression(
                    entityType,
                    new ProjectionBindingExpression(
                        selectExpression,
                        new ProjectionMember(),
                        typeof(ValueBuffer)),
                    false));
        private sealed class WeakEntityExpandingExpressionVisitor : ExpressionVisitor
        {
            private static readonly MethodInfo _objectEqualsMethodInfo
                = typeof(object).GetRuntimeMethod(nameof(object.Equals), new[] { typeof(object), typeof(object) });

            private readonly RelationalSqlTranslatingExpressionVisitor _sqlTranslator;
            private readonly ISqlExpressionFactory _sqlExpressionFactory;

            private SelectExpression _selectExpression;

            public WeakEntityExpandingExpressionVisitor(
                RelationalSqlTranslatingExpressionVisitor sqlTranslator,
                ISqlExpressionFactory sqlExpressionFactory)
            {
                _sqlTranslator = sqlTranslator;
                _sqlExpressionFactory = sqlExpressionFactory;
            }

            public string TranslationErrorDetails
                => _sqlTranslator.TranslationErrorDetails;

            public Expression Expand(SelectExpression selectExpression, Expression lambdaBody)
            {
                _selectExpression = selectExpression;

                return Visit(lambdaBody);
            }

            protected override Expression VisitMember(MemberExpression memberExpression)
            {
                var innerExpression = Visit(memberExpression.Expression);

                return TryExpand(innerExpression, MemberIdentity.Create(memberExpression.Member))
                    ?? memberExpression.Update(innerExpression);
            }

            protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
            {
                if (methodCallExpression.TryGetEFPropertyArguments(out var source, out var navigationName))
                {
                    source = Visit(source);

                    return TryExpand(source, MemberIdentity.Create(navigationName))
                        ?? methodCallExpression.Update(null, new[] { source, methodCallExpression.Arguments[1] });
                }

                if (methodCallExpression.TryGetEFPropertyArguments(out source, out navigationName))
                {
                    source = Visit(source);

                    return TryExpand(source, MemberIdentity.Create(navigationName))
                        ?? methodCallExpression.Update(source, new[] { methodCallExpression.Arguments[1] });
                }

                return base.VisitMethodCall(methodCallExpression);
            }

            protected override Expression VisitExtension(Expression extensionExpression)
            {
                return extensionExpression is EntityShaperExpression
                    || extensionExpression is ShapedQueryExpression
                    ? extensionExpression
                    : base.VisitExtension(extensionExpression);
            }

            private Expression TryExpand(Expression source, MemberIdentity member)
            {
                source = source.UnwrapTypeConversion(out var convertedType);
                if (!(source is EntityShaperExpression entityShaperExpression))
                {
                    return null;
                }

                var entityType = entityShaperExpression.EntityType;
                if (convertedType != null)
                {
                    entityType = entityType.GetRootType().GetDerivedTypesInclusive()
                        .FirstOrDefault(et => et.ClrType == convertedType);

                    if (entityType == null)
                    {
                        return null;
                    }
                }

                var navigation = member.MemberInfo != null
                    ? entityType.FindNavigation(member.MemberInfo)
                    : entityType.FindNavigation(member.Name);

                if (navigation == null)
                {
                    return null;
                }

                var targetEntityType = navigation.TargetEntityType;
                if (targetEntityType == null
                    || (!targetEntityType.HasDefiningNavigation()
                        && !targetEntityType.IsOwned()))
                {
                    return null;
                }

                var foreignKey = navigation.ForeignKey;
                if (navigation.IsCollection)
                {
                    var innerShapedQuery = CreateShapedQueryExpression(
                        targetEntityType, _sqlExpressionFactory.Select(targetEntityType));

                    var makeNullable = foreignKey.PrincipalKey.Properties
                        .Concat(foreignKey.Properties)
                        .Select(p => p.ClrType)
                        .Any(t => t.IsNullableType());

                    var innerSequenceType = innerShapedQuery.Type.TryGetSequenceType();
                    var correlationPredicateParameter = Expression.Parameter(innerSequenceType);

                    var outerKey = entityShaperExpression.CreateKeyValuesExpression(
                        navigation.IsOnDependent
                            ? foreignKey.Properties
                            : foreignKey.PrincipalKey.Properties,
                        makeNullable);
                    var innerKey = correlationPredicateParameter.CreateKeyValuesExpression(
                        navigation.IsOnDependent
                            ? foreignKey.PrincipalKey.Properties
                            : foreignKey.Properties,
                        makeNullable);

                    Expression predicate = null;
                    if (AppContext.TryGetSwitch("Microsoft.EntityFrameworkCore.Issue23130", out var isEnabled) && isEnabled)
                    {
                        var outerKeyFirstProperty = outerKey is NewExpression newExpression
                            ? ((UnaryExpression)((NewArrayExpression)newExpression.Arguments[0]).Expressions[0]).Operand
                            : outerKey;

                        predicate = outerKeyFirstProperty.Type.IsNullableType()
                            ? Expression.AndAlso(
                                Expression.NotEqual(outerKeyFirstProperty, Expression.Constant(null, outerKeyFirstProperty.Type)),
                                Expression.Equal(outerKey, innerKey))
                            : Expression.Equal(outerKey, innerKey);
                    }
                    else
                    {
                        var keyComparison = Expression.Call(_objectEqualsMethodInfo, AddConvertToObject(outerKey), AddConvertToObject(innerKey));

                        predicate = makeNullable
                            ? Expression.AndAlso(
                                outerKey is NewArrayExpression newArrayExpression
                                    ? newArrayExpression.Expressions
                                        .Select(
                                            e =>
                                            {
                                                var left = (e as UnaryExpression)?.Operand ?? e;

                                                return Expression.NotEqual(left, Expression.Constant(null, left.Type));
                                            })
                                        .Aggregate((l, r) => Expression.AndAlso(l, r))
                                    : Expression.NotEqual(outerKey, Expression.Constant(null, outerKey.Type)),
                                keyComparison)
                            : (Expression)keyComparison;
                    }

                    var correlationPredicate = Expression.Lambda(predicate, correlationPredicateParameter);

                    return Expression.Call(
                        QueryableMethods.Where.MakeGenericMethod(innerSequenceType),
                        innerShapedQuery,
                        Expression.Quote(correlationPredicate));
                }

                var entityProjectionExpression = (EntityProjectionExpression)
                    (entityShaperExpression.ValueBufferExpression is ProjectionBindingExpression projectionBindingExpression
                        ? _selectExpression.GetMappedProjection(projectionBindingExpression.ProjectionMember)
                        : entityShaperExpression.ValueBufferExpression);

                var innerShaper = entityProjectionExpression.BindNavigation(navigation);
                if (innerShaper == null)
                {
                    // Owned types don't support inheritance See https://github.com/dotnet/efcore/issues/9630
                    // So there is no handling for dependent having TPT
                    // If navigation is defined on derived type and entity type is part of TPT then we need to get ITableBase for derived type.
                    // TODO: The following code should also handle Function and SqlQuery mappings
                    var table = navigation.DeclaringEntityType.BaseType == null
                        || entityType.GetDiscriminatorProperty() != null
                            ? navigation.DeclaringEntityType.GetViewOrTableMappings().Single().Table
                            : navigation.DeclaringEntityType.GetViewOrTableMappings().Select(tm => tm.Table)
                                .Except(navigation.DeclaringEntityType.BaseType.GetViewOrTableMappings().Select(tm => tm.Table))
                                .Single();
                    if (table.GetReferencingRowInternalForeignKeys(foreignKey.PrincipalEntityType)?.Contains(foreignKey) == true)
                    {
                        // Mapped to same table
                        // We get identifying column to figure out tableExpression to pull columns from and nullability of most principal side
                        var identifyingColumn = entityProjectionExpression.BindProperty(entityType.FindPrimaryKey().Properties.First());
                        var principalNullable = identifyingColumn.IsNullable
                            // Also make nullable if navigation is on derived type and and principal is TPT
                            // Since identifying PK would be non-nullable but principal can still be null
                            // Derived owned navigation does not de-dupe the PK column which for principal is from base table
                            // and for dependent on derived table
                            || (entityType.GetDiscriminatorProperty() == null
                                && navigation.DeclaringEntityType.IsStrictlyDerivedFrom(entityShaperExpression.EntityType));

                        var propertyExpressions = GetPropertyExpressionFromSameTable(
                            targetEntityType, table, _selectExpression, identifyingColumn, principalNullable);
                        if (propertyExpressions != null)
                        {
                            innerShaper = new RelationalEntityShaperExpression(
                                targetEntityType, new EntityProjectionExpression(targetEntityType, propertyExpressions), true);
                        }
                    }

                    if (innerShaper == null)
                    {
                        // InnerShaper is still null if either it is not table sharing or we failed to find table to pick data from
                        // So we find the table it is mapped to and generate join with it.
                        // Owned types don't support inheritance See https://github.com/dotnet/efcore/issues/9630
                        // So there is no handling for dependent having TPT
                        table = targetEntityType.GetViewOrTableMappings().Single().Table;
                        var innerSelectExpression = _sqlExpressionFactory.Select(targetEntityType);
                        var innerShapedQuery = CreateShapedQueryExpression(targetEntityType, innerSelectExpression);

                        var makeNullable = foreignKey.PrincipalKey.Properties
                            .Concat(foreignKey.Properties)
                            .Select(p => p.ClrType)
                            .Any(t => t.IsNullableType());

                        var outerKey = entityShaperExpression.CreateKeyValuesExpression(
                            navigation.IsOnDependent
                                ? foreignKey.Properties
                                : foreignKey.PrincipalKey.Properties,
                            makeNullable);
                        var innerKey = innerShapedQuery.ShaperExpression.CreateKeyValuesExpression(
                            navigation.IsOnDependent
                                ? foreignKey.PrincipalKey.Properties
                                : foreignKey.Properties,
                            makeNullable);

                        var joinPredicate = _sqlTranslator.Translate(Expression.Equal(outerKey, innerKey));
                        _selectExpression.AddLeftJoin(innerSelectExpression, joinPredicate);
                        var leftJoinTable = ((LeftJoinExpression)_selectExpression.Tables.Last()).Table;
                        var propertyExpressions = GetPropertyExpressionsFromJoinedTable(targetEntityType, table, leftJoinTable);

                        innerShaper = new RelationalEntityShaperExpression(
                            targetEntityType, new EntityProjectionExpression(targetEntityType, propertyExpressions), true);
                    }

                    entityProjectionExpression.AddNavigationBinding(navigation, innerShaper);
                }

                return innerShaper;
            }

            private static Expression AddConvertToObject(Expression expression)
                => expression.Type.IsValueType
                    ? Expression.Convert(expression, typeof(object))
                    : expression;

            private static IDictionary<IProperty, ColumnExpression> GetPropertyExpressionFromSameTable(
                IEntityType entityType,
                ITableBase table,
                SelectExpression selectExpression,
                ColumnExpression identifyingColumn,
                bool nullable)
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                if (identifyingColumn.Table is TableExpression tableExpression)
                {
                    if (!string.Equals(tableExpression.Name, table.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // Fetch the table for the type which is defining the navigation since dependent would be in that table
                        tableExpression = selectExpression.Tables
                            .Select(t => (t as InnerJoinExpression)?.Table ?? (t as LeftJoinExpression)?.Table ?? t)
                            .Cast<TableExpression>()
                            .First(t => string.Equals(t.Name, table.Name) && string.Equals(t.Schema, table.Schema));
                    }

                    var propertyExpressions = new Dictionary<IProperty, ColumnExpression>();
                    foreach (var property in entityType
                        .GetAllBaseTypes().Concat(entityType.GetDerivedTypesInclusive())
                        .SelectMany(EntityTypeExtensions.GetDeclaredProperties))
                    {
                        var instance = Activator.CreateInstance(typeof(ColumnExpression), flags,
                            property, table.FindColumn(property), tableExpression, nullable || !property.IsPrimaryKey());
                        propertyExpressions[property] = (ColumnExpression)instance;
                    }

                    return propertyExpressions;
                }

                if (identifyingColumn.Table is SelectExpression subquery)
                {
                    var subqueryIdentifyingColumn = (ColumnExpression)subquery.Projection
                        .SingleOrDefault(e => string.Equals(e.Alias, identifyingColumn.Name, StringComparison.OrdinalIgnoreCase))
                        .Expression;

                    var subqueryPropertyExpressions = GetPropertyExpressionFromSameTable(
                        entityType, table, subquery, subqueryIdentifyingColumn, nullable);

                    if (subqueryPropertyExpressions == null)
                    {
                        return null;
                    }

                    var newPropertyExpressions = new Dictionary<IProperty, ColumnExpression>();
                    foreach (var item in subqueryPropertyExpressions)
                    {
                        var instance = Activator.CreateInstance(typeof(ColumnExpression), flags, subquery.Projection[subquery.AddToProjection(item.Value)], subquery);
                        newPropertyExpressions[item.Key] = (ColumnExpression)instance;
                    }

                    return newPropertyExpressions;
                }

                return null;
            }

            private static IDictionary<IProperty, ColumnExpression> GetPropertyExpressionsFromJoinedTable(
                IEntityType entityType,
                ITableBase table,
                TableExpressionBase tableExpression)
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var propertyExpressions = new Dictionary<IProperty, ColumnExpression>();
                foreach (var property in entityType
                    .GetAllBaseTypes().Concat(entityType.GetDerivedTypesInclusive()).SelectMany(EntityTypeExtensions.GetDeclaredProperties))
                {
                    var instance = Activator.CreateInstance(typeof(ColumnExpression), flags, property, table.FindColumn(property), tableExpression, true);
                    propertyExpressions[property] = (ColumnExpression)instance;
                }

                return propertyExpressions;
            }
        }

        /*
        private ShapedQueryExpression AggregateResultShaper(
            ShapedQueryExpression source, Expression projection, bool throwOnNullResult, Type resultType)
        {
            if (projection == null)
            {
                return null;
            }

            var selectExpression = (SelectExpression)source.QueryExpression;
            selectExpression.ReplaceProjectionMapping(
                new Dictionary<ProjectionMember, Expression> { { new ProjectionMember(), projection } });

            selectExpression.ClearOrdering();

            var nullableResultType = resultType.MakeNullable();
            Expression shaper = new ProjectionBindingExpression(
                source.QueryExpression, new ProjectionMember(), throwOnNullResult ? nullableResultType : projection.Type);

            if (throwOnNullResult)
            {
                var resultVariable = Expression.Variable(nullableResultType, "result");
                var returnValueForNull = resultType.IsNullableType()
                    ? (Expression)Expression.Constant(null, resultType)
                    : Expression.Throw(
                        Expression.New(
                            typeof(InvalidOperationException).GetConstructors()
                                .Single(ci => ci.GetParameters().Length == 1),
                            Expression.Constant(CoreStrings.NoElements)),
                        resultType);

                shaper = Expression.Block(
                    new[] { resultVariable },
                    Expression.Assign(resultVariable, shaper),
                    Expression.Condition(
                        Expression.Equal(resultVariable, Expression.Default(nullableResultType)),
                        returnValueForNull,
                        resultType != resultVariable.Type
                            ? Expression.Convert(resultVariable, resultType)
                            : (Expression)resultVariable));
            }
            else if (resultType != shaper.Type)
            {
                shaper = Expression.Convert(shaper, resultType);
            }

            source.ShaperExpression = shaper;

            return source;
        }

        private static LambdaExpression UnwrapLambdaFromQuote(Expression expression)
            => (LambdaExpression)(expression is UnaryExpression unary && expression.NodeType == ExpressionType.Quote
                ? unary.Operand
                : expression);

        private static MethodInfo CassandraWhere = GetCassandraQueryableMethods().Single(
            mi => mi.Name == nameof(Queryable.Where)
                    && mi.GetParameters().Length == 3);

        private static List<MethodInfo> GetCassandraQueryableMethods()
        {
            return typeof(CassandraQueryable).GetTypeInfo()
               .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).ToList();
        }
        */
    }
}
