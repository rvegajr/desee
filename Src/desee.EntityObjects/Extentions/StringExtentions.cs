using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("desee.Tests")]

namespace desee.EntityObjects.Extentions
{
    internal static class StringExtentions
    {
        /// <summary>
        /// This will determine if the string this executed on contains a match in a wild card comma delimited list.  For example 
        /// "ThisIsAString".IsIn("*Is*") = true,  "ThisIsAString".IsIn("*String") = true
        /// </summary>
        /// <param name="stringToCheck"></param>
        /// <param name="wildCardCommaDelimited"></param>
        /// <param name="columnNameToCheck"></param>
        public static bool IsIn(this string stringToCheck, string wildCardCommaDelimited)
        {
            return stringToCheck.IsIn(wildCardCommaDelimited.Split(','));
        }
        public static bool IsIn(this string stringToCheck, string[] wildCardCommaDelimitedArray)
        {
            var isTrueCount = 0;
            var isFalseCount = 0;
            var isIn = false;
            var isAllColumns = (wildCardCommaDelimitedArray.Length>0 ? wildCardCommaDelimitedArray[0].StartsWith("-") : false);
            foreach (var _wildCardCommaDelimitedItem in wildCardCommaDelimitedArray)
            {
                var wildCardCommaDelimitedItem = _wildCardCommaDelimitedItem;
                var hasNotOp = wildCardCommaDelimitedItem.StartsWith("-");
                if (hasNotOp) wildCardCommaDelimitedItem = wildCardCommaDelimitedItem.Substring(1);
                isIn = Regex.IsMatch(stringToCheck, "^" + Regex.Escape(wildCardCommaDelimitedItem).Replace("\\?", ".").Replace("\\*", ".*") + "$");
                if (hasNotOp) isIn=!isIn;
                if (isIn) isTrueCount++;
                if (!isIn) isFalseCount++;
            }
            return (isFalseCount==0);
        }
    }
}