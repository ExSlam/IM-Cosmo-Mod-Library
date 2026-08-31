using System;
using System.Globalization;
using System.Collections.Generic;
using SaveNLoadFixes.Persistence;

namespace SaveNLoadFixes.Repairs
{
    /// <summary>
    /// Repair-owned deterministic entropy for legacy save migration. This deliberately
    /// uses only fixed implementation-owned primitives so compatibility loading cannot
    /// perturb the game's global RNG stream or inherit runtime-randomized hash behavior.
    /// </summary>
    internal static class LegacyMigrationDeterminism
    {
        internal const int AlgorithmVersion = 1;

        internal sealed class Stream
        {
            private ulong state;

            internal Stream(ulong seed)
            {
                state = seed;
            }

            internal int NextInt(int minInclusive, int maxExclusive)
            {
                if (maxExclusive <= minInclusive)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxExclusive));
                }

                ulong span = (ulong)(maxExclusive - minInclusive);
                ulong value = NextUInt64();
                return minInclusive + (int)(value % span);
            }

            private ulong NextUInt64()
            {
                unchecked
                {
                    state += 0x9E3779B97F4A7C15UL;
                    ulong value = state;
                    value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                    value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                    return value ^ (value >> 31);
                }
            }
        }

        internal static void ShuffleInPlace<T>(IList<T> items, Stream stream)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // Fisher-Yates with repair-owned entropy. Unlike ExtensionMethods.Shuffle,
            // this never consumes UnityEngine.Random and therefore cannot perturb the
            // post-load gameplay RNG trajectory merely because a legacy migration ran.
            for (int index = items.Count - 1; index > 0; index--)
            {
                int swapIndex = stream.NextInt(0, index + 1);
                if (swapIndex == index)
                {
                    continue;
                }

                T value = items[index];
                items[index] = items[swapIndex];
                items[swapIndex] = value;
            }
        }

        internal static Stream CreateStream(
            SaveManager.SavedData data,
            string familyCode,
            string entityCode,
            string fieldCode,
            out bool usedPhysicalPath)
        {
            string saveIdentity = BuildSaveIdentity(data, out usedPhysicalPath);
            ulong seed = HashParts(
                "snlf-legacy-migration",
                AlgorithmVersion.ToString(CultureInfo.InvariantCulture),
                saveIdentity,
                familyCode ?? string.Empty,
                entityCode ?? string.Empty,
                fieldCode ?? string.Empty);
            return new Stream(seed);
        }

        internal static string BuildSaveIdentity(
            SaveManager.SavedData data,
            out bool usedPhysicalPath)
        {
            usedPhysicalPath = false;
            string physicalPath = string.Empty;

            RepairEnvelopeLoadState state;
            if (data != null &&
                RepairEnvelopeTransport.TryGetLoadState(data, out state) &&
                state != null &&
                !string.IsNullOrEmpty(state.PhysicalPath))
            {
                physicalPath = state.PhysicalPath.Replace('\\', '/');
                usedPhysicalPath = true;
            }

            return string.Join(
                "|",
                new string[]
                {
                    usedPhysicalPath ? "path:" + physicalPath : "path:<unavailable>",
                    "version:" + (data == null ? string.Empty : data.version ?? string.Empty),
                    "date:" + (data == null ? string.Empty : data.staticVars__dateTime ?? string.Empty),
                    "girl_allocator:" + (data == null
                        ? string.Empty
                        : data.data_girls__LastGirlID.ToString(CultureInfo.InvariantCulture))
                });
        }

        private static ulong HashParts(params string[] parts)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;

            unchecked
            {
                if (parts != null)
                {
                    for (int p = 0; p < parts.Length; p++)
                    {
                        string part = parts[p] ?? string.Empty;
                        for (int i = 0; i < part.Length; i++)
                        {
                            char ch = part[i];
                            hash ^= (byte)(ch & 0xFF);
                            hash *= prime;
                            hash ^= (byte)((ch >> 8) & 0xFF);
                            hash *= prime;
                        }

                        // Component separator prevents tuple-concatenation ambiguity.
                        hash ^= 0xFF;
                        hash *= prime;
                    }
                }
            }

            return hash;
        }
    }
}
