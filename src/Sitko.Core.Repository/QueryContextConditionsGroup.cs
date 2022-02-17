using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Sitko.Core.Repository;

public class QueryContextConditionsGroup
{
    [JsonConstructor]
    public QueryContextConditionsGroup(List<QueryContextCondition> conditions) => Conditions = conditions;

    public QueryContextConditionsGroup(params QueryContextCondition[] conditions) =>
        Conditions = conditions.ToList();

    public QueryContextConditionsGroup(QueryContextCondition condition) =>
        Conditions = new List<QueryContextCondition> { condition };

    public List<QueryContextCondition> Conditions { get; }
}
