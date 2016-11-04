using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExpressionBuilder.ConsoleTest
{
    class reference
    {
        private void meth()
        {
            ParameterExpression dictExpr = Expression.Parameter(typeof(Dictionary<string, int>));
            ParameterExpression keyExpr = Expression.Parameter(typeof(string));
            ParameterExpression valueExpr = Expression.Parameter(typeof(int));

            // Simple and direct. Should normally be enough
            // PropertyInfo indexer = dictExpr.Type.GetProperty("Item");

            // Alternative, note that we could even look for the type of parameters, if there are indexer overloads.
            PropertyInfo indexer = (dictExpr.Type.GetDefaultMembers()
                .OfType<PropertyInfo>()
                .Where(p => p.PropertyType == typeof(int))
                .Select(p => new { p, q = p.GetIndexParameters() })
                .Where(t => t.q.Length == 1 && t.q[0].ParameterType == typeof(string))
                .Select(t => t.p)).Single();

            PropertyInfo indexer1 = (from p in dictExpr.Type.GetDefaultMembers().OfType<PropertyInfo>()
                                         // This check is probably useless. You can't overload on return value in C#.
                                     where p.PropertyType == typeof(int)
                                     let q = p.GetIndexParameters()
                                     // Here we can search for the exact overload. Length is the number of "parameters" of the indexer, and then we can check for their type.
                                     where q.Length == 1 && q[0].ParameterType == typeof(string)
                                     select p).Single();



            IndexExpression indexExpr = Expression.Property(dictExpr, indexer, keyExpr);

            BinaryExpression assign = Expression.Assign(indexExpr, valueExpr);

            var lambdaSetter = Expression.Lambda<Action<Dictionary<string, int>, string, int>>(assign, dictExpr, keyExpr, valueExpr);
            var lambdaGetter = Expression.Lambda<Func<Dictionary<string, int>, string, int>>(indexExpr, dictExpr, keyExpr);
            var setter = lambdaSetter.Compile();
            var getter = lambdaGetter.Compile();

            var dict = new Dictionary<string, int>();
            setter(dict, "MyKey", 2);
            var value = getter(dict, "MyKey");
        }

        Expression<Func<bool, MyObject>> BuildLambda1()
        {
            var createdType = typeof(MyObject);
            var displayValueParam = Expression.Parameter(typeof(bool), "displayValue");
            var ctor = Expression.New(createdType);
            var displayValueProperty = createdType.GetProperty("DisplayValue");
            var displayValueAssignment = Expression.Bind(
                displayValueProperty, displayValueParam);
            var memberInit = Expression.MemberInit(ctor, displayValueAssignment);

            return
                Expression.Lambda<Func<bool, MyObject>>(memberInit, displayValueParam);
        }

        public static Func<bool, dynamic> Creator;

        public static Func<bool, dynamic> BuildLambda2()
        {
            var expectedType = typeof(MyObject);
            var displayValueParam = Expression.Parameter(typeof(bool), "displayValue");
            var ctor = Expression.New(expectedType);
            var local = Expression.Parameter(expectedType, "obj");
            var displayValueProperty = Expression.Property(local, "DisplayValue");

            var returnTarget = Expression.Label(expectedType);
            var returnExpression = Expression.Return(returnTarget, local, expectedType);
            var returnLabel = Expression.Label(returnTarget, Expression.Default(expectedType));

            var block = Expression.Block(
                new[] { local },
                Expression.Assign(local, ctor),
                Expression.Assign(displayValueProperty, displayValueParam),
                /* I forgot to remove this line:
                 * Expression.Return(Expression.Label(expectedType), local, expectedType), 
                 * and now it works.
                 * */
                returnExpression,
                returnLabel
                );
            return
                Expression.Lambda<Func<bool, dynamic>>(block, displayValueParam)
                    .Compile();
        }
    }
}
