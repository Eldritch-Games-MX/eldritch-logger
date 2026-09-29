using System;
using System.Collections.Generic;
using System.Threading;

namespace EldritchGames.EldritchLogger.Pipeline
{
    /// <summary>
    /// Ambient properties added to every entry logged inside a <c>using</c> block:
    /// <code>
    /// using (logger.BeginScope("MatchId", match.Id))
    /// {
    ///     logger.AtInfo().Log("Round started"); // carries MatchId
    /// }
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scopes flow across <c>await</c> and <c>Task.Run</c> (they are stored in an <see cref="AsyncLocal{T}"/>)
    /// and nest; inner scopes win on duplicate keys, and properties set on the entry itself win over scopes.
    /// The logger applies scopes itself, so they work with any enricher configuration.
    /// </para>
    /// <para>
    /// <b>Never keep a scope open across a coroutine <c>yield</c>.</b> Unity resumes coroutines from native
    /// code on the main thread without restoring the execution context, so a scope opened before a
    /// <c>yield</c> stays active for everything logged on the main thread until the coroutine resumes and
    /// disposes it. Analyzer rule ELG005 flags this. Dispose scopes in reverse order (a <c>using</c> does this).
    /// </para>
    /// </remarks>
    public static class LogScope
    {
        private sealed class Frame
        {
            public readonly Frame Parent;
            public readonly KeyValuePair<string, object>[] Properties;

            public Frame(Frame parent, KeyValuePair<string, object>[] properties)
            {
                Parent = parent;
                Properties = properties;
            }
        }

        private sealed class Handle : IDisposable
        {
            private Frame frame;

            public Handle(Frame frame) => this.frame = frame;

            public void Dispose()
            {
                var closing = Interlocked.Exchange(ref frame, null);
                if (closing == null) return;

                // Normally the scope being closed is the innermost one; if scopes were closed out of
                // order, drop everything opened after it too rather than leaving it active.
                if (IsActive(closing))
                    current.Value = closing.Parent;
            }
        }

        private static readonly AsyncLocal<Frame> current = new();

        /// <summary>Opens a scope with one property.</summary>
        public static IDisposable Push(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return Push(new[] { new KeyValuePair<string, object>(key, value) });
        }

        /// <summary>Opens a scope with several properties.</summary>
        public static IDisposable Push(IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));
            return PushOwned(new List<KeyValuePair<string, object>>(properties).ToArray());
        }

        /// <summary>Opens a scope over an array nobody else holds (no defensive copy).</summary>
        internal static IDisposable PushOwned(KeyValuePair<string, object>[] properties)
        {
            foreach (var kv in properties)
                if (kv.Key == null) throw new ArgumentException("Scope property keys cannot be null.", nameof(properties));

            var frame = new Frame(current.Value, properties);
            current.Value = frame;
            return new Handle(frame);
        }

        /// <summary>
        /// Adds the active scope properties to <paramref name="target"/>. Keys already present are kept, so
        /// entry properties beat scope properties and inner scopes beat outer ones. Allocates nothing.
        /// </summary>
        public static void CopyTo(IDictionary<string, object> target)
        {
            // Innermost frame first; within a frame, the last duplicate key wins.
            for (var frame = current.Value; frame != null; frame = frame.Parent)
            {
                var properties = frame.Properties;
                for (int i = properties.Length - 1; i >= 0; i--)
                    if (!target.ContainsKey(properties[i].Key))
                        target.Add(properties[i].Key, properties[i].Value);
            }
        }

        /// <summary>True when at least one scope is open on the current execution flow.</summary>
        public static bool HasActiveScopes => current.Value != null;

        private static bool IsActive(Frame frame)
        {
            for (var f = current.Value; f != null; f = f.Parent)
                if (ReferenceEquals(f, frame)) return true;
            return false;
        }
    }
}
