using Wacton.Unicolour;

namespace XeniaDiscord.Common;

public static class UnicolourExtensions
{
    public static bool HasConversionError(this Unicolour colour)
    {
        // if the clipped RGB has no hex value, RGB is invalid (likely a NaN during conversion)
        return colour.Rgb.Byte255.Clipped.Hex == "-";
    }
    public static string ToCss(this Unicolour colour, double alpha)
    {
        if (colour.HasConversionError())
        {
            return "transparent";
        }

        var (r, g, b) = colour.Rgb.Clipped;
        return $"rgb({r * 100}% {g * 100}% {b * 100}% / {alpha}%)";
    }
}