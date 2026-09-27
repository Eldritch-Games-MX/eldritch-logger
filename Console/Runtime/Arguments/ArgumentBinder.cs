using EldritchGames.EldritchLogger.Console.Commands;
using EldritchGames.EldritchLogger.Console.Parsing;
using System;
using System.Collections.Generic;

namespace EldritchGames.EldritchLogger.Console.Arguments
{
    public sealed class BindResult
    {
        public bool Success { get; }
        public string Error { get; }
        public CommandArguments Arguments { get; }

        private BindResult(bool success, string error, CommandArguments arguments)
        {
            Success = success;
            Error = error;
            Arguments = arguments;
        }

        public static BindResult Ok(CommandArguments arguments) => new(true, null, arguments);
        public static BindResult Fail(string error) => new(false, error, null);
    }

    public interface IArgumentBinder
    {
        BindResult Bind(CommandDescriptor descriptor, ParsedCommand command);
    }

    /// <summary>
    /// Validates a command line against a <see cref="CommandDescriptor"/> and produces typed
    /// <see cref="CommandArguments"/>. Flags may appear anywhere before a remainder parameter,
    /// as <c>--name=value</c>, <c>--name value</c> or (switches) <c>--name</c>.
    /// </summary>
    public sealed class ArgumentBinder : IArgumentBinder
    {
        public BindResult Bind(CommandDescriptor descriptor, ParsedCommand command)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            if (command == null) throw new ArgumentNullException(nameof(command));

            var args = new CommandArguments();
            var tokens = command.ArgumentTokens;
            var parameters = descriptor.Parameters;
            List<object> variadic = null;
            int paramIndex = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var current = paramIndex < parameters.Count ? parameters[paramIndex] : null;

                if (token.Type == TokenType.Flag)
                {
                    var error = BindFlag(descriptor, tokens, ref i, args);
                    if (error != null) return Fail(descriptor, error);
                    continue;
                }

                if (current == null)
                    return Fail(descriptor, $"Too many arguments: '{token.Value}' was not expected.");

                if (current.Kind == ParameterKind.Remainder)
                {
                    var rest = new Token[tokens.Count - i];
                    for (int j = i; j < tokens.Count; j++) rest[j - i] = tokens[j];
                    args.SetRemainder(current.Name, rest);
                    paramIndex++;
                    break;
                }

                if (!current.Type.TryParse(token.Value, out var value, out var parseError))
                    return Fail(descriptor, $"Invalid value for <{current.Name}>: {parseError}");

                if (current.Kind == ParameterKind.Variadic)
                {
                    (variadic ??= new List<object>()).Add(value);
                }
                else
                {
                    args.SetValue(current.Name, value);
                    paramIndex++;
                }
            }

            if (variadic != null)
                args.SetValue(parameters[paramIndex].Name, variadic);

            foreach (var parameter in parameters)
                if (parameter.IsRequired && !args.Has(parameter.Name))
                    return Fail(descriptor, $"Missing required argument <{parameter.Name}>.");

            foreach (var flag in descriptor.Flags)
                if (flag.IsRequired && !args.HasFlag(flag.Name))
                    return Fail(descriptor, $"Missing required flag --{flag.Name}.");

            return BindResult.Ok(args);
        }

        private static string BindFlag(CommandDescriptor descriptor, IReadOnlyList<Token> tokens, ref int i, CommandArguments args)
        {
            var text = tokens[i].Value;
            int eq = text.IndexOf('=');
            string name = eq >= 0 ? text.Substring(0, eq) : text;
            string inlineValue = eq >= 0 ? text.Substring(eq + 1) : null;

            var spec = descriptor.FindFlag(name);
            if (spec == null)
                return $"Unknown flag '--{name}'.";

            if (spec.IsSwitch)
            {
                if (inlineValue != null)
                    return $"Flag --{spec.Name} does not take a value.";
                args.SetFlag(spec.Name, true);
                return null;
            }

            string raw = inlineValue;
            if (raw == null)
            {
                if (i + 1 >= tokens.Count || tokens[i + 1].Type == TokenType.Flag)
                    return $"Flag --{spec.Name} requires a value.";
                raw = tokens[++i].Value;
            }

            if (!spec.ValueType.TryParse(raw, out var value, out var error))
                return $"Invalid value for --{spec.Name}: {error}";

            args.SetFlag(spec.Name, value);
            return null;
        }

        private static BindResult Fail(CommandDescriptor descriptor, string error) =>
            BindResult.Fail($"{error} Usage: {descriptor.Usage}");
    }
}
