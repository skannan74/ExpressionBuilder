using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ExpressionBuilder.CodeLines;
using ExpressionBuilder.Fluent;
using ExpressionBuilder.Parser;

namespace ExpressionBuilder
{
    public class ForEach : IForEach, ICodeLine
    {
        internal List<ICodeLine> CodeLines; // Loop Content
        internal ILeftRightable Collection;
        internal ICodeLine LoopVar;


        internal ForEach(ILeftRightable collection)
        {
            if (collection == null) throw new ArgumentException();
            Collection = collection;
            LoopVar = CodeLine.CreateVariable(collection.ParsedType.GetGenericArguments().Single(), "Item");
            CodeLines = new List<ICodeLine>();
        }

        public ICodeLine Each(ICodeLine firstCodeLine, params ICodeLine[] codeLines)
        {
            CodeLines.Add(firstCodeLine);
            foreach (var codeLine in codeLines)
            {
                CodeLines.Add(codeLine);
            }
            return this;
        }


        public string ToString(ParseContext context)
        {
            var result = "Foreach(var item in " + Collection.ToString(context) + ")\n";
            result += context.Pad + "{\n";
            context.AddLevel();

            foreach (var line in CodeLines)
            {
                var createVariable = line as CreateVariable;
                if (createVariable != null)
                {
                    createVariable.DefaultInitialize(context);
                }
                result += context.Pad + line.ToString(context) + ";\n";
            }

            context.RemoveLevel();
            result += context.Pad + "}";
            return result;
        }

        public void PreParseExpression(ParseContext context)
        {
            //var pl = context.Current;
            Collection.PreParseExpression(context);
            context.AddLevel();

            foreach (var line in CodeLines)
            {
                line.PreParseExpression(context);
            }

            context.RemoveLevel();
        }

        public Type ParsedType { get; private set; }

        public Expression ToExpression(ParseContext context)
        {
            var conditionExpression = Condition.ToExpression(context);
            context.AddLevel();

            var thenLine = new List<Expression>();
            var listOfThenVars = new List<ParameterExpression>();
            foreach (var line in CodeLines)
            {
                var expLine = line.ToExpression(context);

                var createVariable = line as CreateVariable;
                if (createVariable != null)
                {
                    listOfThenVars.Add((ParameterExpression)expLine);
                    expLine = createVariable.DefaultInitialize(context);
                }
                thenLine.Add(expLine);
            }
            var thenBlock = Expression.Block(listOfThenVars.ToArray(), thenLine);

            context.RemoveLevel();

            LabelTarget label = Expression.Label(Guid.NewGuid().ToString());
            var ifThenElse = Expression.IfThenElse(
                                                                conditionExpression,
                                                                thenBlock,
                                                                Expression.Break(label));
            return Expression.Loop(ifThenElse, label);
        }
    }
}
