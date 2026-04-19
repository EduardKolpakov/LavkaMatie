using Avalonia.Controls;
using Avalonia.Interactivity;
using KLALIK.Data;
using KLALIK.Models.Entities;
using KLALIK.Services;
using Microsoft.EntityFrameworkCore;

namespace KLALIK.Views;

public partial class RegisterView : UserControl
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthSession _authSession;
    private readonly Action _onSuccess;
    private readonly Action _goToLogin;

    public RegisterView(IDbContextFactory<AppDbContext> dbFactory, AuthSession authSession, Action onSuccess,
        Action goToLogin)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _authSession = authSession;
        _onSuccess = onSuccess;
        _goToLogin = goToLogin;
        RegisterButton.Click += RegisterButton_OnClick;
        GoToLoginButton.Click += (_, _) => _goToLogin();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    private void ClearError()
    {
        ErrorText.Text = string.Empty;
        ErrorText.IsVisible = false;
    }

    private async void RegisterButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearError();
        var displayName = DisplayNameBox.Text?.Trim() ?? string.Empty;
        var email = EmailBox.Text?.Trim() ?? string.Empty;
        var password = PasswordBox.Text ?? string.Empty;
        if (displayName.Length == 0 || email.Length == 0 || password.Length < 6)
        {
            ShowError("Заполните имя, email и пароль (не короче 6 символов).");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            ShowError("Пользователь с таким email уже существует.");
            return;
        }

        var clientRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Client);
        var user = new AppUser
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = clientRole.Id,
            Balance = 0,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        _authSession.SetUser(user.Id, user.DisplayName, RoleNames.Client, null);
        _onSuccess();
    }
}
