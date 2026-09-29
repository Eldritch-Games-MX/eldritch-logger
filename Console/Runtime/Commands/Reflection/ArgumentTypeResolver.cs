using EldritchGames.EldritchLogger.Console.Arguments;
using System;
using System.Reflection;

namespace EldritchGames.EldritchLogger.Console.Commands.Reflection
{
    /// <summary>Maps .NET types to the console's argument types.</summary>
    public static class ArgumentTypeResolver
    {
        private static readonly MethodInfo EnumFactory = typeof(ArgumentTypes).GetMethod(nameof(ArgumentTypes.Enum));

        /// <summary>
        /// The argument type for <paramref name="type"/>: <c>int</c>, <c>float</c>, <c>double</c>, <c>bool</c>,
        /// <c>string</c> or an enum. Returns false for anything else.
        /// </summary>
        public static bool TryResolve(Type type, out IArgumentType argumentType)
        {
            argumentType = null;
            if (type == null) return false;

            if (type == typeof(int)) argumentType = ArgumentTypes.Int;
            else if (type == typeof(float)) argumentType = ArgumentTypes.Float;
            else if (type == typeof(double)) argumentType = ArgumentTypes.Double;
            else if (type == typeof(bool)) argumentType = ArgumentTypes.Bool;
            else if (type == typeof(string)) argumentType = ArgumentTypes.String;
            else if (type.IsEnum) argumentType = (IArgumentType)EnumFactory.MakeGenericMethod(type).Invoke(null, null);

            return argumentType != null;
        }

        /// <summary>Formats a value for display (invariant culture, <c>null</c> as text).</summary>
        public static string Format(object value) => value switch
        {
            null => "null",
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => value.ToString()
        };
    }
}
