using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Clunker.Diagnostics
{
    public class Timing
    {
        public static bool Enabled { get; set; }
        private static List<(string, int, Stopwatch)> _watches = new List<(string, int, Stopwatch)>();
        private static int _depth;

        public static void PushFrameTimer(string name)
        {
            if (!Enabled) return;
            _watches.Add((name, _depth, Stopwatch.StartNew()));
            _depth++;
        }

        public static void PopFrameTimer()
        {
            if (!Enabled) return;
            var (name, depth, watch) = _watches[_watches.Count - _depth];
            watch.Stop();
            _depth--;
        }

        internal static void Render(float frameTime)
        {
            if (!_watches.Any()) return;

            ImGui.Begin("Frame Times");
            ImGui.Text($"Framerate: {1f / frameTime}");
            foreach(var (name, depth, watch) in _watches)
            {
                ImGui.Text($"{new string(' ', depth)}{name}: {(float)watch.Elapsed.TotalMilliseconds}ms");
            }
            ImGui.End();
            _watches.Clear();
        }
    }
}
