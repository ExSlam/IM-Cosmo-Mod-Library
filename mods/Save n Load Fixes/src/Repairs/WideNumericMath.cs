using System;
using System.Collections.Generic;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Pure checked Int64 arithmetic used by A33. This type deliberately has no Unity,
    /// game, logging, or Harmony dependency so its boundary behavior can be executed in
    /// a small regression harness.
    /// </summary>
    internal static class WideNumericMath
    {
        internal static long Add(long left, long right)
        {
            return checked(left + right);
        }

        internal static long Subtract(long left, long right)
        {
            return checked(left - right);
        }

        internal static long Negate(long value)
        {
            return checked(-value);
        }

        internal static long Multiply(long left, long right)
        {
            return checked(left * right);
        }

        internal static long MultiplyAdd(long left, long right, long addend)
        {
            return Add(Multiply(left, right), addend);
        }

        internal static long Sum(IEnumerable<long> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            long result = 0L;
            foreach (long value in values)
            {
                result = Add(result, value);
            }

            return result;
        }

        /// <summary>
        /// Applies an exact rational coefficient and rounds midpoint ties to even. The
        /// audited concert coefficients (1/2, 3/4, 3/8, and 9/16) are exact IEEE-754
        /// Single values, so this avoids first narrowing a wide yen amount to Single.
        /// </summary>
        internal static long RoundRatioToEven(long value, int numerator, int denominator)
        {
            if (numerator < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numerator));
            }

            if (denominator <= 0 || numerator > denominator)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            decimal scaled = checked((decimal)value * (decimal)numerator) /
                (decimal)denominator;
            decimal rounded = Math.Round(scaled, 0, MidpointRounding.ToEven);
            return checked((long)rounded);
        }

        internal static long RoundDecimalProductToEven(long value, decimal coefficient)
        {
            decimal scaled = checked((decimal)value * coefficient);
            decimal rounded = Math.Round(scaled, 0, MidpointRounding.ToEven);
            return checked((long)rounded);
        }

        internal static int ToInt32Exact(long value)
        {
            return checked((int)value);
        }

        internal static int ClampToInt32(long value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value < int.MinValue)
            {
                return int.MinValue;
            }

            return (int)value;
        }
    }
}
