namespace OneSecurity.Server.Models.Enums
{
    public enum ResponseStatus
    {
        Pending,
        Queued,
        Executing,
        Succeeded,
        Completed = Succeeded,
        Failed,
        Cancelled,
        Timeout
    }
}
