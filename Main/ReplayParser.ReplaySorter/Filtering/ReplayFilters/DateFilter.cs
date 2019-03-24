using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Filtering.ReplayFilters
{
    public class DateFilter : ComplexFilter<File<IReplay>>
    {
        public override IQueryable<File<IReplay>> Execute(IList<File<IReplay>> list)
        {
            throw new NotImplementedException();
        }
    }
}
