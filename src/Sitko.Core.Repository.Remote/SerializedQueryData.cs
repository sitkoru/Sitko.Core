namespace Sitko.Core.Repository.Remote;

public record SerializedQueryData
{
    public List<string> Where { get; set; } = new();
    public List<string> WhereByString { get; set; } = new();
    public List<string> OrderBy { get; set; } = new();
    public List<string> OrderByDescending { get; set; } = new();
    public List<string> Includes { get; set; } = new();
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public string? SelectExpressionString { get; set; }
}
