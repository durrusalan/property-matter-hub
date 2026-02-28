namespace PropertyMatterHub.Core.Models;

public class EmailRecord
{
    public int Id { get; set; }
    public string GmailMessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? Snippet { get; set; }
    public DateTime SentAt { get; set; }
    public EmailDirection Direction { get; set; }
    public EmailClassificationStatus ClassificationStatus { get; set; } = EmailClassificationStatus.Unclassified;
    public float ClassificationConfidence { get; set; }
    public string? AttachmentsJson { get; set; }    // JSON array of attachment metadata
    public bool IsSavedToZDrive { get; set; }

    public int? MatterId { get; set; }
    public Matter? Matter { get; set; }

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}

public enum EmailDirection
{
    Inbound,
    Outbound
}

public enum EmailClassificationStatus
{
    Unclassified,
    AutoClassified,
    ManuallyClassified,
    NeedsReview
}
