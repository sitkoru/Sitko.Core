namespace Sitko.Core.Tasks;

public enum TaskStatus
{
    Wait = 0,
    InProgress = 1,
    SuccessWithWarnings = 2,
    Success = 3,
    Fails = 4
}