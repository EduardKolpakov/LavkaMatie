using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using KLALIK.Data;
using KLALIK.Helpers;
using KLALIK.Models.Entities;
using KLALIK.Services;
using Microsoft.EntityFrameworkCore;

namespace KLALIK.Views;

public partial class AdminDashboard : UserControl
{
    private readonly Window _hostWindow;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthSession _authSession;
    private readonly Action _logout;

    public AdminDashboard(Window hostWindow, IDbContextFactory<AppDbContext> dbFactory, AuthSession authSession,
        Action refreshShell, Action logout)
    {
        InitializeComponent();
        _hostWindow = hostWindow;
        _dbFactory = dbFactory;
        _authSession = authSession;
        _ = refreshShell;
        _logout = logout;
        LogoutButton.Click += (_, _) => _logout();
        AddEmployeeButton.Click += async (_, _) => await AddEmployeeAsync();
        Loaded += (_, _) => _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var users = await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.Email)
            .ToListAsync();
        var roles = await db.Roles.OrderBy(r => r.Name).ToListAsync();

        UsersList.Items.Clear();
        foreach (var u in users)
        {
            var roleCombo = new ComboBox { MinWidth = 160, Tag = u.Id };
            foreach (var r in roles)
            {
                roleCombo.Items.Add(new ComboBoxItem { Content = r.Name, Tag = r.Id });
                if (r.Id == u.RoleId)
                    roleCombo.SelectedIndex = roleCombo.Items.Count - 1;
            }

            var save = new Button { Content = "Сохранить роль", Margin = new Avalonia.Thickness(12, 0, 0, 0) };
            save.Click += async (_, _) => await SaveUserRoleAsync(u.Id, roleCombo);

            var emailTb = new TextBlock
            {
                Text = u.Email,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            var nameTb = new TextBlock
            {
                Text = u.DisplayName,
                Foreground = new SolidColorBrush(Color.Parse("#FFA6ADC8")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(12, 0, 0, 0)
            };

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("220,*,160,Auto"),
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            row.Children.Add(emailTb);
            row.Children.Add(nameTb);
            row.Children.Add(roleCombo);
            row.Children.Add(save);
            Grid.SetColumn(nameTb, 1);
            Grid.SetColumn(roleCombo, 2);
            Grid.SetColumn(save, 3);
            UsersList.Items.Add(row);
        }
    }

    private async Task SaveUserRoleAsync(int userId, ComboBox roleCombo)
    {
        if (roleCombo.SelectedItem is not ComboBoxItem item || item.Tag is not int roleId)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.Include(u => u.MasterProfile).FirstAsync(u => u.Id == userId);
        user.RoleId = roleId;

        var newRole = await db.Roles.FirstAsync(r => r.Id == roleId);
        if (newRole.Name == RoleNames.Master && user.MasterProfile == null)
        {
            var level = await db.QualificationLevels.OrderBy(l => l.SortOrder).FirstAsync();
            db.MasterProfiles.Add(new MasterProfile
            {
                UserId = user.Id,
                QualificationLevelId = level.Id
            });
        }

        if (newRole.Name != RoleNames.Master && user.MasterProfile != null)
        {
            var profileId = user.MasterProfile.Id;
            var requests = await db.QualificationRequests.Where(q => q.MasterProfileId == profileId).ToListAsync();
            db.QualificationRequests.RemoveRange(requests);
            var reviews = await db.Reviews.Where(r => r.MasterProfileId == profileId).ToListAsync();
            db.Reviews.RemoveRange(reviews);
            var links = await db.MasterServiceLinks.Where(l => l.MasterProfileId == profileId).ToListAsync();
            db.MasterServiceLinks.RemoveRange(links);
            var bookings = await db.Bookings.Where(b => b.MasterProfileId == profileId).ToListAsync();
            db.Bookings.RemoveRange(bookings);
            db.MasterProfiles.Remove(user.MasterProfile);
        }

        await db.SaveChangesAsync();
        await DialogHelper.AlertAsync(_hostWindow, "Роль пользователя обновлена.", "Готово");
        await LoadAsync();
    }

    private async Task AddEmployeeAsync()
    {
        var email = await DialogHelper.PromptAsync(_hostWindow, "Сотрудник", "Email", "employee@matye.local");
        if (string.IsNullOrWhiteSpace(email))
            return;
        var name = await DialogHelper.PromptAsync(_hostWindow, "Сотрудник", "Отображаемое имя", "Новый сотрудник");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var password = await DialogHelper.PromptAsync(_hostWindow, "Сотрудник", "Временный пароль", "ChangeMe123!");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Пароль не короче 6 символов.", "Ошибка");
            return;
        }

        var rolePick = new Window
        {
            Title = "Роль сотрудника",
            Width = 360,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var combo = new ComboBox { Margin = new Avalonia.Thickness(16, 12, 16, 0) };
        combo.Items.Add(new ComboBoxItem { Content = RoleNames.Master, Tag = RoleNames.Master });
        combo.Items.Add(new ComboBoxItem { Content = RoleNames.Moderator, Tag = RoleNames.Moderator });
        combo.Items.Add(new ComboBoxItem { Content = RoleNames.Administrator, Tag = RoleNames.Administrator });
        combo.SelectedIndex = 0;
        var ok = new Button { Content = "Создать", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Avalonia.Thickness(16) };
        string? chosenRole = null;
        ok.Click += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is string rn)
                chosenRole = rn;
            rolePick.Close();
        };
        rolePick.Content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "Выберите роль", Margin = new Avalonia.Thickness(16, 12, 16, 0) },
                combo,
                ok
            }
        };
        await rolePick.ShowDialog(_hostWindow);
        if (chosenRole == null)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Email == email.Trim()))
        {
            await DialogHelper.AlertAsync(_hostWindow, "Пользователь с таким email уже есть.", "Ошибка");
            return;
        }

        var roleEntity = await db.Roles.FirstAsync(r => r.Name == chosenRole);
        var user = new AppUser
        {
            Email = email.Trim(),
            DisplayName = name.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = roleEntity.Id,
            Balance = 0,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        if (chosenRole == RoleNames.Master)
        {
            var level = await db.QualificationLevels.OrderBy(l => l.SortOrder).FirstAsync();
            db.MasterProfiles.Add(new MasterProfile
            {
                UserId = user.Id,
                QualificationLevelId = level.Id
            });
            await db.SaveChangesAsync();
        }

        await DialogHelper.AlertAsync(_hostWindow, "Сотрудник создан.", "Готово");
        await LoadAsync();
    }
}
