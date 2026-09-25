using System.Globalization;

namespace IMDataCore.DataMigrationTool.Migration;

internal static class GameDateUtilities
{
    internal const string VanillaDataFormat = "yyyy-MM-dd HH:mm:ss";
    internal const string RoundTripFormat = "o";

    internal static bool TryParseVanillaOrRoundTrip(string? value, out DateTime parsed)
    {
        string text = value ?? string.Empty;
        if (DateTime.TryParseExact(
                text,
                VanillaDataFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed))
            return true;

        return DateTime.TryParseExact(
            text,
            RoundTripFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out parsed);
    }

    internal static bool TryParseRoundTrip(string? value, out DateTime parsed) =>
        DateTime.TryParseExact(
            value ?? string.Empty,
            RoundTripFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out parsed);

    internal static string ToRoundTripFromVanilla(string? value)
    {
        if (!TryParseVanillaOrRoundTrip(value, out DateTime parsed))
            throw new InvalidDataException(
                "Vanilla save staticVars__dateTime is not a valid Idol Manager game date.");

        return parsed.ToString(RoundTripFormat, CultureInfo.InvariantCulture);
    }
}
