using Avalonia.Controls;
using Avalonia.Interactivity;
using KLALIK.Data;
using KLALIK.Models.Entities;
using KLALIK.Services;
using Microsoft.EntityFrameworkCore;

namespace KLALIK.Views;

public partial class LoginView : UserControl
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthSession _authSession;
    private readonly Action _onSuccess;
    private readonly Action _goToRegister;

    public LoginView(IDbContextFactory<AppDbContext> dbFactory, AuthSession authSession, Action onSuccess,
        Action goToRegister)
    {
        InitializeComponent();
        _dbFactory = dbFactory;
        _authSession = authSession;
        _onSuccess = onSuccess;
        _goToRegister = goToRegister;
        LoginButton.Click += LoginButton_OnClick;
        GoToRegisterButton.Click += (_, _) => _goToRegister();
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

    private async void LoginButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ClearError();
        var email = EmailBox.Text?.Trim() ?? string.Empty;
        var password = PasswordBox.Text ?? string.Empty;
        if (email.Length == 0 || password.Length == 0)
        {
            ShowError("Укажите email и пароль.");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.Include(u => u.Role).Include(u => u.MasterProfile)
            .FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ShowError("Неверный email или пароль.");
            return;
        }

        _authSession.SetUser(user.Id, user.DisplayName, user.Role.Name, user.MasterProfile?.Id);
        _onSuccess();
    }
}
