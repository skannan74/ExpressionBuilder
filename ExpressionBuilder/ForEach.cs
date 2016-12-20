using System;
using System.Collections;
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
    public class ForEach : IForEach
    {
        internal List<ICodeLine> CodeLines; // Loop Content
        internal IRightable CollectionSourceVar; // Source collection
        //   internal IRightable CollectionSourceValue;
        internal ICodeLine LoopVar;
        internal string LoopVarName;

        internal ForEach(IRightable collection,string loopVariable)
        {
            if (collection == null) throw new ArgumentException();
            CollectionSourceVar = collection;
            LoopVarName = loopVariable;
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
            var result = $"Foreach(var {LoopVarName} in {CollectionSourceVar.ToString(context)})\n";
            result += context.Pad + "{\n";
            context.AddLevel();

            foreach (var line in CodeLines)
            {
                var createVariable = line as CreateVariable;
                createVariable?.DefaultInitialize(context);
                result += context.Pad + line.ToString(context) + ";\n";
            }

            context.RemoveLevel();
            result += context.Pad + "}";
            return result;
        }

        public void PreParseExpression(ParseContext context)
        {
            //var pl = context.Current;
            CollectionSourceVar.PreParseExpression(context);
           
            LoopVar =
               // CodeLine.CreateVariable(CollectionSourceVar.ParsedType, "loopvar");
            CodeLine.CreateVariable(CollectionSourceVar.ParsedType.GetGenericArguments().Single(), LoopVarName);
            context.AddLevel();
            LoopVar.PreParseExpression(context);
            foreach (var line in CodeLines)
            {
                line.PreParseExpression(context);
            }

            context.RemoveLevel();
        }

        public Type ParsedType { get; private set; }

        public Expression ToExpression(ParseContext context)
        {
            var collectionVarExpr = CollectionSourceVar.ToExpression(context); //CollectionExpr
            context.AddLevel();

            //Type itemType = CollectionSourceVar.ParsedType.GetGenericArguments().Single();
            var item = LoopVar.ToExpression(context) as ParameterExpression;
            var loopContent = new List<Expression>();
            foreach (var line in CodeLines)
            {
                var expLine = line.ToExpression(context);

                var createVariable = line as CreateVariable;
                if (createVariable != null)
                {
                    //listOfThenVars.Add((ParameterExpression)expLine);
                    expLine = createVariable.DefaultInitialize(context);
                }

                loopContent.Add(expLine);
            }
            return ForEachExpr(collectionVarExpr, item, loopContent.ToArray(),context);
        }


        private  Expression ForEachExpr(Expression collection, ParameterExpression loopVar, Expression[] loopContent, ParseContext context)
        {

          //  var collectionValueAssign = Expression.Assign(collection,CollectionSourceValue.ToExpression(context));
          //  var sourceexpr = context.GetVariable("source.ilstAddresses").Expression as ParameterExpression;
            var elementType = loopVar.Type;
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

            var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
            var getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod("GetEnumerator"));
            var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

            // The MoveNext method's actually on IEnumerator, not IEnumerator<T>
            var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));

            var breakLabel = Expression.Label("LoopBreak");

            List<Expression> contentexpr = new List<Expression>();
            contentexpr.Add(Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")));
            contentexpr.AddRange(loopContent);
            var loop = Expression.Block(new[] {  enumeratorVar },
                enumeratorAssign
                , Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Equal(moveNextCall, Expression.Constant(true)),
                        Expression.Block(new[] { loopVar },
                            contentexpr
                        ),
                        Expression.Break(breakLabel)
                    ),
                breakLabel)
            );

            return loop;
        }

     
    }
}
