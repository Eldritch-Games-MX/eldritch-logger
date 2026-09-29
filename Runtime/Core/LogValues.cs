using System;
using System.Globalization;

namespace EldritchGames.EldritchLogger.Core
{
    /// <summary>Turns property and template values into text without ever throwing.</summary>
    internal static class LogValues
    {
        /// <summary>
        /// Formats <paramref name="value"/> with the invariant culture, so output is the same on every machine
        /// (<c>182.4</c>, not <c>182,4</c> on a German one). An invalid <paramref name="format"/> is ignored;
        /// a value whose <c>ToString()</c> throws becomes <c>&lt;TypeName&gt;</c>. Logging must never break the caller.
        /// </summary>
        public static string Format(object value, string format = null)
        {
            if (value == null) return null;
            try
            {
                return value is IFormattable formattable
                    ? formattable.ToString(format, CultureInfo.InvariantCulture)
                    : value.ToString();
            }
            catch (FormatException) when (format != null)
            {
                return Format(value); // e.g. "{Hp:D}" given a float
            }
            catch (Exception)
            {
                return "<" + value.GetType().Name + ">";
            }
        }
    }
}
