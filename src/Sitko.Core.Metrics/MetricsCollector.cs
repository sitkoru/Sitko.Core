using System;
using System.Collections.Generic;
using System.Linq;
using App.Metrics;
using App.Metrics.Histogram;
using App.Metrics.Meter;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Metrics
{
    public interface IMetricsCollector
    {
        void Meter(string name, int value = 1, Dictionary<string, string> tags = null, string context = null);
        void Histogram(string name, long value, Dictionary<string, string> tags = null, string context = null);

    }

    internal class MetricsCollector : IMetricsCollector
    {
        private readonly IMetrics _metrics;

        public MetricsCollector(IServiceProvider serviceProvider)
        {
            _metrics = serviceProvider.GetService<IMetrics>();
        }

        public void Meter(string name, int value = 1, Dictionary<string, string> tags = null, string context = null)
        {
            var options = new MeterOptions
            {
                Name = name
            };
            if (!string.IsNullOrEmpty(context))
            {
                options.Context = context;
            }

            _metrics?.Measure.Meter.Mark(options, BuildTags(tags), value);
        }

        public void Histogram(string name, long value, Dictionary<string, string> tags = null, string context = null)
        {
            var options = new HistogramOptions
            {
                Name = name
            };
            if (!string.IsNullOrEmpty(context))
            {
                options.Context = context;
            }

            _metrics?.Measure.Histogram.Update(options,
                BuildTags(tags), value);
        }

        private static MetricTags BuildTags(Dictionary<string, string> tags)
        {
            return tags != null ? new MetricTags(tags.Keys.ToArray(), tags.Values.ToArray()) : new MetricTags();
        }
    }
}
