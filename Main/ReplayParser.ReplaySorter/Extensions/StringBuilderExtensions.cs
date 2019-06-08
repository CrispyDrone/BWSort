using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Extensions
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder TryAddSingleSpace(this StringBuilder stringBuilder)
        {
            if (!char.IsWhiteSpace(GetLastCharacter(stringBuilder)))
                stringBuilder.Append(' ');

            return stringBuilder;
        }

        public static char GetLastCharacter(this StringBuilder stringBuilder)
        {
            var length = stringBuilder.Length;
            if (length == 0)
                return char.MinValue;

            return stringBuilder[length - 1];
        }
    }
}
