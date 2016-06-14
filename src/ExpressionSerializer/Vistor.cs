using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionSerializer
{
    public class Visitor: ExpressionVisitor
    {
        public override Expression Visit(Expression expression)
        {
            return Expression.Lambda<Func<object>>(base.Visit(expression));
        }

        protected override Expression VisitLambda<T>(Expression<T> expression)
        {
            /*if(IsPrimaryConditional)
            {
                return Expression.Lambda<Func<object>>(Visit(expression.Body), expression.TailCall);
            }*/

            var parameters = new List<Expression>();
            parameters.AddRange(expression.Parameters);
            parameters.Add(expression.Body);
            return BuildNode("=>", parameters.ToArray());
        }

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            return Expression.Constant(expression.Value, typeof(object));
        }

        protected override Expression VisitParameter(ParameterExpression expression)
        {
            return Expression.Constant(expression.Name, typeof(object));
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            return BuildNode(".", expression.Expression, Expression.Constant(expression.Member.Name));
        }

        protected override Expression VisitUnary(UnaryExpression expression)
        {
            var operatorKey = DetermineKeyFromBinaryType(expression.NodeType);
            return BuildNode(operatorKey, expression.Operand);
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            var operatorKey = DetermineKeyFromBinaryType(expression.NodeType);
            return BuildNode(operatorKey, expression.Left, expression.Right);   
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            var key = expression.Method.Name;
            var initializers = new List<Expression>();
            if(expression.Object != null)
            {
                initializers.Add(expression.Object);
            }

            initializers.AddRange(expression.Arguments);
            return BuildNode(key, initializers.ToArray());
        }
        
        protected override Expression VisitConditional(ConditionalExpression expression)
        {
            return BuildNode("?", expression.Test, expression.IfTrue, expression.IfFalse);
        }
        
        private Expression BuildNode(string key, params Expression[] initializers)
        {
            var visitedInitializers = initializers.Select(i => base.Visit(i));
            var ctor = Expression.New(nodeType);
            return Expression.ListInit(
                ctor,
                Expression.ElementInit(
                    addNode,
                    Expression.Constant(key),
                    Expression.NewArrayInit(
                        typeof(object),
                        visitedInitializers
                    )
                )
            );
        }

        private string DetermineKeyFromBinaryType(ExpressionType type)
        {
            var types = new Dictionary<ExpressionType, string>{
                {ExpressionType.Equal, "=="},
                {ExpressionType.NotEqual, "!="},
                {ExpressionType.Add, "+"},
                {ExpressionType.Subtract, "-"},
                {ExpressionType.Multiply, "*"},
                {ExpressionType.Divide, "/"},
                {ExpressionType.AndAlso, "&&"},
                {ExpressionType.OrElse, "||"},
                {ExpressionType.GreaterThan, ">"},
                {ExpressionType.GreaterThanOrEqual, ">="},
                {ExpressionType.LessThan, "<"},
                {ExpressionType.LessThanOrEqual, "<="},
                {ExpressionType.Modulo, "%"},
                {ExpressionType.Not, "!"}
            };

            if(types.ContainsKey(type))
            {
                return types[type];
            }
            
            throw new InvalidOperationException($"Operator type `{type.ToString()}` is not supported.");
        }

        private Type nodeType = typeof(Dictionary<string, IEnumerable<object>>);
        private MethodInfo addNode = typeof(Dictionary<string, IEnumerable<object>>).GetMethod("Add");
    }
}