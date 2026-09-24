using System;
using System.Collections.Generic;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Pure checked Int64 arithmetic used by A33. This type deliberately has no Unity,
    /// game, logging, Harmony, or optional framework-assembly dependency so its boundary
    /// behavior can execute inside Idol Manager's Unity/Mono profile.
    /// </summary>
    internal static class WideNumericMath
    {
        /// <summary>
        /// Minimal signed arbitrary-precision integer used only for exact intermediate
        /// arithmetic. Idol Manager's Unity/Mono profile does not reliably provide
        /// System.Numerics.dll, so A33 must not depend on BigInteger at runtime.
        /// Magnitudes use little-endian base-2^32 words; operations intentionally favor
        /// simple, auditable correctness over throughput because these values are tiny
        /// compared with general-purpose bignum workloads.
        /// </summary>
        private sealed class ExactInteger
        {
            private readonly int sign;
            private readonly uint[] words;

            internal static readonly ExactInteger Zero =
                new ExactInteger(0, new uint[0], false);
            internal static readonly ExactInteger One =
                new ExactInteger(1, new uint[] { 1U }, false);

            private ExactInteger(int valueSign, uint[] magnitude, bool normalize)
            {
                uint[] normalized = normalize ? Normalize(magnitude) : magnitude;
                if (normalized == null || normalized.Length == 0)
                {
                    sign = 0;
                    words = new uint[0];
                    return;
                }

                sign = valueSign < 0 ? -1 : 1;
                words = normalized;
            }

            internal int Sign { get { return sign; } }
            internal bool IsZero { get { return sign == 0; } }
            internal bool IsEven
            {
                get { return sign == 0 || (words[0] & 1U) == 0U; }
            }

            internal static ExactInteger FromInt64(long value)
            {
                if (value == 0L)
                {
                    return Zero;
                }

                bool negative = value < 0L;
                ulong magnitude = negative
                    ? unchecked((ulong)(-(value + 1L))) + 1UL
                    : (ulong)value;
                return FromUInt64(magnitude, negative ? -1 : 1);
            }

            internal static ExactInteger FromUInt32(uint value)
            {
                return value == 0U
                    ? Zero
                    : new ExactInteger(1, new uint[] { value }, false);
            }

            private static ExactInteger FromUInt64(ulong value, int valueSign)
            {
                if (value == 0UL)
                {
                    return Zero;
                }

                uint low = (uint)value;
                uint high = (uint)(value >> 32);
                return high == 0U
                    ? new ExactInteger(valueSign, new uint[] { low }, false)
                    : new ExactInteger(valueSign, new uint[] { low, high }, false);
            }

            internal static ExactInteger Abs(ExactInteger value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                return value.sign < 0
                    ? new ExactInteger(1, value.words, false)
                    : value;
            }

            internal static ExactInteger Negate(ExactInteger value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                return value.sign == 0
                    ? Zero
                    : new ExactInteger(-value.sign, value.words, false);
            }

            internal static ExactInteger Add(ExactInteger left, ExactInteger right)
            {
                if (left == null)
                {
                    throw new ArgumentNullException(nameof(left));
                }
                if (right == null)
                {
                    throw new ArgumentNullException(nameof(right));
                }
                if (left.sign == 0)
                {
                    return right;
                }
                if (right.sign == 0)
                {
                    return left;
                }

                if (left.sign == right.sign)
                {
                    return new ExactInteger(
                        left.sign,
                        AddMagnitude(left.words, right.words),
                        false);
                }

                int comparison = CompareMagnitude(left.words, right.words);
                if (comparison == 0)
                {
                    return Zero;
                }
                if (comparison > 0)
                {
                    return new ExactInteger(
                        left.sign,
                        SubtractMagnitude(left.words, right.words),
                        true);
                }
                return new ExactInteger(
                    right.sign,
                    SubtractMagnitude(right.words, left.words),
                    true);
            }

            internal static ExactInteger Subtract(ExactInteger left, ExactInteger right)
            {
                return Add(left, Negate(right));
            }

            internal static ExactInteger Multiply(ExactInteger left, ExactInteger right)
            {
                if (left == null)
                {
                    throw new ArgumentNullException(nameof(left));
                }
                if (right == null)
                {
                    throw new ArgumentNullException(nameof(right));
                }
                if (left.sign == 0 || right.sign == 0)
                {
                    return Zero;
                }

                uint[] result = new uint[left.words.Length + right.words.Length];
                for (int leftIndex = 0; leftIndex < left.words.Length; leftIndex++)
                {
                    ulong carry = 0UL;
                    for (int rightIndex = 0; rightIndex < right.words.Length; rightIndex++)
                    {
                        int resultIndex = leftIndex + rightIndex;
                        ulong current =
                            (ulong)left.words[leftIndex] * (ulong)right.words[rightIndex] +
                            (ulong)result[resultIndex] + carry;
                        result[resultIndex] = (uint)current;
                        carry = current >> 32;
                    }

                    int carryIndex = leftIndex + right.words.Length;
                    while (carry != 0UL)
                    {
                        ulong current = (ulong)result[carryIndex] + carry;
                        result[carryIndex] = (uint)current;
                        carry = current >> 32;
                        carryIndex++;
                    }
                }

                return new ExactInteger(left.sign * right.sign, result, true);
            }

            internal static ExactInteger ShiftLeft(ExactInteger value, int bitCount)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (bitCount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(bitCount));
                }
                if (value.sign == 0 || bitCount == 0)
                {
                    return value;
                }

                int wordShift = bitCount / 32;
                int intraWordShift = bitCount % 32;
                uint[] result = new uint[
                    value.words.Length + wordShift + (intraWordShift == 0 ? 0 : 1)];

                ulong carry = 0UL;
                for (int index = 0; index < value.words.Length; index++)
                {
                    ulong current = ((ulong)value.words[index] << intraWordShift) | carry;
                    result[index + wordShift] = (uint)current;
                    carry = intraWordShift == 0 ? 0UL : current >> 32;
                }

                if (intraWordShift != 0 && carry != 0UL)
                {
                    result[value.words.Length + wordShift] = (uint)carry;
                }

                return new ExactInteger(value.sign, result, true);
            }

            internal static int Compare(ExactInteger left, ExactInteger right)
            {
                if (left == null)
                {
                    throw new ArgumentNullException(nameof(left));
                }
                if (right == null)
                {
                    throw new ArgumentNullException(nameof(right));
                }
                if (left.sign != right.sign)
                {
                    return left.sign < right.sign ? -1 : 1;
                }
                if (left.sign == 0)
                {
                    return 0;
                }

                int magnitude = CompareMagnitude(left.words, right.words);
                return left.sign > 0 ? magnitude : -magnitude;
            }

            internal static void DivRem(
                ExactInteger dividend,
                ExactInteger divisor,
                out ExactInteger quotient,
                out ExactInteger remainder)
            {
                if (dividend == null)
                {
                    throw new ArgumentNullException(nameof(dividend));
                }
                if (divisor == null)
                {
                    throw new ArgumentNullException(nameof(divisor));
                }
                if (divisor.sign == 0)
                {
                    throw new DivideByZeroException();
                }
                if (dividend.sign == 0)
                {
                    quotient = Zero;
                    remainder = Zero;
                    return;
                }

                ExactInteger absoluteDividend = Abs(dividend);
                ExactInteger absoluteDivisor = Abs(divisor);
                int comparison = CompareMagnitude(
                    absoluteDividend.words,
                    absoluteDivisor.words);
                if (comparison < 0)
                {
                    quotient = Zero;
                    remainder = dividend;
                    return;
                }
                if (comparison == 0)
                {
                    quotient = dividend.sign == divisor.sign ? One : Negate(One);
                    remainder = Zero;
                    return;
                }

                int bitLength = GetBitLength(absoluteDividend.words);
                uint[] quotientWords = new uint[(bitLength + 31) / 32];
                ExactInteger runningRemainder = Zero;

                for (int bitIndex = bitLength - 1; bitIndex >= 0; bitIndex--)
                {
                    runningRemainder = ShiftLeft(runningRemainder, 1);
                    if (GetMagnitudeBit(absoluteDividend.words, bitIndex))
                    {
                        runningRemainder = Add(runningRemainder, One);
                    }

                    if (CompareMagnitude(
                            runningRemainder.words,
                            absoluteDivisor.words) >= 0)
                    {
                        runningRemainder = new ExactInteger(
                            1,
                            SubtractMagnitude(
                                runningRemainder.words,
                                absoluteDivisor.words),
                            true);
                        int wordIndex = bitIndex / 32;
                        int bitWithinWord = bitIndex % 32;
                        quotientWords[wordIndex] |= 1U << bitWithinWord;
                    }
                }

                quotient = new ExactInteger(
                    dividend.sign == divisor.sign ? 1 : -1,
                    quotientWords,
                    true);
                remainder = runningRemainder.sign == 0 || dividend.sign > 0
                    ? runningRemainder
                    : Negate(runningRemainder);
            }

            internal bool TryToInt64(out long value)
            {
                value = 0L;
                if (sign == 0)
                {
                    return true;
                }
                if (words.Length > 2)
                {
                    return false;
                }

                ulong magnitude = words.Length == 1
                    ? (ulong)words[0]
                    : (ulong)words[0] | ((ulong)words[1] << 32);

                if (sign > 0)
                {
                    if (magnitude > (ulong)long.MaxValue)
                    {
                        return false;
                    }
                    value = (long)magnitude;
                    return true;
                }

                const ulong MinMagnitude = 0x8000000000000000UL;
                if (magnitude > MinMagnitude)
                {
                    return false;
                }
                if (magnitude == MinMagnitude)
                {
                    value = long.MinValue;
                    return true;
                }

                value = -(long)magnitude;
                return true;
            }

            private static uint[] Normalize(uint[] magnitude)
            {
                if (magnitude == null || magnitude.Length == 0)
                {
                    return new uint[0];
                }

                int length = magnitude.Length;
                while (length > 0 && magnitude[length - 1] == 0U)
                {
                    length--;
                }
                if (length == 0)
                {
                    return new uint[0];
                }
                if (length == magnitude.Length)
                {
                    return magnitude;
                }

                uint[] result = new uint[length];
                Array.Copy(magnitude, result, length);
                return result;
            }

            private static int CompareMagnitude(uint[] left, uint[] right)
            {
                int leftLength = EffectiveLength(left);
                int rightLength = EffectiveLength(right);
                if (leftLength != rightLength)
                {
                    return leftLength < rightLength ? -1 : 1;
                }

                for (int index = leftLength - 1; index >= 0; index--)
                {
                    if (left[index] == right[index])
                    {
                        continue;
                    }
                    return left[index] < right[index] ? -1 : 1;
                }
                return 0;
            }

            private static uint[] AddMagnitude(uint[] left, uint[] right)
            {
                int length = Math.Max(left.Length, right.Length);
                uint[] result = new uint[length + 1];
                ulong carry = 0UL;
                for (int index = 0; index < length; index++)
                {
                    ulong leftWord = index < left.Length ? left[index] : 0UL;
                    ulong rightWord = index < right.Length ? right[index] : 0UL;
                    ulong current = leftWord + rightWord + carry;
                    result[index] = (uint)current;
                    carry = current >> 32;
                }
                if (carry != 0UL)
                {
                    result[length] = (uint)carry;
                }
                return Normalize(result);
            }

            private static uint[] SubtractMagnitude(uint[] larger, uint[] smaller)
            {
                uint[] result = new uint[larger.Length];
                ulong borrow = 0UL;
                for (int index = 0; index < larger.Length; index++)
                {
                    ulong largerWord = larger[index];
                    ulong smallerWord = index < smaller.Length ? smaller[index] : 0UL;
                    ulong subtrahend = smallerWord + borrow;
                    bool nextBorrow = largerWord < subtrahend ||
                        (borrow != 0UL && subtrahend == 0UL);
                    result[index] = unchecked((uint)(largerWord - subtrahend));
                    borrow = nextBorrow ? 1UL : 0UL;
                }

                if (borrow != 0UL)
                {
                    throw new InvalidOperationException(
                        "ExactInteger magnitude subtraction underflowed.");
                }
                return Normalize(result);
            }

            private static int EffectiveLength(uint[] magnitude)
            {
                if (magnitude == null)
                {
                    return 0;
                }
                int length = magnitude.Length;
                while (length > 0 && magnitude[length - 1] == 0U)
                {
                    length--;
                }
                return length;
            }

            private static int GetBitLength(uint[] magnitude)
            {
                int length = EffectiveLength(magnitude);
                if (length == 0)
                {
                    return 0;
                }

                uint high = magnitude[length - 1];
                int highBits = 0;
                while (high != 0U)
                {
                    highBits++;
                    high >>= 1;
                }
                return (length - 1) * 32 + highBits;
            }

            private static bool GetMagnitudeBit(uint[] magnitude, int bitIndex)
            {
                int wordIndex = bitIndex / 32;
                int bitWithinWord = bitIndex % 32;
                return wordIndex >= 0 && wordIndex < magnitude.Length &&
                    (magnitude[wordIndex] & (1U << bitWithinWord)) != 0U;
            }
        }

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

        internal static long DivideRoundToEven(long numerator, long denominator)
        {
            if (denominator <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            return DivideRoundToEven(
                ExactInteger.FromInt64(numerator),
                ExactInteger.FromInt64(denominator));
        }

        /// <summary>
        /// Applies the exact IEEE-754 value of every supplied Single and rounds the
        /// final rational result to the nearest integer with ties to even. Gameplay
        /// remains Int64; ExactInteger is used only as a non-observable intermediate so
        /// an in-range result cannot overflow before division and no optional framework
        /// assembly is required by the Unity/Mono runtime.
        /// </summary>
        internal static long RoundSingleProductToEven(long value, params float[] coefficients)
        {
            ExactInteger numerator = ExactInteger.FromInt64(value);
            ExactInteger denominator = ExactInteger.One;

            if (coefficients == null)
            {
                throw new ArgumentNullException(nameof(coefficients));
            }

            for (int index = 0; index < coefficients.Length; index++)
            {
                ExactInteger coefficientNumerator;
                ExactInteger coefficientDenominator;
                DecomposeSingle(
                    coefficients[index],
                    out coefficientNumerator,
                    out coefficientDenominator);
                numerator = ExactInteger.Multiply(numerator, coefficientNumerator);
                denominator = ExactInteger.Multiply(denominator, coefficientDenominator);
            }

            return DivideRoundToEven(numerator, denominator);
        }

        internal static long RoundRatioWithSingleProductsToEven(
            long value,
            long ratioDenominator,
            params float[] coefficients)
        {
            if (ratioDenominator <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(ratioDenominator));
            }
            ExactInteger numerator = ExactInteger.FromInt64(value);
            ExactInteger denominator = ExactInteger.FromInt64(ratioDenominator);
            ApplySingleCoefficients(coefficients, ref numerator, ref denominator);
            return DivideRoundToEven(numerator, denominator);
        }

        internal static long RoundProductRatioWithSingleProductsToEven(
            long left,
            long right,
            long ratioDenominator,
            params float[] coefficients)
        {
            if (ratioDenominator <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(ratioDenominator));
            }
            ExactInteger numerator = ExactInteger.Multiply(
                ExactInteger.FromInt64(left),
                ExactInteger.FromInt64(right));
            ExactInteger denominator = ExactInteger.FromInt64(ratioDenominator);
            ApplySingleCoefficients(coefficients, ref numerator, ref denominator);
            return DivideRoundToEven(numerator, denominator);
        }

        internal static long RoundLinearCombinationWithSingleProductsToEven(
            long first,
            long firstFactor,
            long second,
            long secondFactor,
            long denominatorValue,
            params float[] coefficients)
        {
            if (denominatorValue <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(denominatorValue));
            }
            ExactInteger numerator = ExactInteger.Add(
                ExactInteger.Multiply(
                    ExactInteger.FromInt64(first),
                    ExactInteger.FromInt64(firstFactor)),
                ExactInteger.Multiply(
                    ExactInteger.FromInt64(second),
                    ExactInteger.FromInt64(secondFactor)));
            ExactInteger denominator = ExactInteger.FromInt64(denominatorValue);
            ApplySingleCoefficients(coefficients, ref numerator, ref denominator);
            return DivideRoundToEven(numerator, denominator);
        }

        /// <summary>
        /// Applies the exact IEEE-754 value of every supplied Single and truncates the
        /// final rational result toward zero, matching a C# floating-point-to-Int64 cast
        /// without first narrowing the Int64 operand to Single.
        /// </summary>
        internal static long TruncateSingleProduct(long value, params float[] coefficients)
        {
            ExactInteger numerator = ExactInteger.FromInt64(value);
            ExactInteger denominator = ExactInteger.One;
            ApplySingleCoefficients(coefficients, ref numerator, ref denominator);

            ExactInteger absolute = ExactInteger.Abs(numerator);
            ExactInteger quotient;
            ExactInteger remainder;
            ExactInteger.DivRem(absolute, denominator, out quotient, out remainder);
            if (numerator.Sign < 0)
            {
                quotient = ExactInteger.Negate(quotient);
            }
            return ToInt64Checked(quotient);
        }

        /// <summary>
        /// Compares two exact products of an Int64 fan total and RR's compiled Single
        /// momentum coefficient. Former-idol labels receive RR's exact compiled 2.5f
        /// multiplier. No Int64 result is materialized, so comparison remains exact even
        /// when either product would exceed Int64.
        /// </summary>
        internal static int CompareSingleProducts(
            long left,
            float leftCoefficient,
            bool leftFormerIdol,
            long right,
            float rightCoefficient,
            bool rightFormerIdol)
        {
            const long exactSingleIntegerBoundary = 16777216L;
            float leftProduct = (float)left * leftCoefficient;
            if (leftFormerIdol)
            {
                leftProduct *= 2.5f;
            }
            float rightProduct = (float)right * rightCoefficient;
            if (rightFormerIdol)
            {
                rightProduct *= 2.5f;
            }

            if (left >= -exactSingleIntegerBoundary &&
                left <= exactSingleIntegerBoundary &&
                right >= -exactSingleIntegerBoundary &&
                right <= exactSingleIntegerBoundary &&
                !float.IsNaN(leftProduct) &&
                !float.IsInfinity(leftProduct) &&
                !float.IsNaN(rightProduct) &&
                !float.IsInfinity(rightProduct) &&
                leftProduct >= -exactSingleIntegerBoundary &&
                leftProduct <= exactSingleIntegerBoundary &&
                rightProduct >= -exactSingleIntegerBoundary &&
                rightProduct <= exactSingleIntegerBoundary)
            {
                return leftProduct.CompareTo(rightProduct);
            }

            ExactInteger leftNumerator = ExactInteger.FromInt64(left);
            ExactInteger leftDenominator = ExactInteger.One;
            ApplySingleCoefficients(
                leftFormerIdol
                    ? new float[] { leftCoefficient, 2.5f }
                    : new float[] { leftCoefficient },
                ref leftNumerator,
                ref leftDenominator);

            ExactInteger rightNumerator = ExactInteger.FromInt64(right);
            ExactInteger rightDenominator = ExactInteger.One;
            ApplySingleCoefficients(
                rightFormerIdol
                    ? new float[] { rightCoefficient, 2.5f }
                    : new float[] { rightCoefficient },
                ref rightNumerator,
                ref rightDenominator);

            return ExactInteger.Compare(
                ExactInteger.Multiply(leftNumerator, rightDenominator),
                ExactInteger.Multiply(rightNumerator, leftDenominator));
        }

        internal static long FloorSingleProduct(long value, params float[] coefficients)
        {
            ExactInteger numerator = ExactInteger.FromInt64(value);
            ExactInteger denominator = ExactInteger.One;
            ApplySingleCoefficients(coefficients, ref numerator, ref denominator);

            ExactInteger absolute = ExactInteger.Abs(numerator);
            ExactInteger quotient;
            ExactInteger remainder;
            ExactInteger.DivRem(absolute, denominator, out quotient, out remainder);
            if (numerator.Sign < 0)
            {
                quotient = ExactInteger.Negate(quotient);
                if (!remainder.IsZero)
                {
                    quotient = ExactInteger.Subtract(quotient, ExactInteger.One);
                }
            }
            return ToInt64Checked(quotient);
        }

        internal static long CeilingSingleProduct(long value, params float[] coefficients)
        {
            ExactInteger numerator = ExactInteger.FromInt64(value);
            ExactInteger denominator = ExactInteger.One;
            ApplySingleCoefficients(coefficients, ref numerator, ref denominator);

            ExactInteger absolute = ExactInteger.Abs(numerator);
            ExactInteger quotient;
            ExactInteger remainder;
            ExactInteger.DivRem(absolute, denominator, out quotient, out remainder);
            if (numerator.Sign < 0)
            {
                quotient = ExactInteger.Negate(quotient);
            }
            else if (!remainder.IsZero)
            {
                quotient = ExactInteger.Add(quotient, ExactInteger.One);
            }
            return ToInt64Checked(quotient);
        }

        internal static long TruncateRatio(long value, long numerator, long denominator)
        {
            if (denominator <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            ExactInteger product = ExactInteger.Multiply(
                ExactInteger.FromInt64(value),
                ExactInteger.FromInt64(numerator));
            ExactInteger absolute = ExactInteger.Abs(product);
            ExactInteger quotient;
            ExactInteger remainder;
            ExactInteger.DivRem(absolute, ExactInteger.FromInt64(denominator),
                out quotient, out remainder);
            if (product.Sign < 0) quotient = ExactInteger.Negate(quotient);
            return ToInt64Checked(quotient);
        }

        internal static long RoundRatioToEven(long value, long numerator, long denominator)
        {
            if (denominator <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            return DivideRoundToEven(
                ExactInteger.Multiply(
                    ExactInteger.FromInt64(value),
                    ExactInteger.FromInt64(numerator)),
                ExactInteger.FromInt64(denominator));
        }

        private static long DivideRoundToEven(
            ExactInteger numerator,
            ExactInteger denominator)
        {
            if (denominator == null || denominator.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            int sign = numerator == null ? 0 : numerator.Sign;
            ExactInteger absolute = ExactInteger.Abs(numerator ?? ExactInteger.Zero);
            ExactInteger quotient;
            ExactInteger remainder;
            ExactInteger.DivRem(absolute, denominator, out quotient, out remainder);
            int comparison = ExactInteger.Compare(
                ExactInteger.ShiftLeft(remainder, 1),
                denominator);
            if (comparison > 0 || (comparison == 0 && !quotient.IsEven))
            {
                quotient = ExactInteger.Add(quotient, ExactInteger.One);
            }

            if (sign < 0)
            {
                quotient = ExactInteger.Negate(quotient);
            }
            return ToInt64Checked(quotient);
        }

        private static void DecomposeSingle(
            float value,
            out ExactInteger numerator,
            out ExactInteger denominator)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            byte[] bytes = BitConverter.GetBytes(value);
            uint bits = BitConverter.ToUInt32(bytes, 0);
            bool negative = (bits & 0x80000000U) != 0U;
            int exponent = (int)((bits >> 23) & 0xffU);
            uint fraction = bits & 0x7fffffU;

            if (exponent == 0 && fraction == 0U)
            {
                numerator = ExactInteger.Zero;
                denominator = ExactInteger.One;
                return;
            }

            uint significand;
            int binaryExponent;
            if (exponent == 0)
            {
                significand = fraction;
                binaryExponent = -149;
            }
            else
            {
                significand = fraction | 0x800000U;
                binaryExponent = exponent - 150;
            }

            numerator = ExactInteger.FromUInt32(significand);
            denominator = ExactInteger.One;
            if (binaryExponent >= 0)
            {
                numerator = ExactInteger.ShiftLeft(numerator, binaryExponent);
            }
            else
            {
                denominator = ExactInteger.ShiftLeft(denominator, -binaryExponent);
            }

            if (negative)
            {
                numerator = ExactInteger.Negate(numerator);
            }
        }

        private static void ApplySingleCoefficients(
            float[] coefficients,
            ref ExactInteger numerator,
            ref ExactInteger denominator)
        {
            if (coefficients == null)
            {
                throw new ArgumentNullException(nameof(coefficients));
            }
            for (int index = 0; index < coefficients.Length; index++)
            {
                ExactInteger coefficientNumerator;
                ExactInteger coefficientDenominator;
                DecomposeSingle(
                    coefficients[index],
                    out coefficientNumerator,
                    out coefficientDenominator);
                numerator = ExactInteger.Multiply(numerator, coefficientNumerator);
                denominator = ExactInteger.Multiply(denominator, coefficientDenominator);
            }
        }

        private static long ToInt64Checked(ExactInteger value)
        {
            long result;
            if (value == null || !value.TryToInt64(out result))
            {
                throw new OverflowException("Exact rational result is outside Int64.");
            }
            return result;
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
