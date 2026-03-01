using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.App.ViewModels;

public partial class EmailViewModel : ObservableObject
{
    private readonly IEmailService _email;

    [ObservableProperty] private IReadOnlyList<EmailRecord> _unclassifiedEmails = [];
    [ObservableProperty] private IReadOnlyList<EmailRecord> _allEmails = [];
    [ObservableProperty] private EmailRecord? _selectedEmail;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isComposing;

    // Compose fields
    [ObservableProperty] private string _composeTo = string.Empty;
    [ObservableProperty] private string _composeSubject = string.Empty;
    [ObservableProperty] private string _composeBody = string.Empty;
    [ObservableProperty] private int? _composeMatterId;

    public EmailViewModel(IEmailService email) => _email = email;

    [RelayCommand]
    public async Task FetchEmailsAsync()
    {
        IsLoading = true;
        try
        {
            UnclassifiedEmails = await _email.GetUnclassifiedEmailsAsync();
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ClassifyAsync(int matterId)
    {
        if (SelectedEmail is null) return;
        await _email.ClassifyEmailAsync(SelectedEmail.Id, matterId);
        await FetchEmailsAsync();
    }

    [RelayCommand]
    private void Compose() { IsComposing = true; }

    [RelayCommand]
    private void CancelCompose() { IsComposing = false; ResetCompose(); }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(ComposeTo)) return;
        await _email.SendEmailAsync(ComposeTo, ComposeSubject, ComposeBody);
        IsComposing = false;
        ResetCompose();
    }

    private void ResetCompose()
    {
        ComposeTo      = string.Empty;
        ComposeSubject = string.Empty;
        ComposeBody    = string.Empty;
        ComposeMatterId = null;
    }
}
