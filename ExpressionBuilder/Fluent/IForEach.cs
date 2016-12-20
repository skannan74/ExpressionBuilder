using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpressionBuilder.Fluent
{
    public interface IForEach : ICodeLine
    {
        ICodeLine Each(ICodeLine firstCodeLine, params ICodeLine[] codeLines);
    }
}
