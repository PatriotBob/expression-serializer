using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ExpressionSerializer
{
    public class Builder<TIn, TOut>
    {
        public Expression<Func<TIn, TOut>> Build(Dictionary<string, IEnumerable<object>> data)
        {
            Console.WriteLine("Building root.");
            return BuildLambda<TIn, TOut>(data);
        }

        protected Expression Build(object data)
        {
            var type = data.GetType();
            if(type == typeof(Dictionary<string, IEnumerable<object>>))
            {
                Console.WriteLine("Found dictionary");
                var dataObject = (Dictionary<string, IEnumerable<object>>)data;
                var builder = GetBuilder(dataObject);
                return builder(dataObject);
            }
            if(type == typeof(string) || type == typeof(int) || type == typeof(decimal) || type == typeof(bool) || type == typeof(float) || type == typeof(double))
            {
                if(type == typeof(string))
                {
                    var param = ParameterExpressions.SingleOrDefault(p => p.Name == (string)data);
                    if(param != null) {
                        return param;
                    }
                }
                Console.WriteLine($"Found {type}");
                return BuildConstant(data);
            }

            Console.WriteLine($"Couldn't find {type}");

            return null;
        }

        protected Expression<Func<TLambdaIn, TLambdaOut>> BuildLambda<TLambdaIn, TLambdaOut>(Dictionary<string, IEnumerable<object>> data)
        {
            var dataParams = data.Values.First();
            ParameterExpressions = dataParams.Take(dataParams.Count() - 1).Select(p => BuildParameter<TLambdaIn>((string)p)); 
            return Expression.Lambda<Func<TLambdaIn, TLambdaOut>>(
                Build(dataParams.Last()),
                ParameterExpressions
            );
        }

        protected LambdaExpression BuildLambda(Dictionary<string, IEnumerable<object>> data)
        {
            var dataParams = data.Values.First();
            var lambdaParams = dataParams.Take(dataParams.Count() - 1);
            var body = dataParams.Last();
            return Expression.Lambda(
                Build(body),
                lambdaParams.Select(p => BuildParameter<object>((string)p))
            );
        }

        protected ParameterExpression BuildParameter<T>(string data)
        {
            return Expression.Parameter(typeof(T), data);
        }

        protected ConstantExpression BuildConstant(object data)
        {
            return Expression.Constant(data, data.GetType());
        }

        protected MemberExpression BuildMember(Dictionary<string, IEnumerable<object>> data)
        {
            var dataParams = data.Values.First();
            var objectExpression = Build(dataParams.First());
            Console.WriteLine($"Looking for member `{dataParams.Last()}` on type `{objectExpression.Type.FullName}`");
            return Expression.MakeMemberAccess(
                objectExpression,
                objectExpression.Type.GetMember((string)dataParams.Last()).First()
            );
        }

        protected BinaryExpression BuildBinary(Dictionary<string, IEnumerable<object>> data)
        {
            var type = GetExpressionType(data);
            var dataParams = data.Values.First();
            var leftExpression = Build(dataParams.First());
            var rightExpression = Build(dataParams.Last());
            if(leftExpression.Type != rightExpression.Type && leftExpression.NodeType == ExpressionType.Constant || rightExpression.NodeType == ExpressionType.Constant)
            {
                if(leftExpression.NodeType == ExpressionType.Constant) {
                    leftExpression = Expression.Constant(Convert.ChangeType(((ConstantExpression)leftExpression).Value, rightExpression.Type), rightExpression.Type);
                } else {
                    rightExpression = Expression.Constant(Convert.ChangeType(((ConstantExpression)rightExpression).Value, leftExpression.Type), leftExpression.Type);
                }
            }

            return Expression.MakeBinary(
                type,
                leftExpression,
                rightExpression
            );
        }

        protected UnaryExpression BuildNot(Dictionary<string, IEnumerable<object>> data)
        {
            var type = GetExpressionType(data);
            var dataParams = data.Values.First();
            return Expression.Not(
                Build(dataParams.First())
            );
        }

        protected MethodCallExpression BuildMethodCall(Dictionary<string, IEnumerable<object>> data)
        {
            var methodName = data.Keys.First();
            var dataParams = data.Values.First();
            var methodParams = dataParams.Skip(1);
            var methodParamExpressionTypes = methodParams.Select(p => p.GetType() == typeof(Dictionary<string, IEnumerable<object>>) ? GetExpressionType((Dictionary<string, IEnumerable<object>>)p) : ExpressionType.Constant);
            if(methodParamExpressionTypes.Any(et => et == ExpressionType.Lambda))
            {
                
            }
            var methodParamExpressions = methodParams.Select(p => Build(p));
            var instanceExpression = Build(dataParams.First());
            var methodParamTypes = methodParamExpressions.Select(p => p.Type).ToArray();
            var methodInfo = GetMethodInfo(instanceExpression.Type, methodName, methodParamTypes);
            if(methodInfo == null)
            {
                var newParams = new List<Expression> { instanceExpression };
                newParams.AddRange(methodParamExpressions);
                methodParamExpressions = newParams;
                methodParamTypes = methodParamExpressions.Select(p => p.Type).ToArray();
                methodInfo = GetMethodInfo(typeof(System.Linq.Enumerable), methodName, instanceExpression.Type.GenericTypeArguments, methodParamTypes);
            }
            if(methodInfo.IsStatic)
            {
                if(methodParamExpressions.Any())
                {
                    return Expression.Call(
                        null,
                        methodInfo,
                        methodParamExpressions
                    );
                }

                return Expression.Call(
                    null,
                    methodInfo
                );
            } else {
                if(methodParamExpressions.Any())
                {
                    return Expression.Call(
                        instanceExpression,
                        methodInfo,
                        methodParamExpressions
                    );
                }

                return Expression.Call(
                    instanceExpression,
                    methodInfo
                );
            }  
        }

        protected ConditionalExpression BuildConditional(Dictionary<string, IEnumerable<object>> data)
        {
            var dataParams = data.Values.First();
            var conditionalParams = dataParams.Select(p => Build(p)).ToArray();
            return Expression.Condition(
                conditionalParams[0],
                conditionalParams[1],
                conditionalParams[2]
            );
        }

        private Func<Dictionary<string, IEnumerable<object>>, Expression> GetBuilder(Dictionary<string, IEnumerable<object>> data)
        {
            var key = data.Keys.First();
            if(MethodCallCheck.IsMatch(key))
                return d => BuildMethodCall(d);
            CheckKeyExists(key);
            return types[key].Method;
        }

        private ExpressionType GetExpressionType(Dictionary<string, IEnumerable<object>> data)
        {
            var key = data.Keys.First();
            CheckKeyExists(key);
            return types[key].Type;
        }

        private Dictionary<string, OperatorInfo> types
        {
            get
            {
                return new Dictionary<string, OperatorInfo> {
                    {"=>", new OperatorInfo {Method = d => BuildLambda(d), Type = ExpressionType.Lambda}},
                    {"==", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.Equal}},
                    {"!=", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.NotEqual}},
                    {"+", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.Add}},
                    {"-", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.Subtract}},
                    {"*", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.Multiply}},
                    {"/", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.Divide}},
                    {"&&", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.AndAlso}},
                    {"||", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.OrElse}},
                    {">", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.GreaterThan}},
                    {">=", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.GreaterThanOrEqual}},
                    {"<", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.LessThan}},
                    {"<=", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.LessThanOrEqual}},
                    {"%", new OperatorInfo {Method = d => BuildBinary(d), Type = ExpressionType.Modulo}},
                    {"!", new OperatorInfo {Method = d => BuildNot(d), Type = ExpressionType.Not}},
                    {".", new OperatorInfo {Method = d => BuildMember(d), Type = ExpressionType.MemberAccess}},
                    {"?", new OperatorInfo {Method = d => BuildConditional(d), Type = ExpressionType.Conditional}}
                };
            }
        }

        private void CheckKeyExists(string key)
        {
            if(!types.ContainsKey(key))
                throw new InvalidOperationException($"Builder does not support the `{key}` operator.");
        }

        protected IEnumerable<ParameterExpression> ParameterExpressions { get; set; }

        private struct OperatorInfo
        {
            public Func<Dictionary<string, IEnumerable<object>>, Expression> Method;
            public ExpressionType Type;
        }

        private Regex MethodCallCheck = new Regex(@"^\w+$");

        private MethodInfo GetMethodInfo(Type instanceType, string methodName, params Type[] parameterTypes)
        {
            var methods = GetMethodsByName(instanceType, methodName, parameterTypes);
            if (parameterTypes.Any())
            {
                methods = methods.Where(mi => mi.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes, new DerivedTypeComparer()));
            }

            Console.WriteLine($"Found {methods.Count()} methods matching param types");
            return methods.FirstOrDefault();
        }

        private MethodInfo GetMethodInfo(Type instanceType, string methodName, Type[] genericParams, params Type[] parameterTypes)
        {
            var methods = GetMethodsByName(instanceType, methodName, parameterTypes);
            if (parameterTypes.Any())
            {
                methods = methods.Where(mi => mi.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes, new GenericDerivedTypeComparer()));
            }
            
            Console.WriteLine($"Found {methods.Count()} methods matching param types");
            var method = methods.FirstOrDefault();
            if(method.IsGenericMethod) {
                method = method.MakeGenericMethod(genericParams);
            }
            return method;
        }

        private static IEnumerable<MethodInfo> GetMethodsByName(Type instanceType, string methodName, Type[] parameterTypes)
        {
            var parameterTypeNames = string.Join(", ", parameterTypes.Select(p => p.Name));
            Console.WriteLine($"Looking form {methodName} on {instanceType.Name} with params {parameterTypeNames}.");
            var methods = instanceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(mi => mi.Name == methodName);
            Console.WriteLine($"Found {methods.Count()} methods matching name");
            return methods;
        }

        private class DerivedTypeComparer : IEqualityComparer<Type>
        {
            public virtual bool Equals(Type x, Type y)
            {
                return x.IsAssignableFrom(y);
            }

            public int GetHashCode(Type obj)
            {
                return obj.GetHashCode();
            }
        }
        private class GenericDerivedTypeComparer : DerivedTypeComparer
        {
            public override bool Equals(Type x, Type y)
            {
                Console.WriteLine($"Checking if {x.Name} is assignable from {y.Name}: {x.IsAssignableFrom(y)}");
                if(x.GenericTypeArguments.Any(t => t.IsGenericParameter) && x.GenericTypeArguments.Length == y.GenericTypeArguments.Length)
                {
                    var typeArgs = x.GenericTypeArguments.Select((t, i) => t.IsGenericParameter ? y.GenericTypeArguments[i] : t).ToArray();
                    var concreteType = x.GetGenericTypeDefinition().MakeGenericType(typeArgs);
           	        Console.WriteLine($"{concreteType.IsAssignableFrom(y)}");
                    return concreteType.IsAssignableFrom(y); 
                } else {
                    return base.Equals(x, y);
                }
            }
        }
    }
}