using Avalonia.Controls;
using Avalonia.Input;
using KLALIK.Data;
using KLALIK.Helpers;
using KLALIK.Services;
using KLALIK.Views;
using Microsoft.EntityFrameworkCore;

namespace KLALIK;

public partial class MainWindow : Window
{
    private readonly AuthSession _authSession;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private bool _forceClose;

    public MainWindow(AuthSession authSession, IDbContextFactory<AppDbContext> dbFactory)
    {
        InitializeComponent();
        _authSession = authSession;
        _dbFactory = dbFactory;
        RefreshShell();
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void MinimizeButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_forceClose)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        _ = CloseWithConfirmAsync();
    }

    private async Task CloseWithConfirmAsync()
    {
        var ok = await DialogHelper.ConfirmAsync(this, "Закрыть приложение?", "Выход");
        if (!ok)
            return;
        _forceClose = true;
        Close();
    }

    public void RefreshShell()
    {
        SubtitleText.Text = _authSession.IsAuthenticated
            ? $"{_authSession.DisplayName} · {_authSession.RoleName}"
            : "Войдите или зарегистрируйтесь";

        if (!_authSession.IsAuthenticated)
        {
            ShowLogin();
            return;
        }

        ShellHost.Content = _authSession.RoleName switch
        {
            RoleNames.Client => new ClientDashboard(this, _dbFactory, _authSession, RefreshShell, Logout),
            RoleNames.Master => new MasterDashboard(this, _dbFactory, _authSession, RefreshShell, Logout),
            RoleNames.Moderator => new ModeratorDashboard(this, _dbFactory, _authSession, RefreshShell, Logout),
            RoleNames.Administrator => new AdminDashboard(this, _dbFactory, _authSession, RefreshShell, Logout),
            _ => null
        };
        if (ShellHost.Content == null)
        {
            _authSession.Clear();
            ShowLogin();
        }
    }

    private void ShowLogin()
    {
        SubtitleText.Text = "Войдите или зарегистрируйтесь";
        ShellHost.Content = new LoginView(_dbFactory, _authSession, RefreshShell, ShowRegister);
    }

    private void ShowRegister()
    {
        SubtitleText.Text = "Создание аккаунта";
        ShellHost.Content = new RegisterView(_dbFactory, _authSession, RefreshShell, ShowLogin);
    }

    private void Logout()
    {
        _authSession.Clear();
        RefreshShell();
    }
}
