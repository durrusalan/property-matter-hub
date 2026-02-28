using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Services;

public interface IClassificationRule
{
    ClassificationResult Evaluate(EmailRecord email, IReadOnlyList<Matter> matters, IReadOnlyList<Client> clients);
}

public record ClassificationResult(int? MatterId, float Confidence, string Reason);

public class MatterRefRule : IClassificationRule
{
    // Matches PROP-2026-0042 style references in subject or body
    private static readonly System.Text.RegularExpressions.Regex MatterRefPattern =
        new(@"PROP-\d{4}-\d{4}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public ClassificationResult Evaluate(EmailRecord email, IReadOnlyList<Matter> matters, IReadOnlyList<Client> clients)
    {
        var text = $"{email.Subject} {email.Body} {email.Snippet}";
        var match = MatterRefPattern.Match(text);
        if (!match.Success) return new ClassificationResult(null, 0f, "No matter reference found");

        var matched = matters.FirstOrDefault(m =>
            string.Equals(m.MatterRef, match.Value, StringComparison.OrdinalIgnoreCase));

        return matched is not null
            ? new ClassificationResult(matched.Id, 0.95f, $"Matter reference '{match.Value}' found in email text")
            : new ClassificationResult(null, 0f, $"Reference '{match.Value}' not in database");
    }
}

public class ClientEmailRule : IClassificationRule
{
    public ClassificationResult Evaluate(EmailRecord email, IReadOnlyList<Matter> matters, IReadOnlyList<Client> clients)
    {
        var senderEmail = email.From?.ToLowerInvariant() ?? string.Empty;
        var client = clients.FirstOrDefault(c =>
            !string.IsNullOrEmpty(c.Email) &&
            senderEmail.Contains(c.Email.ToLowerInvariant()));

        if (client is null) return new ClassificationResult(null, 0f, "No client email match");

        var matter = matters
            .Where(m => m.ClientId == client.Id && m.Status == MatterStatus.Active)
            .OrderByDescending(m => m.UpdatedAt)
            .FirstOrDefault();

        return matter is not null
            ? new ClassificationResult(matter.Id, 0.75f, $"Sender matches client '{client.Name}'")
            : new ClassificationResult(null, 0.3f, $"Client '{client.Name}' found but no active matter");
    }
}

public class EmailClassificationService
{
    private readonly IReadOnlyList<IClassificationRule> _rules;

    public EmailClassificationService(IEnumerable<IClassificationRule>? rules = null)
    {
        _rules = rules?.ToList() ?? new List<IClassificationRule>
        {
            new MatterRefRule(),
            new ClientEmailRule()
        };
    }

    public ClassificationResult Classify(EmailRecord email, IReadOnlyList<Matter> matters, IReadOnlyList<Client> clients)
    {
        ClassificationResult best = new(null, 0f, "No rules matched");

        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(email, matters, clients);
            if (result.Confidence > best.Confidence)
                best = result;
        }

        return best;
    }

    public EmailClassificationStatus DetermineStatus(float confidence) => confidence switch
    {
        >= 0.8f => EmailClassificationStatus.AutoClassified,
        >= 0.5f => EmailClassificationStatus.NeedsReview,
        _ => EmailClassificationStatus.Unclassified
    };
}
