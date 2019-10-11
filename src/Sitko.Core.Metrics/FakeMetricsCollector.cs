using System.Collections.Generic;

namespace Sitko.Core.Metrics
{
    public class FakeMetricsCollector : IMetricsCollector
    {
        public void Meter(string name, int value = 1, Dictionary<string, string> tags = null, string context = null)
        {
        }

        public void Histogram(string name, long value, Dictionary<string, string> tags = null, string context = null)
        {
        }
    }
}
