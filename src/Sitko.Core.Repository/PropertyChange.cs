namespace Sitko.Core.Repository;

public class EntityChange
{
    private readonly List<PropertyChange> changes;

    public EntityChange(IEntity entity)
    {
        changes = new List<PropertyChange>();
        Entity = entity;
    }

    public IEntity Entity { get; }
    public PropertyChange[] Changes => changes.ToArray();

    public void AddChange(string name, object? originalValue, object? currentValue, ChangeType changeType) =>
        changes.Add(new PropertyChange(name, originalValue, currentValue, changeType));
}

public struct PropertyChange
{
    public PropertyChange(string name, object? originalValue, object? currentValue, ChangeType changeType)
    {
        Name = name;
        OriginalValue = originalValue;
        CurrentValue = currentValue;
        ChangeType = changeType;
    }

    public string Name { get; }
    public object? OriginalValue { get; }
    public object? CurrentValue { get; }
    public ChangeType ChangeType { get; }
}

public enum ChangeType
{
    Added,
    Modified,
    Deleted
}

