using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using ExpressionBuilder.Fluent;
using ExpressionBuilder.Operations;
using Newtonsoft.Json;

namespace ExpressionBuilder.ConsoleTest
{
    public class FilterProcessor
    {
        //public DynamicFunctionObject iobjFunctionObject { get; set; }
        public void ParseFilterFiles(object mainobj)
        {
            var idicParamStack = new Dictionary<string, object>();


            var lobjXDocument = XDocument.Load(@"..\..\newgetcontactInfo.xml");
            var lobjRootNode = lobjXDocument.Root;

            DynamicFunctionObject dfo =
                ParseFilterFiles(lobjRootNode.XPathSelectElements("/xFilter/xFilterGet").Elements());

            var fn = Function.Create()
                   .WithParameters(dfo.InputParams.ToArray())
                   .WithBody(CodeLine.Nop, dfo.FunctionBody.ToArray())
                   .Returns("data");


            var outputExpr = fn.ToExpression();
            var outputExprCompiled = outputExpr.Compile().DynamicInvoke(mainobj);
            var output = Newtonsoft.Json.JsonConvert.SerializeObject(outputExprCompiled, Formatting.Indented, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            Console.WriteLine(output);
            Console.ReadLine();

            //    var fn = Function.Create()
            //        .WithParameters(dfo.InputParams.ToArray())
            //        .WithBody(CodeLine.Nop, dfo.FunctionBody.ToArray())
            //        .Returns("data");


            //    var outputExpr = fn.ToExpression();
            //    var outputExprCompiled = outputExpr.Compile().DynamicInvoke(mainobj);
            //    var output = Newtonsoft.Json.JsonConvert.SerializeObject(outputExprCompiled, Formatting.Indented, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            //    Console.WriteLine(output);
            //    Console.ReadLine();
            //}
            //catch (Exception ex)
            //{
            //}

        }

        private DynamicFunctionObject ParseFilterFiles(IEnumerable<XElement> aobjElements)
        {
            DynamicFunctionObject dfo = new DynamicFunctionObject()
            {
                InputParams = new List<Variable>(),
                FunctionBody = new List<ICodeLine>(),// { CodeLine.CreateVariable(typeof(mDictionary), "data"), CodeLine.Assign("data", Operation.CreateInstance(typeof(mDictionary)))},//this dictionary will be used  by single and list nodes. Each and every property/value pair is pushed to this object before adding it to the parent dictionary.
                FunctionReturn = string.Empty
            };

            try
            {
                foreach (var lobjChildNode in aobjElements)
                {
                    switch (lobjChildNode.Name.LocalName)
                    {
                        case "input":
                            {
                                var codelines = ProcessInputNode(lobjChildNode);
                                dfo.InputParams.AddRange(codelines);
                            }
                            break;
                        case "body":
                            {
                                List<ICodeLine> llstCodeLines = new List<ICodeLine>();
                                string lstrContainerKey = lobjChildNode.Attribute("name")?.Value;
                                llstCodeLines.AddRange(CodeLine.CreateVariableAndInitialize<mDictionary>(lstrContainerKey));
                                ProcessNode(lobjChildNode, llstCodeLines, lstrContainerKey);
                                dfo.FunctionBody.AddRange(llstCodeLines);
                            }
                            break;
                        
                        default:
                            break;
                    }
                }

            }
            catch (Exception ex)
            {

            }
            return dfo;
        }


        private void ProcessNode(XElement aobjNode, List<ICodeLine> alstCodeLines, string astrContainer)
        {
            string lstrNodeType = aobjNode.Name.LocalName;
            foreach (var lobjChildNode in aobjNode.Elements())
            {
                switch (lobjChildNode.Name.LocalName)
                {
                    //case "assign":
                    //    {
                    //        ProcessSingleNode(aobjNode, alstCodeLines, astrContainer);
                    //    }
                    //    break;
                    case "single":
                        {
                            ProcessSingleNode(lobjChildNode, alstCodeLines, astrContainer);
                        }
                        break;
                    case "list":
                        {
                            ProcessListNode(lobjChildNode, alstCodeLines, astrContainer);
                        }
                        break;
                }
            }

        }

        private void ProcessSingleNode(XElement aobjSingleNode, List<ICodeLine> alstCodeLines, string astrContainer, List<ICodeLine> alstLoopCodeLines=null)
        {
            string lstrSingleObjContainerKey = aobjSingleNode.Attribute("name")?.Value;
          //  string _lstrSingleObjContainerKey = lstrSingleObjContainerKey;

            //string lstrLocalVariable = $"_{lstrSingleObjContainerKey}";
            //if(fromloop)
            //    alstCodeLines.Add(CodeLine.Assign(lstrSingleObjContainerKey, Operation.CreateInstance<mDictionary>()));
            //else
            if (alstLoopCodeLines != null)
            {
                alstLoopCodeLines.Add(CodeLine.CreateVariable<mDictionary>(lstrSingleObjContainerKey));
                alstCodeLines.Add(CodeLine.Assign(lstrSingleObjContainerKey, Operation.CreateInstance<mDictionary>()));
            }
            else
            {
                alstCodeLines.AddRange(CodeLine.CreateVariableAndInitialize<mDictionary>(lstrSingleObjContainerKey));
            }
            //alstCodeLines.Add(CodeLine.Assign(_lstrSingleObjContainerKey, Operation.CreateInstance<mDictionary>()));

            alstCodeLines.Add(AddToDictionary(astrContainer, lstrSingleObjContainerKey, lstrSingleObjContainerKey));

            foreach (var lobjElements in aobjSingleNode.Elements())
            {
                switch (lobjElements.Name.LocalName)
                {
                    case "assign":
                        {
                            ProcessAssignElement(lobjElements, alstCodeLines, lstrSingleObjContainerKey);
                        }
                        break;
                    case "single":
                        {
                            ProcessSingleNode(lobjElements, alstCodeLines, lstrSingleObjContainerKey, alstLoopCodeLines);
                        }
                        break;
                    case "list":
                        {
                               ProcessListNode(lobjElements, alstCodeLines, lstrSingleObjContainerKey);
                        }
                        break;
                }
            }
        }

        private void ProcessListNode(XElement aobjListNode, List<ICodeLine> alstCodeLines, string astrContainer)
        {
            string lstrListObjContainerKey = aobjListNode.Attribute("name")?.Value;
            string lstrLocalList = $"_lst_{lstrListObjContainerKey}";
            //list node container
            alstCodeLines.AddRange(CodeLine.CreateVariableAndInitialize<mDictionary>(lstrListObjContainerKey));

            alstCodeLines.AddRange(CodeLine.CreateVariableAndInitialize<List<object>>(lstrLocalList));
            //add list node container to the main container
            alstCodeLines.Add(AddToDictionary(astrContainer, lstrListObjContainerKey, lstrLocalList));
            
            //local list variable to hold local dictionary
            

            //local dictionary
            alstCodeLines.Add(AddToDictionary(lstrListObjContainerKey, lstrLocalList, lstrLocalList));

            List<ICodeLine> lobjEachBlockCodeLines = new List<ICodeLine>();
            lobjEachBlockCodeLines.Add(CodeLine.Assign(lstrListObjContainerKey, Operation.CreateInstance<mDictionary>()));
            lobjEachBlockCodeLines.Add(Operation.Invoke(Operation.Variable(lstrLocalList), "Add", Operation.Variable(lstrListObjContainerKey)));


            foreach (var lobjElements in aobjListNode.Elements())
            {
                switch (lobjElements.Name.LocalName)
                {
                    case "assign":
                        {
                            ProcessAssignElement(lobjElements, lobjEachBlockCodeLines, lstrListObjContainerKey);
                        }
                        break;
                    case "single":
                        {
                           // alstCodeLines.Add(CodeLine.CreateVariable<mDictionary>(lobjElements.Attribute("name")?.Value));
                            ProcessSingleNode(lobjElements, lobjEachBlockCodeLines, lstrListObjContainerKey, alstCodeLines);
                        }
                        break;
                    case "list":
                        {
                            //   ProcessListNode(aobjNode, alstCodeLines, astrContainer);
                        }
                        break;
                }
            }

            string lstrListPath = aobjListNode.Attribute("path").Value;
            string lstrLoopItemName = aobjListNode.Attribute("loopVariable").Value;

            alstCodeLines.Add(CodeLine.CreateForEach(lstrListPath, lstrLoopItemName).Each(CodeLine.Nop, lobjEachBlockCodeLines.ToArray()));


        }

        //private void ProcessListNode(XElement aobjListNode, string astrLocalVariable, List<ICodeLine> alstCodeLines)
        //{
        //    List<ICodeLine> llstCodeLines = new List<ICodeLine>();
        //    string lstrListObjectKey = aobjListNode.Attribute("name").Value;
        //    string lstrListPath = aobjListNode.Attribute("path").Value;
        //    string lstrLoopItemName = aobjListNode.Attribute("loopVariable").Value;
        //    List<ICodeLine> lobjEachBlockCodeLines = new List<ICodeLine>();
        //    // llstCodeLines.Add(CodeLine.CreateVariable(typeof(List<object>), "lst"));
        //    //llstCodeLines.Add(CodeLine.Assign("lst", Operation.CreateInstance(typeof(List<object>))));
        //    string lstrSingleObjContainerKey = aobjListNode.Attribute("name")?.Value;
        //    string lstrLocalVariable = lstrSingleObjContainerKey + "_0";

        //    lobjEachBlockCodeLines.AddRange(ProcessSingleNode(aobjListNode, astrLocalVariable));
        //    // lobjEachBlockCodeLines.Add(Operation.Invoke(Operation.Variable("lst"), "Add", Operation.Variable(lstrLocalVariable)));
        //    llstCodeLines.Add(CodeLine.CreateForEach(lstrListPath, lstrLoopItemName).Each(CodeLine.Nop, lobjEachBlockCodeLines.ToArray()));
        //    alstCodeLines.AddRange(llstCodeLines);
        //    //return llstCodeLines;
        //}

        //private List<ICodeLine> ProcessSingleNode(XElement aobjSingleNode, string astrParentNodeName, List<ICodeLine> alstCodeLines = null)
        //{
        //    string lstrSingleObjContainerKey = aobjSingleNode.Attribute("name")?.Value;
        //    string lstrLocalVariable = lstrSingleObjContainerKey + "_0";
        //    if (alstCodeLines == null)
        //    {
        //        alstCodeLines = new List<ICodeLine>();
        //    }
        //    alstCodeLines.Add(CodeLine.CreateVariable(typeof(mDictionary), lstrLocalVariable));
        //    //initialize the local dictionary if it is null. it will be null when the control comes for the first iteration. During next iteration, no need to initialize since it is already initialized in the first iteration
        //    alstCodeLines.Add(CodeLine.Assign(lstrLocalVariable, Operation.CreateInstance(typeof(mDictionary))));
        //    alstCodeLines.Add(CodeLine.CreateVariable(typeof(mDictionary), lstrSingleObjContainerKey));//create container dictionary
        //    alstCodeLines.Add(CodeLine.Assign(lstrSingleObjContainerKey, Operation.CreateInstance(typeof(mDictionary))));
        //    //initialize the container dictionary
        //    foreach (var lobjChildNode in aobjSingleNode.Elements())
        //    {
        //        switch (lobjChildNode.Name.LocalName)
        //        {
        //            case "assign":
        //                {
        //                    string lstrPath = lobjChildNode.Attribute("path").Value;
        //                    string lstrKey = lobjChildNode.Attribute("name").Value;
        //                    alstCodeLines.Add(ProcessAssignElement(lstrLocalVariable, lstrKey, lstrPath));
        //                }
        //                break;
        //            case "single":
        //                {
        //                    ProcessSingleNode(lobjChildNode, lstrLocalVariable, alstCodeLines);
        //                }
        //                break;
        //            case "list":
        //                {
        //                    ProcessListNode(lobjChildNode, lstrLocalVariable, alstCodeLines);
        //                }
        //                break;
        //        }
        //    }

        //    if (astrParentNodeName != null)
        //    {
        //        string lstrParentNodeName = astrParentNodeName;//astrParentNodeName == "data_0" ? "data" : astrParentNodeName;
        //        if (astrParentNodeName.EndsWith("data_0"))
        //            lstrParentNodeName = astrParentNodeName.Substring(0, astrParentNodeName.Length - 2);
        //        alstCodeLines.Add(ProcessAssignElement(lstrParentNodeName, lstrSingleObjContainerKey, lstrLocalVariable));
        //    }
        //    return alstCodeLines;
        //}

        //private void ProcessListNode(XElement aobjListNode, string astrLocalVariable, List<ICodeLine> alstCodeLines)
        //{
        //    List<ICodeLine> llstCodeLines = new List<ICodeLine>();
        //    string lstrListObjectKey = aobjListNode.Attribute("name").Value;
        //    string lstrListPath = aobjListNode.Attribute("path").Value;
        //    string lstrLoopItemName = aobjListNode.Attribute("loopVariable").Value;
        //    List<ICodeLine> lobjEachBlockCodeLines = new List<ICodeLine>();
        //    // llstCodeLines.Add(CodeLine.CreateVariable(typeof(List<object>), "lst"));
        //    //llstCodeLines.Add(CodeLine.Assign("lst", Operation.CreateInstance(typeof(List<object>))));
        //    string lstrSingleObjContainerKey = aobjListNode.Attribute("name")?.Value;
        //    string lstrLocalVariable = lstrSingleObjContainerKey + "_0";

        //    lobjEachBlockCodeLines.AddRange(ProcessSingleNode(aobjListNode, astrLocalVariable));
        //    // lobjEachBlockCodeLines.Add(Operation.Invoke(Operation.Variable("lst"), "Add", Operation.Variable(lstrLocalVariable)));
        //    llstCodeLines.Add(CodeLine.CreateForEach(lstrListPath, lstrLoopItemName).Each(CodeLine.Nop, lobjEachBlockCodeLines.ToArray()));
        //    alstCodeLines.AddRange(llstCodeLines);
        //    //return llstCodeLines;
        //}


        private List<Variable> ProcessInputNode(XElement lobjChildNode)
        {
            List<Variable> lobjInputVariables = new List<Variable>();
            foreach (var variable in lobjChildNode.Elements("variable"))
            {
                string variableName = variable.Attribute("name").Value;
                string aliasName = variable.Attribute("alias") == null ? variable.Attribute("name").Value : variable.Attribute("alias").Value;
                lobjInputVariables.Add(new Variable(Type.GetType(variableName), aliasName, false));
            }
            return lobjInputVariables;
        }

        private static ICodeLine ProcessAssignElement(string astrSourceDictionary, string astrKey, string astrValue)
        {
            return Operation.Invoke(Operation.Variable(astrSourceDictionary),
                 "SetVal", new OperationConst(astrKey), Operation.Get(astrValue));
        }


        private void ProcessAssignElement(XElement aobjNode, List<ICodeLine> alstCodeLinesBuilder, string astrContainer)
        {
            string lstrKey = aobjNode.Attribute("name").Value;
            string lstrValue = aobjNode.Attribute("path").Value;
            alstCodeLinesBuilder.Add(Operation.Invoke(Operation.Variable(astrContainer), "SetVal", new OperationConst(lstrKey), Operation.Get(lstrValue)));
        }

        private static ICodeLine AddToDictionary(string astrPayloadDictionaryKey, string astrKey, string astrValuePlaceHolder)
        {
            return Operation.Invoke(Operation.Variable(astrPayloadDictionaryKey),
                "SetVal", new OperationConst(astrKey), Operation.Variable(astrValuePlaceHolder));
        }

    }
}
