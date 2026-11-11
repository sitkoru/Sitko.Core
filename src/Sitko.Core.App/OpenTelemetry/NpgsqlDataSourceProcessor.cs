using System.Diagnostics;
using OpenTelemetry;

namespace Sitko.Core.App.OpenTelemetry
{
    internal sealed class NpgsqlDataSourceProcessor : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity activity)
        {
            if (activity.Source.Name == "Npgsql" &&
                activity.Duration < TimeSpan.FromMilliseconds(100) &&
                activity.Status != ActivityStatusCode.Error &&
                !activity.Events.Any(static activityEvent => activityEvent.Name == "exception") &&
                activity.GetTagItem("db.system.name") is "postgresql" &&
                activity.GetTagItem("server.address") is string { Length: > 0 } &&
                activity.GetTagItem("db.namespace") is string { Length: > 0 } &&
                activity.GetTagItem("db.query.text") is string { Length: > 0 })
            {
                _ = activity.SetTag("db.npgsql.data_source", null);
            }
        }
    }
}
