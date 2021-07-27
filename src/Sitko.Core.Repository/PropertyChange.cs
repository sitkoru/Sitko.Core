namespace Sitko.Core.Repository
{
    public struct PropertyChange
    {
        public PropertyChange(string name, object? originalValue, object? currentValue)
        {
            Name = name;
            OriginalValue = originalValue;
            CurrentValue = currentValue;
        }

        public string Name { get; }
        public object? OriginalValue { get; }
        public object? CurrentValue { get; }
    }
}
