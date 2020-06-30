using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Clunker.Utilties.Logging
{
    public class Metrics
    {
        private static ConcurrentDictionary<string, List<(DateTime, double)>> _metrics;

        static Metrics()
        {
            _metrics = new ConcurrentDictionary<string, List<(DateTime, double)>>();
        }

        public static void LogMetric(string name, double value)
        {
            var list = GetMetrics(name);

            lock (list)
            {
                list.Add((DateTime.Now, value));
            }
        }

        public static void LogMetric(string name, double value, TimeSpan keepTime)
        {
            var list = GetMetrics(name);

            lock (list)
            {
                list.Add((DateTime.Now, value));
                var minTime = DateTime.Now - keepTime;
                _metrics[name] = list.Where(t => t.Item1 > minTime).ToList();
            }
        }

        public static void LogMetric(string name, double value, int keepNum)
        {
            var list = GetMetrics(name);

            lock (list)
            {
                list.Add((DateTime.Now, value));
                if(list.Count > keepNum)
                {
                    var skip = list.Count - keepNum;
                    _metrics[name] = list.Skip(skip).ToList();
                }
            }
        }

        public static IEnumerable<string> ListMetrics()
        {
            return _metrics.Keys;
        }

        public static List<(DateTime, double)> GetMetrics(string name)
        {
            if (!_metrics.ContainsKey(name))
            {
                _metrics[name] = new List<(DateTime, double)>();
            }

            return _metrics[name];
        }
    }
}
