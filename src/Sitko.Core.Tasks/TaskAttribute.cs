namespace Sitko.Core.Tasks;

[AttributeUsage(AttributeTargets.Class)]
public class TaskAttribute : Attribute
{
    public TaskAttribute(string key) => Key = key;
    public string Key { get; }
}