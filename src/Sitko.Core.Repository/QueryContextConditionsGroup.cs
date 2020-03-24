using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sitko.Core.Repository
{
    public class QueryContextConditionsGroup
    {
        [JsonConstructor]
        public QueryContextConditionsGroup(List<QueryContextCondition> conditions)
        {
            Conditions = conditions;
        }

        public QueryContextConditionsGroup(QueryContextCondition condition)
        {
            Conditions = new List<QueryContextCondition> {condition};
        }

        public List<QueryContextCondition> Conditions { get; }
    }
}
