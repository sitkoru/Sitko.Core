using System.Collections.Generic;

namespace Sitko.Core.Repository
{
    public class QueryContextConditionsGroup
    {
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
