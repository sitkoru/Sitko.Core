namespace Sitko.Core.Email;

public class MailEntry<T> : MailEntry
{
    public MailEntry(string subject, IEnumerable<string> recipients, T message) : base(subject, recipients) =>
        Message = message;

    public T Message { get; }
}

public class MailEntry
{
    public MailEntry(string subject, IEnumerable<string> recipients)
    {
        Subject = subject;
        Recipients = recipients.ToArray();
    }

    public string Subject { get; }
    public string[] Recipients { get; }
    public List<MailEntryAttachment> Attachments { get; } = new();
}

public class MailEntryAttachment
{
    public MailEntryAttachment(string name, string type, Stream data)
    {
        Name = name;
        Type = type;
        Data = data;
    }

    public string Name { get; }
    public string Type { get; }
    public Stream Data { get; }
}

