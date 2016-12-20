using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ExpressionBuilder.Fluent;
using ExpressionBuilder.Parser;

namespace ExpressionBuilder.Operations
{
    public class OperationProperty : ILeftRightable
    {
        internal string Name;

        public OperationProperty(string name)
        {
            Name = name;
        }

        public string ToString(ParseContext context)
        {
            return Name;
        }


        public void PreParseExpression(ParseContext context)
        {
            var resultVar = context.GetVariable(Name);
            ParsedType = resultVar.DataType;
        }

        public Type ParsedType { get; private set; }

        public Expression ToExpression(ParseContext context)
        {
            if (Name.Contains("."))
            {
                string lstrSourceName = Name.Split('.')[0];
                var lobjSourceVariable = context.GetVariable(lstrSourceName);
                if (lobjSourceVariable == null)
                {
                    throw new Exception($"Variable {lstrSourceName} not found");
                }
                Expression lobjSourceExpression = lobjSourceVariable.Expression;
                return ToExpression(lobjSourceExpression, Name.Split('.').Skip(1).Aggregate((a, i) => a + "." + i));

            }

            return context.GetVariable(Name).Expression;
        }

        //private Expression ToExpression1(Expression aobjSourceExpression, string astrPropertyName)
        //{

        //    string[] parts = astrPropertyName.Split('.');

        //    int partsL = parts.Length;

        //    return (partsL > 1)
        //        ?
        //        Expression.PropertyOrField(
        //            ToExpression(
        //                aobjSourceExpression,
        //                parts.Take(partsL - 1)
        //                    .Aggregate((a, i) => a + "." + i)
        //            ),
        //            parts[partsL - 1])
        //        :
        //        Expression.PropertyOrField(aobjSourceExpression, astrPropertyName);

        //}

        private Expression ToExpression(Expression obj, string propertyName)
        {
            string[] parts = propertyName.Split(new char[] { '.' }, 2);
            Expression member = Expression.PropertyOrField(obj, parts[0]);

            if (parts.Length > 1)
                member = ToExpression(member, parts[1]);
            if (member.Type.IsGenericType)
                return Expression.Condition(Expression.Equal(obj, Expression.Constant(null)),
                 Expression.Default(member.Type), member);
            else
                return Expression.Condition(Expression.Equal(obj, Expression.Constant(null)),
                     Expression.Convert(Expression.Default(member.Type), typeof(object)), Expression.Convert(member, typeof(object)));


        }
    }
}
