using ExpressionBuilder.Fluent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using ExpressionBuilder.Operations;
using Newtonsoft.Json;

namespace ExpressionBuilder.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // var fn = Sample.BuildLambdaAndReturnDictionaryExpression();
            // mDictionary result = fn.Compile().DynamicInvoke() as mDictionary;


            var person = new busMSSPerson()
            {
                FirstName = "Sagitec",
                LastName = "Solutions",
                PersonID = 1,
                ibusPersonPrimaryPhone = new busPersonPrimaryPhone()
                {
                    icdoPersonPhone = new cdoPersonPhone()
                    {
                        phone_number = "12345678"
                    }
                },
                ilstAddresses = new Collection<busPersonAddress>()
                {
                    new busPersonAddress() {Address1 = "Address1Item1", Address2 = "Address2Item1" , Person = new busMSSPerson() {FirstName = "sample name"} },
                    new busPersonAddress() {Address1 = "Address1Item2", Address2 = "Address2Item2" , Person = new busMSSPerson() {FirstName = "sample name"} },
                    new busPersonAddress() {Address1 = "Address1Item3", Address2 = "Address2Item3" , Person = new busMSSPerson() {FirstName = "sample name"} },
                    new busPersonAddress() {Address1 = "Address1Item4", Address2 = "Address2Item4" , Person = new busMSSPerson() {FirstName = "sample name"} },
                    new busPersonAddress() {Address1 = "Address1Item5", Address2 = "Address2Item5" , Person = new busMSSPerson() {FirstName = "sample name"} },
                }


            };

            //ParseXMLAndCreateFunction(person, "getcontactinfo.xml");


            //var result1 = Sample.PropertySette1() as IExpressionResult;
            ////var expr = result1.ToExpression();
            ////var result2compiled = expr.Compile();//this goes to cache
            ////var result2 = result2compiled.DynamicInvoke(person);
            //var result3 = result1.ToLambda<Func<busMSSPerson, mDictionary>>()(person);
            //var str2 = Newtonsoft.Json.JsonConvert.SerializeObject(result3);


            //var looptestresult = Sample.ForEachSample() as IExpressionResult;
            //var loopexpstr = looptestresult.ToString();
            //var loopexpr = looptestresult.ToExpression();
            //var loopresult2 = loopexpr.Compile().DynamicInvoke(person);
            //var str5 = Newtonsoft.Json.JsonConvert.SerializeObject(loopresult2);

            ParseXMLAndCreateFunction(person, "newGetContactInfo.xml");
        }

        protected static void ParseXMLAndCreateFunction(object mainobj, string xmlfile)
        {
            var idicParamStack = new Dictionary<string, object>();


            var lobjXDocument = XDocument.Load(@"..\..\"+xmlfile);
            var lobjRootNode = lobjXDocument.Root;

            DynamicFunctionObject dfo = new DynamicFunctionObject()
            {
                InputParams = new List<Variable>(),
                FunctionBody = new List<ICodeLine>()
            };
            try
            {
                foreach (var lobjChildNode in lobjRootNode.XPathSelectElements("/xFilter/xFilterGet").Elements())
                {
                    switch (lobjChildNode.Name.LocalName)
                    {
                        case "input":
                        {
                            CreateInputVariables(dfo, lobjChildNode);
                        }
                            break;

                        //case "output":
                        //    {
                        //        string astrRetVal = "data";
                        //        dfo.InputParams.Add(new Variable(typeof(mDictionary), astrRetVal));
                        //        dfo.FunctionReturn = astrRetVal;
                        //    }
                        //    break;
                        case "single":
                        {
                            ProcessSingleElement(dfo, lobjChildNode);
                        }

                            break;
                        case "list":
                        {
                                ProcessListElement(dfo, lobjChildNode);
                            }
                            break;
                        default:
                            break;
                    }
                }

                var fn = Function.Create()
                    .WithParameters(dfo.InputParams.ToArray())
                    .WithBody(CodeLine.Nop, dfo.FunctionBody.ToArray())
                    .Returns("data");


                var outputExpr = fn.ToExpression();
                var outputExprCompiled = outputExpr.Compile().DynamicInvoke(mainobj);
                var output = Newtonsoft.Json.JsonConvert.SerializeObject(outputExprCompiled,Formatting.Indented);
                Console.WriteLine(output);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
            }

        }

        private static void ProcessListElement(DynamicFunctionObject dfo, XElement aobjListChildNode)
        {
            string lstrListObjectKey = aobjListChildNode.Attribute("name").Value;
            string lstrListPath = aobjListChildNode.Attribute("path").Value;
            string lstrLoopItemName = aobjListChildNode.Attribute("loopVariable").Value;
        }

        private static void ProcessSingleElement(DynamicFunctionObject dfo, XElement aobjSingleChildNode)
        {
           // List<ICodeLine> llstSingleElementBody = new List<ICodeLine>();
            string lstrSingleObjectKey = aobjSingleChildNode.Attribute("name").Value;
            //dfo.FunctionBody.Add(CodeLine.Assign(lstrSingleObjectKey, Operation.CreateInstance(typeof(mDictionary))));
            dfo.FunctionBody.Add(CodeLine.Assign("localvar", Operation.CreateInstance(typeof(mDictionary)))); //temporary variable to hold the keyvalue paid within the loop. 

            foreach (var lobjChildNode in aobjSingleChildNode.Elements())
            {
                switch (lobjChildNode.Name.LocalName)
                {
                    case "assign":
                        {
                            string lstrPath = lobjChildNode.Attribute("path").Value;
                            string lstrKey = lobjChildNode.Attribute("name").Value;
                            dfo.FunctionBody.Add(Operation.Invoke(Operation.Variable("localvar"),
                                "SetVal", new OperationConst(lstrKey), Operation.Get(lstrPath)));
                        }
                        break;
                    case "single":
                        {
                            ProcessSingleElement(dfo, lobjChildNode);
                            
                        }
                        break;
                }
            }
           // if (firstIteration)
            {
                ICodeLine lobjCodeLine = Operation.Invoke(Operation.Variable("data"),
                               "SetVal", new OperationConst(lstrSingleObjectKey), Operation.Variable("localvar"));
                dfo.FunctionBody.Add(lobjCodeLine);
            }
        }

        private static void CreateInputVariables(DynamicFunctionObject functionExpression, XElement lobjChildNode)
        {
            foreach (var variable in lobjChildNode.Elements("variable"))
            {
                string variableName = variable.Attribute("name").Value;
                string aliasName = variable.Attribute("alias") == null ? variable.Attribute("name").Value : variable.Attribute("alias").Value;
                functionExpression.InputParams.Add(new Variable(Type.GetType(variableName), aliasName, false));

                
            }
            //create output variable.
            functionExpression.FunctionBody.Add(CodeLine.CreateVariable(typeof(mDictionary), "data"));
            functionExpression.FunctionBody.Add(CodeLine.Assign("data",
                Operation.CreateInstance(typeof(mDictionary))));
            functionExpression.FunctionBody.Add(CodeLine.CreateVariable(typeof(mDictionary), "localvar")); // this is local variable of type dictionary (instance is created in single and list block) where the values in single and loop will be filled in and pushed to the single dictionary


        }
    }




    public class mObject
    {
        List<KeyValuePair<string, object>> duplicatedictionary = new List<KeyValuePair<string, object>>();

        public void SetVal(string key, object val)
        {
            KeyValuePair<string, object> myItem = new KeyValuePair<string, object>(key, val);
            duplicatedictionary.Add(myItem);
        }
    }

    class DynamicFunctionObject
    {
        //public List<Variable> InputParams { get; set; }
        public List<Variable> InputParams { get; set; }
        public List<ICodeLine> FunctionBody { get; set; }
        public string FunctionReturn { get; set; }
    }
}

