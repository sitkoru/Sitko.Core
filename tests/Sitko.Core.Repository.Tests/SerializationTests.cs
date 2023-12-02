using Newtonsoft.Json;
using Serilog;
using Xunit;

namespace Sitko.Core.Repository.Tests;

public class SerializationTests
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto,
        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
        Error = (_, e) =>
        {
            Log.Logger.Error(e.ErrorContext.Error, "Error deserializing json content: {ErrorText}",
                e.ErrorContext.Error.ToString());
            e.ErrorContext.Handled = true;
        }
    };

    [Fact]
    public void DeserializeConditionsGroup()
    {
        var json = "[{\"conditions\":[{\"property\":\"projectId\",\"operator\":1,\"value\":1}]}]";

        var where = JsonConvert.DeserializeObject<List<QueryContextConditionsGroup>>(json);

        Assert.NotNull(where);
        Assert.NotEmpty(where);
        Assert.Single(where);
        Assert.Equal("projectId", where.First().Conditions.First().Property);
        Assert.Equal(QueryContextOperator.Equal, where.First().Conditions.First().Operator);
        Assert.Equal(1L, where.First().Conditions.First().Value);
    }

    [Fact]
    public void Metadata()
    {
        var model = new Model { SubModels = new List<SubModel> { new SubModelA(), new SubModelB() } };
        var json = Serialize(model);
        var deserialized = Deserialize<Model>(json);
        Assert.NotNull(deserialized!.SubModels);
        Assert.NotEmpty(deserialized.SubModels);
        var modifiedJson = json.Replace("Sitko.Core.Repository.Tests.SubModelA",
            "Sitko.Core.Repository.Tests.SubModelC");
        var modifiedDeserialized = Deserialize<Model>(modifiedJson);
        Assert.NotNull(modifiedDeserialized!.SubModels);
        Assert.NotEmpty(modifiedDeserialized.SubModels);
    }

    private static string Serialize(object obj) => JsonConvert.SerializeObject(obj, JsonSettings);

    private static T? Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, JsonSettings);
}

public class Model
{
    public List<SubModel> SubModels { get; set; } = new();
}

public abstract class SubModel;

public class SubModelA : SubModel;

public class SubModelB : SubModel;

