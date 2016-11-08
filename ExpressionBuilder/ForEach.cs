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
    public class ForEach : IForEach, ICodeLine
    {
        internal List<ICodeLine> CodeLines; // Loop Content
        internal ILeftRightable CollectionSourceVar;
     //   internal IRightable CollectionSourceValue;
        internal ICodeLine LoopVar;


        internal ForEach(ILeftRightable collection)
        {
            if (collection == null) throw new ArgumentException();
            CollectionSourceVar = collection; // parameter expression
        //    CollectionSourceValue = Operation.Get((CollectionSourceVar as Operations.OperationVariable).Name);
                                              //LoopVar = loopvar;
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
            var result = "Foreach(var loopvar in " + CollectionSourceVar.ToString(context) + ")\n";
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
            CollectionSourceVar.PreParseExpression(context);
           
            LoopVar =
               // CodeLine.CreateVariable(CollectionSourceVar.ParsedType, "loopvar");
            CodeLine.CreateVariable(CollectionSourceVar.ParsedType.GetGenericArguments().Single(), "loopvar");
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
              
                loopContent.Add(expLine);
            }
            return ForEachExpr(collectionVarExpr, item, loopContent.ToArray(),context);



            //var collection = Expression.Parameter(typeof(List<string>), "collection");
            //var loopVar = Expression.Parameter(typeof(string), "loopVar");
            //var loopBody = Expression.Call(typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }), loopVar);
            //var loopContent1 = new List<Expression>();
            //loopContent1.Add(loopBody);
            //var loop = ForEachExpr(collection, loopVar, loopContent1.ToArray());
            //var compiled = Expression.Lambda<Action<List<string>>>(loop, collection).Compile();
            //var input = new List<string>() { "a", "b", "c" };
            //var res = compiled.DynamicInvoke(input);




        }


        //var enumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
            //var enumeratorVar = Expression.Variable(enumerableType, "enumerator");//typeof(IEnumerable<T>
            //var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));

            //var getEnumeratorCall = Expression.Call(collectionVarExpr, enumerableType.GetMethod("GetEnumerator"));
            //var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);
            //// The MoveNext method's actually on IEnumerator, not IEnumerator<T>
            //var assignCurrent = Expression.Assign(item, Expression.Property(enumeratorVar, "Current"));
            //LabelTarget breakLabel = Expression.Label(Guid.NewGuid().ToString());

            //var conditionExpression = Expression.Equal(moveNextCall, Expression.Constant(true));
            //// var breakLabel = Expression.Label("LoopBreak");
            
            //var loopContent = new List<Expression>();
            //var listOfThenVars = new List<ParameterExpression>();
            ////listOfThenVars.Add(enumeratorVar);
            ////listOfThenVars.Add(enumeratorAssign);
            //listOfThenVars.Add(item as ParameterExpression);
            //loopContent.Add(assignCurrent);
            //foreach (var line in CodeLines)
            //{
            //    var expLine = line.ToExpression(context);

            //    var createVariable = line as CreateVariable;
            //    if (createVariable != null)
            //    {
            //        listOfThenVars.Add((ParameterExpression)expLine);
            //        expLine = createVariable.DefaultInitialize(context);
            //    }
            //    loopContent.Add(expLine);
            //}


            //var loop = Expression.Block(new[] { enumeratorVar },
            //enumeratorAssign,
            //Expression.Loop(
            //    Expression.IfThenElse(
            //        conditionExpression,
            //        Expression.Block(listOfThenVars,loopContent.ToArray()),
            //        Expression.Break(breakLabel)
            //    ),
            //breakLabel)
            //);

            //return loop;

            //var thenBlock = Expression.Block(new[] {loopVar},
            //    Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
            //    loopContent
            //);


            //context.RemoveLevel();

            //LabelTarget breakLabel = Expression.Label(Guid.NewGuid().ToString());
            //var ifThenElse = Expression.IfThenElse(
            //                                                    conditionExpression,
            //                                                    thenBlock,
            //                                                    Expression.Break(label));


            //    var loop = Expression.Block(new[] { enumeratorVar },
            //enumeratorAssign,
            //Expression.Loop(
            //    Expression.IfThenElse(
            //        Expression.Equal(moveNextCall, Expression.Constant(true)),
            //        Expression.Block(new[] { loopVar },
            //            Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
            //            loopContent
            //        ),
            //        Expression.Break(breakLabel)
            //    ),
            //      //breakLabel)
            //   );

            //return loop;

           // return Expression.Loop(ifThenElse, label);
        //}

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

        //    public Expression ToExpression1(ParseContext context)
        //    {
        //        var collectionVarExpr = CollectionSourceVar.ToExpression(context);
        //        context.AddLevel();

        //        Type itemType = CollectionSourceVar.ParsedType.GetGenericArguments().Single();

        //        var item = LoopVar.ToExpression(context);

        //        var enumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
        //        var enumeratorVar = Expression.Variable(enumerableType, "enumerator");
        //        var getEnumeratorCall = Expression.Call(collectionVarExpr, enumerableType.GetMethod("GetEnumerator"));
        //        var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);
        //        // The MoveNext method's actually on IEnumerator, not IEnumerator<T>
        //        var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));
        //        var conditionExpression = Expression.Equal(moveNextCall, Expression.Constant(true));
        //        // var breakLabel = Expression.Label("LoopBreak");
        //        LabelTarget breakLabel = Expression.Label(Guid.NewGuid().ToString());



        //        var loopContent = new List<Expression>();
        //        var listOfThenVars = new List<ParameterExpression>();
        //        //listOfThenVars.Add(enumeratorVar);
        //        //listOfThenVars.Add(enumeratorAssign);
        //        listOfThenVars.Add(loopVar as ParameterExpression);
        //        loopContent.Add(Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")));
        //        foreach (var line in CodeLines)
        //        {
        //            var expLine = line.ToExpression(context);

        //            var createVariable = line as CreateVariable;
        //            if (createVariable != null)
        //            {
        //                listOfThenVars.Add((ParameterExpression)expLine);
        //                expLine = createVariable.DefaultInitialize(context);
        //            }
        //            loopContent.Add(expLine);
        //        }


        //        var loop = Expression.Block(new[] { enumeratorVar },
        //        enumeratorAssign,
        //        Expression.Loop(
        //            Expression.IfThenElse(
        //                conditionExpression,
        //                Expression.Block(listOfThenVars, loopContent.ToArray()),
        //                Expression.Break(breakLabel)
        //            ),
        //        breakLabel)
        //);

        //        return loop;

        //        //var thenBlock = Expression.Block(new[] {loopVar},
        //        //    Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
        //        //    loopContent
        //        //);


        //        //context.RemoveLevel();

        //        //LabelTarget breakLabel = Expression.Label(Guid.NewGuid().ToString());
        //        //var ifThenElse = Expression.IfThenElse(
        //        //                                                    conditionExpression,
        //        //                                                    thenBlock,
        //        //                                                    Expression.Break(label));


        //        //    var loop = Expression.Block(new[] { enumeratorVar },
        //        //enumeratorAssign,
        //        //Expression.Loop(
        //        //    Expression.IfThenElse(
        //        //        Expression.Equal(moveNextCall, Expression.Constant(true)),
        //        //        Expression.Block(new[] { loopVar },
        //        //            Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")),
        //        //            loopContent
        //        //        ),
        //        //        Expression.Break(breakLabel)
        //        //    ),
        //        //      //breakLabel)
        //        //   );

        //        //return loop;

        //        // return Expression.Loop(ifThenElse, label);
        //    }
    }
}
