using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.Extensions
{
    static class StringExtensions
    {


        public static int SafeParseInt32(this string s)
        {
            int result = 0;

            if (!string.IsNullOrWhiteSpace(s))
            {
                int.TryParse(s, out result);
            }

            return result;
        }

        public static long SafeParseInt64(this string s)
        {
            long result = 0;

            if (!string.IsNullOrWhiteSpace(s))
                long.TryParse(s, out result);

            return result;
        }

        public static bool SafeParseBool(this string s)
        {
            bool result = false;

            if (!string.IsNullOrWhiteSpace(s))
            {
                bool.TryParse(s, out result);
            }

            return result;
        }
    }
}
