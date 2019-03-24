using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Filtering.Expressions
{
    public class LiteralExpression : Expression<string>
    {
        private Regex _literalRegex;

        private static string Sanitize(string literal)
        {
            //TODO only * will be supported as regex feature
            // Options for search string:
            // "*"
            // "*map"
            // "map*"
            // "*map*"
            throw new NotImplementedException();
        }

        private LiteralExpression(Regex literalRegex)
        {
            _literalRegex = literalRegex;
        }

        public static LiteralExpression Create(string literal)
        {
            if (string.IsNullOrWhiteSpace(literal)) return null;
            literal = Sanitize(literal);

            return new LiteralExpression(new Regex(literal));
        }

        public string Value => _literalRegex.ToString();
        public Regex ValueAsRegex => _literalRegex;

    }
}
