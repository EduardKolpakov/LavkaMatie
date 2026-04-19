using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using KLALIK.Data;
using KLALIK.Helpers;
using KLALIK.Models.Entities;
using KLALIK.Models.Enums;
using KLALIK.Services;
using Microsoft.EntityFrameworkCore;

namespace KLALIK.Views;

public partial class MasterDashboard : UserControl
{
    private readonly Window _hostWindow;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthSession _authSession;
    private readonly Action _logout;

    public MasterDashboard(Window hostWindow, IDbContextFactory<AppDbContext> dbFactory, AuthSession authSession,
        Action refreshShell, Action logout)
    {
        InitializeComponent();
        _hostWindow = hostWindow;
        _dbFactory = dbFactory;
        _authSession = authSession;
        _ = refreshShell;
        _logout = logout;
        LogoutButton.Click += (_, _) => _logout();
        SubmitQualificationButton.Click += async (_, _) => await SubmitQualificationAsync();
        Loaded += (_, _) => _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var masterId = _authSession.MasterProfileId;
        if (!masterId.HasValue)
        {
            HeaderText.Text = "Профиль мастера не найден";
            MasterReviewsPanel.Children.Clear();
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var profile = await db.MasterProfiles
            .AsNoTracking()
            .Include(m => m.QualificationLevel)
            .Include(m => m.User)
            .FirstAsync(m => m.Id == masterId.Value);
        HeaderText.Text =
            $"{profile.User.DisplayName} · уровень: {profile.QualificationLevel.Name}";

        var bookings = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Client)
            .Include(b => b.WorkshopService)
            .Where(b => b.MasterProfileId == masterId.Value)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync();
        BookingsList.Items.Clear();
        foreach (var b in bookings)
        {
            BookingsList.Items.Add(
                $"#{b.QueueNumber} · {b.WorkshopService.Name} · клиент {b.Client.DisplayName} · {b.Status} · {b.CreatedAtUtc:dd.MM.yyyy HH:mm}");
        }

        var services = await db.MasterServiceLinks
            .AsNoTracking()
            .Include(l => l.WorkshopService)
            .Where(l => l.MasterProfileId == masterId.Value)
            .OrderBy(l => l.WorkshopService.Name)
            .ToListAsync();
        ServicesList.Items.Clear();
        foreach (var l in services)
            ServicesList.Items.Add(l.WorkshopService.Name);

        await RenderMasterReviewsAsync(db, masterId.Value);

        var pending = await db.QualificationRequests
            .AsNoTracking()
            .Where(q => q.MasterProfileId == masterId.Value)
            .OrderByDescending(q => q.CreatedAtUtc)
            .FirstOrDefaultAsync();
        QualificationStatus.Text = pending == null
            ? "Активных заявок нет."
            : $"Последняя заявка: {pending.Status} от {pending.CreatedAtUtc:dd.MM.yyyy}";
    }

    private async Task RenderMasterReviewsAsync(AppDbContext db, int masterProfileId)
    {
        MasterReviewsPanel.Children.Clear();
        var reviews = await db.Reviews
            .AsNoTracking()
            .Include(r => r.Client)
            .Where(r => r.MasterProfileId == masterProfileId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        if (reviews.Count == 0)
        {
            MasterReviewsPanel.Children.Add(new TextBlock
            {
                Text = "Об вас пока нет отзывов.",
                Foreground = new SolidColorBrush(Color.Parse("#FFA6ADC8"))
            });
            return;
        }

        foreach (var r in reviews)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#FF313244")),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(12),
                Child = new StackPanel
                {
                    Spacing = 6,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"★{r.Rating}/5 · от {r.Client.DisplayName}",
                            Foreground = Brushes.White,
                            FontWeight = FontWeight.SemiBold
                        },
                        new TextBlock
                        {
                            Text = r.Comment,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(Color.Parse("#FFCDD6F4"))
                        },
                        new TextBlock
                        {
                            Text = r.CreatedAtUtc.ToString("dd.MM.yyyy HH:mm"),
                            Foreground = new SolidColorBrush(Color.Parse("#FF6C7086")),
                            FontSize = 12
                        }
                    }
                }
            };
            MasterReviewsPanel.Children.Add(card);
        }
    }

    private async Task SubmitQualificationAsync()
    {
        var masterId = _authSession.MasterProfileId;
        if (!masterId.HasValue)
            return;

        var note = QualificationNote.Text?.Trim() ?? string.Empty;
        if (note.Length < 5)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Опишите заявку подробнее (от 5 символов).", "Заявка");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.QualificationRequests.Add(new QualificationRequest
        {
            MasterProfileId = masterId.Value,
            Status = QualificationRequestStatus.Pending,
            Note = note,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        QualificationNote.Text = string.Empty;
        await DialogHelper.AlertAsync(_hostWindow, "Заявка отправлена модератору.", "Готово");
        await LoadAsync();
    }
}
