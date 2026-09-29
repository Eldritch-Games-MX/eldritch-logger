using EldritchGames.EldritchLogger.Console.Arguments;
using EldritchGames.EldritchLogger.Console.Output;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Commands.Reflection
{
    /// <summary>
    /// Creates commands from <see cref="ConsoleMethodAttribute"/> methods and
    /// <see cref="ConsoleVariableAttribute"/> fields and properties.
    /// </summary>
    public static class ReflectionCommands
    {
        /// <summary>Creates the command for an attributed method. Throws <see cref="NotSupportedException"/> when it cannot be exposed.</summary>
        public static ICommand FromMethod(MethodInfo method)
        {
            var binding = new MethodBinding(method);
            return typeof(IEnumerator).IsAssignableFrom(method.ReturnType)
                ? new AsyncMethodCommand(binding)
                : new MethodCommand(binding);
        }

        /// <summary>Creates the command for an attributed field or property. Throws <see cref="NotSupportedException"/> when it cannot be exposed.</summary>
        public static ICommand FromMember(MemberInfo member) => new VariableCommand(member);

        /// <summary>Lower-cased member name, used when the attribute gives no name.</summary>
        internal static string DefaultName(MemberInfo member) => member.Name.ToLowerInvariant();
    }

    /// <summary>Descriptor and invocation logic shared by sync and coroutine method commands.</summary>
    internal sealed class MethodBinding
    {
        private enum Slot { Value, Variadic, Context, Output }

        private readonly MethodInfo method;
        private readonly Slot[] slots;
        private readonly ParameterInfo[] parameters;

        public CommandDescriptor Descriptor { get; }

        public MethodBinding(MethodInfo method)
        {
            this.method = method ?? throw new ArgumentNullException(nameof(method));
            var attribute = method.GetCustomAttribute<ConsoleMethodAttribute>();

            if (method.ContainsGenericParameters)
                throw new NotSupportedException($"{Describe(method)} is generic.");
            if (!method.IsStatic && !typeof(Component).IsAssignableFrom(method.DeclaringType))
                throw new NotSupportedException($"{Describe(method)} must be static, or an instance method of a MonoBehaviour.");

            parameters = method.GetParameters();
            slots = new Slot[parameters.Length];
            var specs = new List<ParameterSpec>();

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.ParameterType == typeof(CommandContext)) { slots[i] = Slot.Context; continue; }
                if (p.ParameterType == typeof(IConsoleOutput)) { slots[i] = Slot.Output; continue; }
                if (p.ParameterType.IsByRef || p.IsOut)
                    throw new NotSupportedException($"Parameter '{p.Name}' of {Describe(method)} is ref/out.");

                bool isParams = p.IsDefined(typeof(ParamArrayAttribute), false);
                var valueType = isParams ? p.ParameterType.GetElementType() : p.ParameterType;
                if (!ArgumentTypeResolver.TryResolve(valueType, out var argumentType))
                    throw new NotSupportedException($"Parameter '{p.Name}' of {Describe(method)} has unsupported type {valueType.Name}.");

                if (isParams)
                {
                    slots[i] = Slot.Variadic;
                    specs.Add(ParameterSpec.Variadic(p.Name, argumentType));
                }
                else
                {
                    slots[i] = Slot.Value;
                    specs.Add(p.HasDefaultValue ? ParameterSpec.Optional(p.Name, argumentType) : ParameterSpec.Required(p.Name, argumentType));
                }
            }

            var description = attribute?.Description;
            if (!method.IsStatic)
                description = $"{description} (runs on every active {method.DeclaringType.Name})".Trim();

            Descriptor = new CommandDescriptor(
                attribute?.Name ?? ReflectionCommands.DefaultName(method),
                description,
                parameters: specs,
                aliases: attribute?.Aliases,
                isCheat: attribute?.IsCheat ?? false);
        }

        public bool ReturnsValue => method.ReturnType != typeof(void);

        /// <summary>Invokes the method on its target(s). Returns (target name or null, return value) per invocation.</summary>
        public List<(string target, object result)> Invoke(CommandContext context)
        {
            var args = BuildArguments(context);
            var results = new List<(string, object)>();

            if (method.IsStatic)
            {
                results.Add((null, Call(null, args)));
                return results;
            }

            var targets = UnityEngine.Object.FindObjectsByType(method.DeclaringType, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (targets.Length == 0)
            {
                context.Output.Warn($"No active {method.DeclaringType.Name} to run '{Descriptor.Name}' on.");
                return results;
            }

            foreach (var target in targets)
                results.Add((target.name, Call(target, args)));
            return results;
        }

        private object[] BuildArguments(CommandContext context)
        {
            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                switch (slots[i])
                {
                    case Slot.Context:
                        args[i] = context;
                        break;
                    case Slot.Output:
                        args[i] = context.Output;
                        break;
                    case Slot.Variadic:
                        var values = context.Arguments.GetAll<object>(p.Name);
                        var array = Array.CreateInstance(p.ParameterType.GetElementType(), values.Count);
                        for (int v = 0; v < values.Count; v++) array.SetValue(values[v], v);
                        args[i] = array;
                        break;
                    default:
                        args[i] = context.Arguments.Has(p.Name) ? context.Arguments.GetOrDefault<object>(p.Name) : p.DefaultValue;
                        break;
                }
            }
            return args;
        }

        private object Call(object target, object[] args)
        {
            try
            {
                return method.Invoke(target, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Surface the method's own exception, not the reflection wrapper.
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static string Describe(MethodInfo method) => $"{method.DeclaringType?.Name}.{method.Name}";
    }

    /// <summary>A console command backed by a <see cref="ConsoleMethodAttribute"/> method.</summary>
    public sealed class MethodCommand : IConsoleCommand
    {
        private readonly MethodBinding binding;

        internal MethodCommand(MethodBinding binding) => this.binding = binding;

        public CommandDescriptor Descriptor => binding.Descriptor;

        public void Execute(CommandContext context)
        {
            foreach (var (target, result) in binding.Invoke(context))
            {
                if (!binding.ReturnsValue) continue;
                var text = ArgumentTypeResolver.Format(result);
                context.Output.Info(target == null ? text : $"{target}: {text}");
            }
        }
    }

    /// <summary>A coroutine console command backed by a <see cref="ConsoleMethodAttribute"/> method returning <c>IEnumerator</c>.</summary>
    public sealed class AsyncMethodCommand : IAsyncConsoleCommand
    {
        private readonly MethodBinding binding;

        internal AsyncMethodCommand(MethodBinding binding) => this.binding = binding;

        public CommandDescriptor Descriptor => binding.Descriptor;

        public IEnumerator Execute(CommandContext context)
        {
            // Instance methods: one routine per target, run one after another.
            foreach (var routine in binding.Invoke(context).Select(r => r.result).OfType<IEnumerator>().ToList())
                yield return routine;
        }
    }
}
