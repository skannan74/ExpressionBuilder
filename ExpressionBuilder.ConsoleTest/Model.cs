using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressionBuilder.Fluent;

namespace ExpressionBuilder.ConsoleTest
{
    public class mDictionary : Dictionary<string, object>
    {
        public void SetVal(string key, object val)
        {
            this[key] = val;
        }
    }

    public class MyObject
    {
        public bool DisplayValue { get; set; }
    }

    public class busMSSPerson
    {
        public busMSSPerson()
        {
         //   ibusPersonPrimaryPhone = new busPersonPrimaryPhone();
        //    ibusPersonAlternatePhone = new busPersonPrimaryPhone();
        }
        public busPersonPrimaryPhone ibusPersonPrimaryPhone { get; set; }
        public busPersonPrimaryPhone ibusPersonAlternatePhone { get; set; }

        public Collection<busPersonAddress> iclbAddress { get; set; }
        public int PersonID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public DateTime DOB { get; set; }

    }

    public class busPersonPrimaryPhone
    {
        public busPersonPrimaryPhone()
        {
            icdoPersonPhone = new cdoPersonPhone();
        }
        public cdoPersonPhone icdoPersonPhone { get; set; }
    }

    public class cdoPersonPhone
    {
        public string phone_number { get; set; }
    }

    public class busPersonAddress
    {
        public string Address1 { get; set; }
        public string Address2 { get; set; }

        public busMSSPerson ContactPerson { get; set; }

        public busMSSPerson Person { get; set; }
    }

    public class DynamicFunctionObject
    {
        //public List<Variable> InputParams { get; set; }
        public List<Variable> InputParams { get; set; }
        public List<ICodeLine> FunctionBody { get; set; }
        public string FunctionReturn { get; set; }
    }

}
