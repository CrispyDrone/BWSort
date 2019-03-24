using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Filtering.Expressions;
using ReplayParser.ReplaySorter.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Filtering.ReplayFilters
{
    public class MapFilter : ComplexFilter<File<IReplay>>
    {
        private MapExpression _mapExpression;

        public MapFilter(MapExpression mapExpression)
        {
            _mapExpression = mapExpression;
        }

        public override IQueryable<File<IReplay>> Execute(IList<File<IReplay>> list)
        {
            throw new NotImplementedException();
        }
    }
}
