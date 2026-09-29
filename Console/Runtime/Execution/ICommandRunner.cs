using System;
using System.Collections;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Console.Execution
{
    /// <summary>Runs asynchronous (coroutine) commands.</summary>
    public interface ICommandRunner
    {
        /// <param name="routine">The command's coroutine.</param>
        /// <param name="onError">Called if the routine throws; the routine is then stopped.</param>
        void Run(IEnumerator routine, Action<Exception> onError);
    }

    /// <summary>Runs command coroutines on a <see cref="MonoBehaviour"/>.</summary>
    public sealed class CoroutineCommandRunner : ICommandRunner
    {
        private readonly MonoBehaviour host;

        public CoroutineCommandRunner(MonoBehaviour host)
        {
            this.host = host != null ? host : throw new ArgumentNullException(nameof(host));
        }

        public void Run(IEnumerator routine, Action<Exception> onError) =>
            host.StartCoroutine(Guard(routine, onError));

        private static IEnumerator Guard(IEnumerator routine, Action<Exception> onError)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext()) yield break;
                    current = routine.Current;
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                    yield break;
                }
                yield return current;
            }
        }
    }

    /// <summary>
    /// Runs a coroutine to completion immediately, ignoring what it yields (waits are skipped).
    /// Useful in edit-mode tests and headless tools.
    /// </summary>
    public sealed class ImmediateCommandRunner : ICommandRunner
    {
        public const int MaxSteps = 100_000;

        public void Run(IEnumerator routine, Action<Exception> onError)
        {
            try
            {
                int steps = 0;
                while (routine.MoveNext())
                {
                    if (routine.Current is IEnumerator nested)
                        Run(nested, onError);
                    if (++steps > MaxSteps)
                        throw new InvalidOperationException($"Command did not finish within {MaxSteps} steps.");
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }
    }
}
