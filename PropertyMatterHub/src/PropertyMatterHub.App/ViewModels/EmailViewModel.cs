using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;

namespace PropertyMatterHub.App.ViewModels;

public partial class EmailViewModel : ObservableObject
{
    // Emails with confidence >= this threshold are auto-classified silently.
    private const float AutoClassifyThreshold = 0.8f;

    private readonly IEmailService _email;
    private readonly IMatterRepository _matters;
    private readonly IClientRepository _clients;
    private readonly EmailClassificationService _classifier;

    // Cached after each fetch so suggestion lookups don't hit the DB again.
    private IReadOnlyList<Matter> _cachedMatters = [];
    private IReadOnlyList<Client> _cachedClients = [];

    [ObservableProperty] private IReadOnlyList<EmailRecord> _unclassifiedEmails = [];
    [ObservableProperty] private IReadOnlyList<EmailRecord> _needsReviewEmails  = [];
    [ObservableProperty] private IReadOnlyList<Matter>      _availableMatters   = [];
    [ObservableProperty] private EmailRecord? _selectedEmail;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isComposing;

    // Suggestion shown when an email is selected
    [ObservableProperty] private int?   _suggestedMatterId;
    [ObservableProperty] private string _suggestionReason     = string.Empty;
    [ObservableProperty] private float  _suggestionConfidence;

    // Compose fields
    [ObservableProperty] private string _composeTo      = string.Empty;
    [ObservableProperty] private string _composeSubject = string.Empty;
    [ObservableProperty] private string _composeBody    = string.Empty;
    [ObservableProperty] private int?   _composeMatterId;

    public EmailViewModel(
        IEmailService email,
        IMatterRepository matters,
        IClientRepository clients,
        EmailClassificationService classifier)
    {
        _email      = email;
        _matters    = matters;
        _clients    = clients;
        _classifier = classifier;
    }

    [RelayCommand]
    public async Task FetchAndClassifyAsync()
    {
        IsLoading = true;
        try
        {
            await RefreshContextCacheAsync();
            await AutoClassifyNewEmailsAsync();
            await RefreshEmailQueuesAsync();
        }
        finally { IsLoading = false; }
    }

    /// <summary>
    /// Computes and displays a classification suggestion for the given email
    /// without persisting anything. Uses the cached matter/client lists so no
    /// extra DB round-trips occur after a recent fetch.
    /// </summary>
    public async Task LoadSuggestionForEmailAsync(EmailRecord email)
    {
        // Ensure cache is warm (handles the case where suggestion is loaded
        // before the first full fetch).
        if (_cachedMatters.Count == 0)
            await RefreshContextCacheAsync();

        var result = _classifier.Classify(email, _cachedMatters, _cachedClients);

        SuggestedMatterId    = result.MatterId;
        SuggestionReason     = result.Reason;
        SuggestionConfidence = result.Confidence;
    }

    [RelayCommand]
    private async Task ClassifyAsync(int matterId)
    {
        if (SelectedEmail is null) return;
        await _email.ClassifyEmailAsync(SelectedEmail.Id, matterId);
        await FetchAndClassifyAsync();
    }

    [RelayCommand]
    private async Task ApproveSuggestionAsync()
    {
        if (SelectedEmail is null || !SuggestedMatterId.HasValue) return;
        await _email.ClassifyEmailAsync(SelectedEmail.Id, SuggestedMatterId.Value);
        await FetchAndClassifyAsync();
    }

    [RelayCommand]
    private void Compose() => IsComposing = true;

    [RelayCommand]
    private void CancelCompose() { IsComposing = false; ResetCompose(); }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(ComposeTo)) return;
        await _email.SendEmailAsync(ComposeTo, ComposeSubject, ComposeBody,
            ComposeMatterId?.ToString());
        IsComposing = false;
        ResetCompose();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task RefreshContextCacheAsync()
    {
        _cachedMatters   = await _matters.GetActiveAsync();
        _cachedClients   = await _clients.GetAllAsync();
        AvailableMatters = _cachedMatters;
    }

    private async Task AutoClassifyNewEmailsAsync()
    {
        var newEmails = await _email.FetchNewEmailsAsync();
        foreach (var email in newEmails)
        {
            var result = _classifier.Classify(email, _cachedMatters, _cachedClients);
            if (result.Confidence >= AutoClassifyThreshold && result.MatterId.HasValue)
                await _email.ClassifyEmailAsync(email.Id, result.MatterId.Value);
        }
    }

    private async Task RefreshEmailQueuesAsync()
    {
        UnclassifiedEmails = await _email.GetUnclassifiedEmailsAsync();
        NeedsReviewEmails  = await _email.GetNeedsReviewEmailsAsync();
    }

    private void ResetCompose()
    {
        ComposeTo       = string.Empty;
        ComposeSubject  = string.Empty;
        ComposeBody     = string.Empty;
        ComposeMatterId = null;
    }
}
