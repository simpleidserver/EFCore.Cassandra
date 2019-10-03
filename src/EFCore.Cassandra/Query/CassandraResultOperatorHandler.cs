// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Cassandra.Query
{
    public class CassandraResultOperatorHandler : RelationalResultOperatorHandler
    {
        private readonly IModel _model;
        private readonly ISqlTranslatingExpressionVisitorFactory _sqlTranslatingExpressionVisitorFactory;
        private readonly ISelectExpressionFactory _selectExpressionFactory;
        private readonly IResultOperatorHandler _resultOperatorHandler;

        private static Expression TransformClientExpression<TResult>(
            HandlerContext handlerContext, bool throwOnNullResult = false)
            => new ResultTransformingExpressionVisitor<TResult>(
                    handlerContext.QueryModelVisitor.QueryCompilationContext,
                    throwOnNullResult)
                .Visit(handlerContext.QueryModelVisitor.Expression);

        private sealed class HandlerContext
        {
            private readonly IResultOperatorHandler _resultOperatorHandler;
            private readonly ISqlTranslatingExpressionVisitorFactory _sqlTranslatingExpressionVisitorFactory;

            public HandlerContext(
                IResultOperatorHandler resultOperatorHandler,
                IModel model,
                ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory,
                ISelectExpressionFactory selectExpressionFactory,
                RelationalQueryModelVisitor queryModelVisitor,
                ResultOperatorBase resultOperator,
                QueryModel queryModel,
                SelectExpression selectExpression)
            {
                _resultOperatorHandler = resultOperatorHandler;
                _sqlTranslatingExpressionVisitorFactory = sqlTranslatingExpressionVisitorFactory;

                Model = model;
                SelectExpressionFactory = selectExpressionFactory;
                QueryModelVisitor = queryModelVisitor;
                ResultOperator = resultOperator;
                QueryModel = queryModel;
                SelectExpression = selectExpression;
            }

            public IModel Model { get; }
            public ISelectExpressionFactory SelectExpressionFactory { get; }
            public ResultOperatorBase ResultOperator { get; }
            public SelectExpression SelectExpression { get; }
            public QueryModel QueryModel { get; }
            public RelationalQueryModelVisitor QueryModelVisitor { get; }
            public Expression EvalOnServer => QueryModelVisitor.Expression;

            public Expression EvalOnClient(bool requiresClientResultOperator = true)
            {
                QueryModelVisitor.RequiresClientResultOperator = requiresClientResultOperator;

                return _resultOperatorHandler
                    .HandleResultOperator(QueryModelVisitor, ResultOperator, QueryModel);
            }

            public SqlTranslatingExpressionVisitor CreateSqlTranslatingVisitor()
                => _sqlTranslatingExpressionVisitorFactory.Create(QueryModelVisitor, SelectExpression);
        }

        public CassandraResultOperatorHandler(IModel model, ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory, ISelectExpressionFactory selectExpressionFactory, IResultOperatorHandler resultOperatorHandler) : base(model, sqlTranslatingExpressionVisitorFactory, selectExpressionFactory, resultOperatorHandler)
        {
            _model = model;
            _sqlTranslatingExpressionVisitorFactory = sqlTranslatingExpressionVisitorFactory;
            _selectExpressionFactory = selectExpressionFactory;
            _resultOperatorHandler = resultOperatorHandler;
        }

        private static readonly Dictionary<Type, Func<HandlerContext, Expression>>
            _resultHandlers = new Dictionary<Type, Func<HandlerContext, Expression>>
            {
                { typeof(LongCountResultOperator), HandleLongCount },
                { typeof(CountResultOperator), HandleCount }
                // { typeof(AllResultOperator), HandleAll },
                // { typeof(AnyResultOperator), HandleAny },
                // { typeof(AverageResultOperator), HandleAverage },
                // { typeof(CastResultOperator), HandleCast },
                // { typeof(ContainsResultOperator), HandleContains },
                // { typeof(LongCountResultOperator), HandleLongCount },
                // { typeof(DefaultIfEmptyResultOperator), HandleDefaultIfEmpty },
                // { typeof(DistinctResultOperator), HandleDistinct },
                // { typeof(FirstResultOperator), HandleFirst },
                // { typeof(GroupResultOperator), HandleGroup },
                // { typeof(LastResultOperator), HandleLast },
                // { typeof(MaxResultOperator), HandleMax },
                // { typeof(MinResultOperator), HandleMin },
                // { typeof(SingleResultOperator), HandleSingle },
                // { typeof(SkipResultOperator), HandleSkip },
                // { typeof(SumResultOperator), HandleSum },
                // { typeof(TakeResultOperator), HandleTake }
            };

        public override Expression HandleResultOperator(EntityQueryModelVisitor entityQueryModelVisitor, ResultOperatorBase resultOperator, QueryModel queryModel)
        {
            var relationalQueryModelVisitor
                = (RelationalQueryModelVisitor)entityQueryModelVisitor;

            var selectExpression
                = relationalQueryModelVisitor
                    .TryGetQuery(queryModel.MainFromClause);

            var handlerContext
                = new HandlerContext(
                    _resultOperatorHandler,
                    _model,
                    _sqlTranslatingExpressionVisitorFactory,
                    _selectExpressionFactory,
                    relationalQueryModelVisitor,
                    resultOperator,
                    queryModel,
                    selectExpression);

            if (relationalQueryModelVisitor.RequiresClientEval
                || relationalQueryModelVisitor.RequiresClientSelectMany
                || relationalQueryModelVisitor.RequiresClientJoin
                || relationalQueryModelVisitor.RequiresClientFilter
                || relationalQueryModelVisitor.RequiresClientOrderBy
                || relationalQueryModelVisitor.RequiresClientResultOperator
                || relationalQueryModelVisitor.RequiresStreamingGroupResultOperator
                || !_resultHandlers.TryGetValue(resultOperator.GetType(), out var resultHandler)
                || selectExpression == null)
            {
                return handlerContext.EvalOnClient();
            }
            else
            {
                return resultHandler(handlerContext);
            }
        }

        private static Expression HandleCount(HandlerContext handlerContext)
        {
            PrepareSelectExpressionForAggregate(handlerContext.SelectExpression, handlerContext.QueryModel);

            handlerContext.SelectExpression
                .SetProjectionExpression(
                    new SqlFunctionExpression(
                        "COUNT",
                        typeof(int),
                        new[] { new SqlFragmentExpression("*") }));

            handlerContext.SelectExpression.ClearOrderBy();

            return TransformClientExpression<int>(handlerContext);
        }

        private static Expression HandleLongCount(HandlerContext handlerContext)
        {
            PrepareSelectExpressionForAggregate(handlerContext.SelectExpression, handlerContext.QueryModel);

            handlerContext.SelectExpression
                .SetProjectionExpression(
                    new SqlFunctionExpression(
                        "COUNT",
                        typeof(long),
                        new[] { new SqlFragmentExpression("*") }));

            handlerContext.SelectExpression.ClearOrderBy();

            return TransformClientExpression<long>(handlerContext);
        }
    }
}