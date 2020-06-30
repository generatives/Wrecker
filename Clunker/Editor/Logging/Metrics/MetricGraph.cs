using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Editor.Logging.Metrics
{
    public class MetricGraph : Editor
    {
        public override string Name => "Metric Graph";

        public override string Category => "Logging";

        private string _selectedMetric;

        public override void DrawEditor(double delta)
        {
            var metricNames = Utilties.Logging.Metrics.ListMetrics();
            if (ImGui.BeginCombo("Metrics", _selectedMetric)) // The second parameter is the label previewed before opening the combo.
            {
                foreach (var metric in metricNames)
                {
                    bool is_selected = metric == _selectedMetric;
                    if (ImGui.Selectable(metric, is_selected))
                        _selectedMetric = metric;
                    if (is_selected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            if(_selectedMetric != null)
            {
                var metrics = Utilties.Logging.Metrics.GetMetrics(_selectedMetric);
                var values = metrics.Select(t => (float)t.Item2).ToArray();

                ImGui.PlotLines("Values", ref values[0], values.Length);
            }
        }
    }
}
