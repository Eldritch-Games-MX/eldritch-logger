using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Dto;
using EldritchGames.EldritchLogger.Settings;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EldritchGames.EldritchLogger.Formatting
{
    /// <summary>
    /// Formats entries as <c>[time] [Level] Category prefix message metadata</c>.
    /// With <c>richText</c> the category is wrapped in a <c>&lt;color&gt;</c> tag
    /// (Unity Console, TextMeshPro); without it the output is plain text (files).
    /// </summary>
    public sealed class TextLogFormatter : ILogFormatter
    {
        public const string DefaultTimestampFormat = "HH:mm:ss";

        private readonly LogSettings settings;
        private readonly bool richText;

        /// <param name="settings">Formatting options and category colors. May be null (defaults are used).</param>
        /// <param name="richText">Emit Unity rich-text color tags.</param>
        public TextLogFormatter(LogSettings settings, bool richText)
        {
            this.settings = settings;
            this.richText = richText;
        }

        public string Format(LogEntryDto entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            string timestampFormat = string.IsNullOrEmpty(settings?.timestampFormat)
                ? DefaultTimestampFormat
                : settings.timestampFormat;
            string prefix = settings?.messagePrefix ?? string.Empty;

            var sb = new StringBuilder(128);
            sb.Append('[').Append(ToLocal(entry.Timestamp).ToString(timestampFormat)).Append("] ");
            sb.Append('[').Append(entry.Level).Append("] ");
            AppendCategory(sb, entry.Category);
            sb.Append(' ').Append(prefix).Append(entry.Message);
            AppendMetadata(sb, entry);

            if (!string.IsNullOrEmpty(entry.Exception))
                sb.Append("\nException: ").Append(entry.Exception);

            return sb.ToString();
        }

        private void AppendCategory(StringBuilder sb, string category)
        {
            if (richText && settings != null && settings.useCategoryColors && !string.IsNullOrEmpty(category))
            {
                var color = settings.GetCategoryColor(new LogCategory(category));
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>')
                  .Append(category).Append("</color>");
            }
            else
            {
                sb.Append(category);
            }
        }

        private static void AppendMetadata(StringBuilder sb, LogEntryDto entry)
        {
            if (entry.Metadata == null) return;

            // Filled template holes are already in the message; repeating them (and the template) is noise.
            foreach (var kv in entry.Metadata)
            {
                if (kv.InMessage || kv.Key == LogPropertyKeys.MessageTemplate) continue;

                sb.Append(' ');
                switch (kv.Key)
                {
                    case LogPropertyKeys.Component: sb.Append("[Component=").Append(kv.Value).Append(']'); break;
                    case LogPropertyKeys.GameObject: sb.Append("[GameObject=").Append(kv.Value).Append(']'); break;
                    case LogPropertyKeys.CSharpEvent: sb.Append("[C#Event=").Append(kv.Value).Append(']'); break;
                    case LogPropertyKeys.UnityEvent: sb.Append("[UnityEvent=").Append(kv.Value).Append(']'); break;
                    default: sb.Append(kv.Key).Append('=').Append(kv.Value); break;
                }
            }
        }

        private static DateTime ToLocal(DateTime timestamp) =>
            timestamp.Kind == DateTimeKind.Utc ? timestamp.ToLocalTime() : timestamp;
    }
}
