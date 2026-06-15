using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CyberBot_PART_3
{
    public static class ActivityLogger
    {
        private static readonly List<string> _entries = new();
        private static int _viewOffset = 0;
        private const int PageSize = 5;
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cyberbot_activity.log");

        public static void Log(string action)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]  {action}";
            _entries.Add(entry);
            if (_entries.Count > 200) _entries.RemoveAt(0);
            try
            {
                File.AppendAllText(LogFile, entry + Environment.NewLine);
            }
            catch { }
        }

        public static void LogSessionBoundary(bool isStart)
        {
            Log(isStart ? "═════ SESSION STARTED ═════" : "═════ SESSION ENDED ═════");
        }

        public static string ViewLog()
        {
            _viewOffset = 0;
            return BuildPage();
        }

        public static string ShowMore()
        {
            _viewOffset += PageSize;
            return BuildPage();
        }

        public static bool HasMore => _entries.Count > 0 && _viewOffset + PageSize < _entries.Count;

        private static string BuildPage()
        {
            if (_entries.Count == 0)
                return "📋  No activity recorded yet.";

            var reversed = _entries.AsEnumerable().Reverse().ToList();
            var page = reversed.Skip(_viewOffset).Take(PageSize).ToList();

            if (page.Count == 0)
                return "📋  No more entries to show.";

            var sb = new StringBuilder();
            sb.AppendLine($"📋  ACTIVITY LOG (showing {_viewOffset + 1}–{_viewOffset + page.Count} of {_entries.Count})");
            sb.AppendLine("────────────────────────────────────────────────");
            foreach (string e in page)
                sb.AppendLine($"  {e}");

            return sb.ToString();
        }

        public static void Clear()
        {
            _entries.Clear();
            _viewOffset = 0;
            try { File.WriteAllText(LogFile, string.Empty); }
            catch { }
        }
    }
}