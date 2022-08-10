using System.Collections.Generic;
using Newtonsoft.Json;
using Sitko.Core.App.Collections;

namespace Sitko.Core.Repository;

public record QueryContextConditionsGroup
{
    [JsonConstructor]
    public QueryContextConditionsGroup(List<QueryContextCondition> conditions) =>
        Conditions = new ValueCollection<QueryContextCondition>(conditions);

    public QueryContextConditionsGroup(params QueryContextCondition[] conditions) =>
        Conditions = new ValueCollection<QueryContextCondition>(conditions);

    public QueryContextConditionsGroup(QueryContextCondition condition) =>
        Conditions = new ValueCollection<QueryContextCondition>(new List<QueryContextCondition> { condition });

    public ValueCollection<QueryContextCondition> Conditions { get; }
}
