using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Diagnostics
{
    public class Timing
    {
        public static bool Enabled { get; set; }
        private static Dictionary<string, (double, DateTime)> _previousTimes = new Dictionary<string, (double, DateTime)>();

        public static void ReportTime(string name, double time, float ttk = 1000f)
        {
            if (!Enabled) return;
            _previousTimes[name] = (time, DateTime.Now + TimeSpan.FromMilliseconds(ttk));
        }

        internal static void Render(float frameTime)
        {
            if (!_previousTimes.Any()) return;

            var now = DateTime.Now;
            var toRemove = new List<string>();
            ImGui.Begin("Times");
            foreach (var (name, (time, ttk)) in _previousTimes)
            {
                ImGui.Text($"{name}: {time}ms");
                if(ttk < now)
                {
                    toRemove.Add(name);
                }
            }
            ImGui.End();

            foreach(var name in toRemove)
            {
                _previousTimes.Remove(name);
            }
        }
    }
}
