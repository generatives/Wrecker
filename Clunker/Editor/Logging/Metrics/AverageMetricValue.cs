using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Editor.Logging.Metrics
{
    public class AverageMetricValue : Editor
    {
        public override string Name => "Average Metric Value";

        public override string Category => "Logging";

        public override void DrawEditor(double delta)
        {
            foreach(var name in Utilties.Logging.Metrics.ListMetrics())
            {
                var metrics = Utilties.Logging.Metrics.GetMetrics(name);
                lock(metrics)
                {
                    var average = metrics.Select(t => t.Item2).Average();
                    ImGui.Text($"{name}: {average}");
                }
            }
        }
    }
}
