namespace Sitko.Core.Repository.EntityFrameworkCore.Tests.Data.TPH;

public class SecondTPHClass : BaseTPHClass<SecondTPHClassConfig>
{
    public string Baz { get; set; } = "";
}
