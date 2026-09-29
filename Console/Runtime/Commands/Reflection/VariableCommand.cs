using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Output;
using System;
using System.Reflection;

namespace EldritchGames.EldritchLogger.Console.Commands.Reflection
{
    /// <summary>
    /// A console variable backed by a static field or property (<see cref="ConsoleVariableAttribute"/>):
    /// <c>name</c> prints the value, <c>name value</c> sets it.
    /// </summary>
    public sealed class VariableCommand : IConsoleCommand
    {
        private readonly FieldInfo field;
        private readonly PropertyInfo property;
        private readonly double min;
        private readonly double max;
        private readonly bool isCheat;

        public CommandDescriptor Descriptor { get; }

        public Type ValueType { get; }

        public bool CanWrite { get; }

        public string Description { get; }

        internal VariableCommand(MemberInfo member)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            var attribute = member.GetCustomAttribute<ConsoleVariableAttribute>();
            string where = $"{member.DeclaringType?.Name}.{member.Name}";

            switch (member)
            {
                case FieldInfo f:
                    if (!f.IsStatic) throw new NotSupportedException($"{where} must be static.");
                    field = f;
                    ValueType = f.FieldType;
                    CanWrite = !f.IsInitOnly && !f.IsLiteral;
                    break;
                case PropertyInfo p:
                    var getter = p.GetGetMethod(true);
                    if (getter == null) throw new NotSupportedException($"{where} has no getter.");
                    if (!getter.IsStatic) throw new NotSupportedException($"{where} must be static.");
                    if (p.GetIndexParameters().Length > 0) throw new NotSupportedException($"{where} is an indexer.");
                    property = p;
                    ValueType = p.PropertyType;
                    CanWrite = p.GetSetMethod(true) != null;
                    break;
                default:
                    throw new NotSupportedException($"{where} is not a field or property.");
            }

            if (!ArgumentTypeResolver.TryResolve(ValueType, out var argumentType))
                throw new NotSupportedException($"{where} has unsupported type {ValueType.Name}.");

            CanWrite &= !(attribute?.ReadOnly ?? false);
            min = attribute?.Min ?? double.NaN;
            max = attribute?.Max ?? double.NaN;
            isCheat = attribute?.IsCheat ?? false;
            Description = attribute?.Description ?? string.Empty;

            var summary = CanWrite ? "Console variable" : "Console variable (read-only)";
            if (!double.IsNaN(min) || !double.IsNaN(max))
                summary += $", range {(double.IsNaN(min) ? "-∞" : Format(min))}..{(double.IsNaN(max) ? "∞" : Format(max))}";

            Descriptor = new CommandDescriptor(
                attribute?.Name ?? ReflectionCommands.DefaultName(member),
                string.IsNullOrEmpty(Description) ? summary + "." : $"{Description} ({summary.ToLowerInvariant()})",
                parameters: CanWrite ? new[] { ParameterSpec.Optional("value", argumentType) } : null);
            // Not a descriptor-level cheat: reading is always allowed, only writes check the cheat policy.
        }

        /// <summary>True when changing the value requires cheats.</summary>
        public bool IsCheat => isCheat;

        public object Value
        {
            get => field != null ? field.GetValue(null) : property.GetValue(null);
            private set
            {
                if (field != null) field.SetValue(null, value);
                else property.SetValue(null, value);
            }
        }

        public void Execute(CommandContext context)
        {
            var name = Descriptor.Name;
            if (!context.Arguments.Has("value"))
            {
                context.Output.Info($"{name} = {ArgumentTypeResolver.Format(Value)}");
                return;
            }

            if (isCheat && !context.CheatsAllowed)
            {
                context.Output.Warn($"Changing '{name}' is a cheat and cheats are disabled.");
                return;
            }

            var value = context.Arguments.Get<object>("value");
            if (IsNumeric(value, out var number) &&
                ((!double.IsNaN(min) && number < min) || (!double.IsNaN(max) && number > max)))
            {
                context.Output.Warn($"{name} must be between {(double.IsNaN(min) ? "-∞" : Format(min))} and {(double.IsNaN(max) ? "∞" : Format(max))}.");
                return;
            }

            try
            {
                Value = value;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            context.Output.Info($"{name} = {ArgumentTypeResolver.Format(Value)}");
        }

        private static bool IsNumeric(object value, out double number)
        {
            switch (value)
            {
                case int i: number = i; return true;
                case float f: number = f; return true;
                case double d: number = d; return true;
                default: number = 0; return false;
            }
        }

        private static string Format(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
