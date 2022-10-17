using System.Text;
using System.Web;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sitko.Core.Grpc;

namespace Sitko.Core.Repository.Grpc;

[PublicAPI]
public static class ApiRequestInfoExtensions
{
    public static ApiRequestInfo SetFilter(this ApiRequestInfo apiRequestInfo,
        IEnumerable<QueryContextConditionsGroup> groups)
    {
        var queryConditionsGroup = JsonConvert.SerializeObject(groups,
            new JsonSerializerSettings { Converters = { new SimpleTypeConverter() } });
        var queryBytes = Encoding.UTF8.GetBytes(queryConditionsGroup);
        apiRequestInfo.Filter = Convert.ToBase64String(queryBytes);
        return apiRequestInfo;
    }

    public static ApiRequestInfo SetFilter(this ApiRequestInfo apiRequestInfo, QueryContextConditionsGroup group) =>
        apiRequestInfo.SetFilter(new[] { group });

    public static ApiRequestInfo SetFilter(this ApiRequestInfo apiRequestInfo, QueryContextCondition condition) =>
        apiRequestInfo.SetFilter(new[] { new QueryContextConditionsGroup(condition) });

    public static ApiRequestInfo SetFilter(this ApiRequestInfo apiRequestInfo,
        string property, QueryContextOperator @operator, object? value,
        Type? valueType = null) => apiRequestInfo.SetFilter(new[]
    {
        new QueryContextConditionsGroup(new QueryContextCondition(property, @operator, value, valueType))
    });

    public static ApiRequestInfo SetFilter(this ApiRequestInfo apiRequestInfo,
        IEnumerable<QueryContextCondition> conditions) =>
        apiRequestInfo.SetFilter(new[] { new QueryContextConditionsGroup(conditions.ToList()) });

    public static IGrpcRequest SetFilter(this IGrpcRequest grpcRequest,
        IEnumerable<QueryContextConditionsGroup> groups)
    {
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        grpcRequest.RequestInfo ??= new ApiRequestInfo();
        grpcRequest.RequestInfo.SetFilter(groups);
        return grpcRequest;
    }

    public static IGrpcRequest SetFilter(this IGrpcRequest grpcRequest, QueryContextConditionsGroup group) =>
        grpcRequest.SetFilter(new[] { group });

    public static IGrpcRequest SetFilter(this IGrpcRequest grpcRequest, QueryContextCondition condition) =>
        grpcRequest.SetFilter(new QueryContextConditionsGroup(condition));

    public static IGrpcRequest SetFilter(this IGrpcRequest grpcRequest,
        IEnumerable<QueryContextCondition> conditions) =>
        grpcRequest.SetFilter(new QueryContextConditionsGroup(conditions.ToList()));

    public static IGrpcRequest SetFilter(this IGrpcRequest grpcRequest,
        string property, QueryContextOperator @operator, object? value,
        Type? valueType = null) =>
        grpcRequest.SetFilter(new QueryContextCondition(property, @operator, value, valueType));

    public static QueryContextConditionsGroup[] GetFilter(this ApiRequestInfo apiRequestInfo)
    {
        if (!string.IsNullOrEmpty(apiRequestInfo.Filter) && apiRequestInfo.Filter != "null")
        {
            var mod4 = apiRequestInfo.Filter.Length % 4;
            if (mod4 > 0)
            {
                apiRequestInfo.Filter += new string('=', 4 - mod4);
            }

            var data = Convert.FromBase64String(apiRequestInfo.Filter);
            var decodedString = HttpUtility.UrlDecode(Encoding.UTF8.GetString(data));
            if (!string.IsNullOrEmpty(decodedString))
            {
                return JsonConvert.DeserializeObject<QueryContextConditionsGroup[]>(decodedString) ??
                       Array.Empty<QueryContextConditionsGroup>();
            }
        }

        return Array.Empty<QueryContextConditionsGroup>();
    }

    public static QueryContextConditionsGroup[] GetFilter(this IGrpcRequest grpcRequest) =>
        grpcRequest.RequestInfo.GetFilter();
}
