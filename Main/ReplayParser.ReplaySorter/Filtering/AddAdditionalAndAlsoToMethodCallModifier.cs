using ReplayParser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Filtering
{
    public class AddAdditionalAndAlsoToMethodCallModifier : ExpressionVisitor
    {
        private Expression _additionalExpression;
        private string _methodName;

        public AddAdditionalAndAlsoToMethodCallModifier(Expression additionalExpression, string methodName)
        {
            _additionalExpression = additionalExpression;
            _methodName = methodName;
        }

        public Expression Modify(Expression expression)
        {
            return Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.Name != _methodName)
                return base.VisitMethodCall(methodCallExpression);

            var lambdaExpressions = methodCallExpression.Arguments.Where(a => a.NodeType == ExpressionType.Lambda && (a as LambdaExpression).Type == typeof(Func<IPlayer, bool>));
            if (lambdaExpressions.Count() != 1)
                return base.VisitMethodCall(methodCallExpression);

            var playerLambda = lambdaExpressions.First() as Expression<Func<IPlayer, bool>>;
            if (playerLambda == null)
                return base.VisitMethodCall(methodCallExpression);

            var player = playerLambda.Parameters.SingleOrDefault(p => p.Type == typeof(IPlayer));
            if (player == null)
                return base.VisitMethodCall(methodCallExpression);

            var newPlayerLambda = playerLambda.Update(
                Expression.AndAlso(
                    playerLambda.Body,
                    _additionalExpression
                ),
                new List<ParameterExpression>() { player }.AsEnumerable()
           );

            var newArguments = methodCallExpression.Arguments.Where(a => a != playerLambda).ToList();
            newArguments.Add(newPlayerLambda);

            return methodCallExpression.Update(methodCallExpression.Object, newArguments);
        }
    }
}
