using System.Collections.Generic;

namespace Sitko.Core.Repository
{
    public class QueryContextConditionsGroup
    {
        public QueryContextConditionsGroup(List<QueryContextCondition> conditions)
        {
            Conditions = conditions;
        }

        public List<QueryContextCondition> Conditions { get; }
    }
}