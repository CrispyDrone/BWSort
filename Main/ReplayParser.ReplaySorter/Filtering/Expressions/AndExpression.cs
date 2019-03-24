using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Filtering.Expressions
{
    public class AndExpression<T>
    {
        public Expression<T> Left;
        public Expression<T> Right;
    }
}