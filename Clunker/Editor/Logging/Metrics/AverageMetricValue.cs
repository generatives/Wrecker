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

        private string _search = "";

        public override void DrawEditor(double delta)
        {
            ImGui.InputText("Search", ref _search, 255);
            foreach(var name in Utilties.Logging.Metrics.ListMetrics())
            {
                if(string.IsNullOrWhiteSpace(_search) || name.Contains(_search))
                {
                    var metrics = Utilties.Logging.Metrics.GetMetrics(name);
                    lock (metrics)
                    {
                        var average = metrics.Select(t => t.Item2).Average();
                        ImGui.Text($"{name}: {average}");
                    }
                }
            }
        }
    }
}
