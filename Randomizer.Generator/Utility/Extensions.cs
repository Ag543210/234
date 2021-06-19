﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hjson;
using Randomizer.Generator.Core;

namespace Randomizer.Generator.Utility
{
    internal static class Extensions
    {
        #region Members
        private static readonly List<Tuple<Int32, String>> _romanNumerals = new()
        {
            { Tuple.Create(1000, "M") },
            { Tuple.Create(900, "CM") },
            { Tuple.Create(500, "D") },
            { Tuple.Create(400, "CD") },
            { Tuple.Create(100, "C") },
            { Tuple.Create(90, "XC") },
            { Tuple.Create(50, "L") },
            { Tuple.Create(40, "XL") },
            { Tuple.Create(10, "X") },
            { Tuple.Create(9,"IX") },
            { Tuple.Create(5, "V") },
            { Tuple.Create(4, "IV") },
            { Tuple.Create(1, "I") }
        };
        #endregion

        #region Char Extensions
        /// <summary>
        /// Determines if the character is a vowel
        /// </summary>
        public static Boolean IsVowel(this Char extended)
        {
            if ("aeiouAEIOU".Contains(extended)) return true;
            return false;
        }
        #endregion

        #region Int32 Extensions
        /// <summary>
        /// Adds the ordinal suffix to an <see cref="int"/>
        /// </summary>
        /// <param name="extended">The <see cref="int"/> to add the suffix to</param>
        /// <returns>The orginal with it's appropriate suffix</returns>
        /// <remarks>Currently only supports English</remarks>
        public static string ToOrdinal(this Int32 extended)
        {
            Int32 lastDigit = Convert.ToInt32(extended.ToString().Right(1));

            if (extended <= 0)
                return extended.ToString();

			if ((extended % 100).In(11, 12, 13))
				return extended + "th";			

			return lastDigit switch
			{
				1 => extended + "st",
				2 => extended + "nd",
				3 => extended + "rd",
				_ => extended + "th",
			};
		}

        /// <summary>
        /// Converts an <see cref="int"/> to its numeric word
        /// </summary>
        /// <param name="extended">The <see cref="int"/> to convert</param>
        /// <returns>The numeric word for the <see cref="int"/></returns>
        /// <remarks>Currently only supports English</remarks>
        public static string ToText(this Int32 extended, TextCases textCase = TextCases.Lower)
        {
            return ((Int64)extended).ToText(textCase);
        }

        /// <summary>
        /// Converts an <see cref="int"/> to its numeric word
        /// </summary>
        /// <param name="extended">The <see cref="int"/> to convert</param>
        /// <returns>The numeric word for the <see cref="int"/></returns>
        /// <remarks>Currently only supports English</remarks>
        public static string ToText(this Int64 extended, TextCases textCase = TextCases.Lower)
        {
            string ProcessValue(Int64 divisor, String valueDescriptor)
            {
                var value = new StringBuilder();
                var remainder = extended % divisor;

                value.Append($"{ToText(extended / divisor)} {valueDescriptor}");

                if (remainder != 0) value.Append($" {ToText(remainder)}");

                return value.ToString();
            }

            String value;

            if (extended < 0)
            {
                value = Convert.ToString("negative ") + ToText(-extended);
            }
            else if (extended == 0)
            {
                value = "zero";
            }
            else if (extended <= 19)
            {
                value = new string[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" }[extended - 1];
            }
            else if (extended <= 99)
            {
                var tens = new string[] { "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" }[extended / 10 - 2];
                value = $"{tens}{(extended % 10 == 0 ? String.Empty : ToText(extended % 10))}";
            }
            else if (extended <= 999)
            {
                value = ProcessValue(100, "hundred");
            }
            else if (extended <= 999999)
            {
                value = ProcessValue(1000, "thousand");
            }
            else if (extended <= 999999999)
            {
                value = ProcessValue(1000000, "million");                
            }
            else if (extended <= 999999999999)
            {
                value = ProcessValue(1000000000, "billion");
            }
            else if (extended <= 999999999999999)
            {
                value = ProcessValue(1000000000000, "trillion");
            }
            else if (extended <= 999999999999999999)
            {
                value = ProcessValue(1000000000000000, "quadrillion");
            }
            else
            {
                value = ProcessValue(1000000000000000000, "quintillion");
            }

            return value.ToCase(textCase);
        }

        /// <summary>
        /// Returns the Roman Numeral equivalent of the provided integer
        /// </summary>
        /// <remarks>
        /// Roman numerals only support numbers between 1 and 4000.  Numbers outside of this range are returned as Arabic numbers.
        /// </remarks>
        public static string ToRomanNumerals(this Int32 extended)
        {
            var value = new StringBuilder();
            var i = 0;
            var number = extended;

            if (extended < 1 || extended > 4000) return extended.ToString();

            while ((number > 0) && (i < _romanNumerals.Count))
            {
                var currentSymbol = _romanNumerals[i];
                if (currentSymbol.Item1 <= number)
                {
                    value.Append(currentSymbol.Item2);
                    number -= currentSymbol.Item1;
                }
                else
                    i++;
            }

            return value.ToString();
        }
        #endregion

        #region String Extensions
        /// <summary>
        /// Determines if the indeterminite article before the string should be A or An
        /// </summary>
        public static string AOrAn(this string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value[0].IsVowel()) return $"an {value}";
            return $"a {value}";
        }

        /// <summary>
        /// Returns the right portion of a string
        /// </summary>
        public static String Right(this String extended, Int32 characters)
        {
            if (extended.Length < characters) return extended;
            return extended[^characters..];
        }
        #endregion

        #region Generic Extensions
        /// <summary>
        /// Checks to see if the extended item is in the list
        /// </summary>
        public static Boolean In<T>(this T extended, params T[] list)
        {
            foreach (var item in list)
            {
                if (extended.Equals(item)) return true;
            }
            return false;
        }
        #endregion
    }
}
