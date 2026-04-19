using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using KLALIK.Data;
using KLALIK.Helpers;
using KLALIK.Models.Entities;
using KLALIK.Models.Enums;
using KLALIK.Services;
using Microsoft.EntityFrameworkCore;

namespace KLALIK.Views;

public partial class ClientDashboard : UserControl
{
    private readonly Window _hostWindow;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthSession _authSession;
    private readonly Action _logout;
    private bool _catalogEventsHooked;

    public ClientDashboard(Window hostWindow, IDbContextFactory<AppDbContext> dbFactory, AuthSession authSession,
        Action refreshShell, Action logout)
    {
        InitializeComponent();
        _hostWindow = hostWindow;
        _dbFactory = dbFactory;
        _authSession = authSession;
        _ = refreshShell;
        _logout = logout;
        LogoutButton.Click += (_, _) => _logout();
        TopUpButton.Click += async (_, _) => await TopUpAsync();
        RefreshButton.Click += (_, _) => _ = LoadAsync();
        SubmitReviewButton.Click += async (_, _) => await SubmitReviewAsync();
        ReviewTargetType.SelectedIndex = 0;
        Loaded += (_, _) => _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var userId = _authSession.UserId!.Value;
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        BalanceText.Text = $"Баланс: {user.Balance:N2} ₽";

        await RepopulateDirectionFiltersAsync();

        if (!_catalogEventsHooked)
        {
            CosplayDirectionFilter.SelectionChanged += CosplayCatalog_OnChanged;
            CosplaySearchBox.TextChanged += CosplayCatalog_OnChanged;
            CustomDirectionFilter.SelectionChanged += CustomCatalog_OnChanged;
            CustomSearchBox.TextChanged += CustomCatalog_OnChanged;
            _catalogEventsHooked = true;
        }

        await RenderCosplayServicesAsync();
        await RenderCustomServicesAsync();
        await RenderBookingsAsync();
        await RenderMyReviewsAsync();
        ReviewTargetType.SelectionChanged -= ReviewTargetType_OnSelectionChanged;
        ReviewTargetType.SelectionChanged += ReviewTargetType_OnSelectionChanged;
        await SyncReviewPickerAsync();
    }

    private void CosplayCatalog_OnChanged(object? sender, EventArgs e) => _ = RenderCosplayServicesAsync();

    private void CustomCatalog_OnChanged(object? sender, EventArgs e) => _ = RenderCustomServicesAsync();

    private async Task RepopulateDirectionFiltersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var directions = await db.CollectionDirections.AsNoTracking().OrderBy(d => d.Name).ToListAsync();

        foreach (var combo in new[] { CosplayDirectionFilter, CustomDirectionFilter })
        {
            combo.Items.Clear();
            combo.Items.Add(new ComboBoxItem { Content = "Все коллекции", Tag = null });
            foreach (var d in directions)
                combo.Items.Add(new ComboBoxItem { Content = d.Name, Tag = d.Id });
            combo.SelectedIndex = 0;
        }
    }

    private void ReviewTargetType_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        _ = SyncReviewPickerAsync();

    private static int? GetSelectedDirectionId(ComboBox combo)
    {
        if (combo.SelectedItem is not ComboBoxItem item)
            return null;
        return item.Tag as int?;
    }

    private Task RenderCosplayServicesAsync() =>
        RenderCategoryServicesAsync(ServiceCategoryNames.Cosplay, CosplayServicesPanel, CosplaySearchBox,
            CosplayDirectionFilter);

    private Task RenderCustomServicesAsync() =>
        RenderCategoryServicesAsync(ServiceCategoryNames.Custom, CustomServicesPanel, CustomSearchBox,
            CustomDirectionFilter);

    private async Task RenderCategoryServicesAsync(string categoryName, StackPanel panel, TextBox searchBox,
        ComboBox directionCombo)
    {
        panel.Children.Clear();
        var directionId = GetSelectedDirectionId(directionCombo);
        var term = searchBox.Text?.Trim() ?? string.Empty;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.WorkshopServices
            .AsNoTracking()
            .Include(s => s.CollectionDirection)
            .Include(s => s.ServiceCategory)
            .Where(s => s.ServiceCategory.Name == categoryName);

        if (directionId.HasValue)
            query = query.Where(s => s.CollectionDirectionId == directionId.Value);

        if (term.Length > 0)
        {
            var t = term.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(t) || s.Description.ToLower().Contains(t));
        }

        var services = await query.OrderBy(s => s.Name).ToListAsync();
        foreach (var s in services)
        {
            var imageControl = BuildImage(s.ImageAssetPath);
            var details = BuildServiceDetails(s);
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("140,*"),
                ColumnSpacing = 14
            };
            grid.Children.Add(imageControl);
            Grid.SetColumn(imageControl, 0);
            grid.Children.Add(details);
            Grid.SetColumn(details, 1);

            var card = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#FF313244")),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(12),
                Child = grid
            };
            panel.Children.Add(card);
        }
    }

    private static Control BuildImage(string? path)
    {
        Bitmap? bmp = ImageLoader.TryLoadFromBaseDirectory(path);
        if (bmp == null)
        {
            return new Border
            {
                Width = 140,
                Height = 100,
                Background = new SolidColorBrush(Color.Parse("#FF45475A")),
                Child = new TextBlock
                {
                    Text = "Нет фото",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                }
            };
        }

        return new Image { Source = bmp, Width = 140, Height = 100, Stretch = Stretch.UniformToFill };
    }

    private Control BuildServiceDetails(WorkshopService s)
    {
        var book = new Button { Content = "Записаться", HorizontalAlignment = HorizontalAlignment.Left, Tag = s.Id };
        book.Click += async (_, _) => await BookAsync(s.Id);

        return new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = s.Name, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White },
                new TextBlock
                {
                    Text = $"{s.CollectionDirection.Name} · {s.ServiceCategory.Name}",
                    Foreground = new SolidColorBrush(Color.Parse("#FFA6ADC8"))
                },
                new TextBlock { Text = s.Description, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White },
                new TextBlock { Text = $"{s.Price:N0} ₽", Foreground = new SolidColorBrush(Color.Parse("#FFA6E3A1")) },
                book
            }
        };
    }

    private async Task BookAsync(int serviceId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var links = await db.MasterServiceLinks
            .AsNoTracking()
            .Include(l => l.MasterProfile)
            .ThenInclude(m => m!.User)
            .Where(l => l.WorkshopServiceId == serviceId)
            .ToListAsync();
        if (links.Count == 0)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Нет мастеров, привязанных к этой услуге. Обратитесь к модератору.",
                "Запись");
            return;
        }

        var dialog = new Window
        {
            Title = "Выбор мастера",
            Width = 420,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var combo = new ComboBox { PlaceholderText = "Мастер", MinHeight = 36 };
        foreach (var link in links)
            combo.Items.Add(new ComboBoxItem
            {
                Content = link.MasterProfile!.User.DisplayName,
                Tag = link.MasterProfileId
            });
        combo.SelectedIndex = 0;
        var ok = new Button { Content = "Записаться", HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 120 };
        int? chosenMasterId = null;
        ok.Click += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is int mid)
                chosenMasterId = mid;
            dialog.Close();
        };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = "Выберите мастера:" });
        panel.Children.Add(combo);
        panel.Children.Add(ok);
        dialog.Content = panel;

        await dialog.ShowDialog(_hostWindow);
        if (!chosenMasterId.HasValue)
            return;

        var clientId = _authSession.UserId!.Value;
        var user = await db.Users.FirstAsync(u => u.Id == clientId);
        var service = await db.WorkshopServices.FirstAsync(s => s.Id == serviceId);
        if (user.Balance < service.Price)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Недостаточно средств на балансе. Пополните баланс.", "Запись");
            return;
        }

        var today = DateTime.UtcNow.Date;
        var nextQueue = await db.Bookings
            .Where(b => b.CreatedAtUtc >= today)
            .Select(b => (int?)b.QueueNumber)
            .MaxAsync() ?? 0;

        user.Balance -= service.Price;
        db.BalanceTransactions.Add(new BalanceTransaction
        {
            UserId = user.Id,
            Amount = -service.Price,
            TransactionType = BalanceTransactionType.Payment,
            Note = $"Запись на «{service.Name}»",
            CreatedAtUtc = DateTime.UtcNow
        });

        db.Bookings.Add(new Booking
        {
            ClientUserId = clientId,
            MasterProfileId = chosenMasterId.Value,
            WorkshopServiceId = serviceId,
            QueueNumber = nextQueue + 1,
            Status = BookingStatus.Pending,
            ScheduledAtUtc = null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        await DialogHelper.AlertAsync(_hostWindow,
            $"Вы записаны. Номер в очереди: {nextQueue + 1}. С баланса списано {service.Price:N0} ₽.", "Готово");
        await LoadAsync();
    }

    private async Task TopUpAsync()
    {
        var raw = await DialogHelper.PromptAsync(_hostWindow, "Пополнение", "Сумма (₽)", "500");
        if (raw == null || !decimal.TryParse(raw.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var userId = _authSession.UserId!.Value;
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.Balance += amount;
        db.BalanceTransactions.Add(new BalanceTransaction
        {
            UserId = userId,
            Amount = amount,
            TransactionType = BalanceTransactionType.TopUp,
            Note = "Пополнение с карты (демо)",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        await LoadAsync();
    }

    private async Task RenderBookingsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var userId = _authSession.UserId!.Value;
        var rows = await db.Bookings
            .AsNoTracking()
            .Include(b => b.WorkshopService)
            .Include(b => b.MasterProfile)
            .ThenInclude(m => m!.User)
            .Where(b => b.ClientUserId == userId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync();

        BookingsList.Items.Clear();
        foreach (var b in rows)
        {
            BookingsList.Items.Add(
                $"#{b.QueueNumber} · {b.WorkshopService.Name} · мастер {b.MasterProfile!.User.DisplayName} · {b.Status} · {b.CreatedAtUtc:dd.MM.yyyy HH:mm}");
        }
    }

    private async Task SyncReviewPickerAsync()
    {
        ReviewTargetPicker.Items.Clear();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var selected = ReviewTargetType.SelectedIndex;
        if (selected <= 0)
        {
            var services = await db.WorkshopServices.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
            foreach (var s in services)
                ReviewTargetPicker.Items.Add(new ComboBoxItem { Content = s.Name, Tag = s.Id });
        }
        else
        {
            var masters = await db.MasterProfiles.AsNoTracking().Include(m => m.User).OrderBy(m => m.User.DisplayName)
                .ToListAsync();
            foreach (var m in masters)
                ReviewTargetPicker.Items.Add(new ComboBoxItem { Content = m.User.DisplayName, Tag = m.Id });
        }

        if (ReviewTargetPicker.Items.Count > 0)
            ReviewTargetPicker.SelectedIndex = 0;
    }

    private async Task SubmitReviewAsync()
    {
        if (ReviewTargetPicker.SelectedItem is not ComboBoxItem item || item.Tag is not int targetId)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Выберите услугу или мастера.", "Отзыв");
            return;
        }

        var rating = (int)(ReviewRating.Value ?? 5);
        var comment = ReviewComment.Text?.Trim() ?? string.Empty;
        if (comment.Length == 0)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Введите текст отзыва.", "Отзыв");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var clientId = _authSession.UserId!.Value;
        var isService = ReviewTargetType.SelectedIndex <= 0;
        db.Reviews.Add(new Review
        {
            ClientUserId = clientId,
            WorkshopServiceId = isService ? targetId : null,
            MasterProfileId = isService ? null : targetId,
            Rating = rating,
            Comment = comment,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        ReviewComment.Text = string.Empty;
        await DialogHelper.AlertAsync(_hostWindow, "Спасибо, отзыв сохранён.", "Отзыв");
        await LoadAsync();
    }

    private async Task RenderMyReviewsAsync()
    {
        MyReviewsPanel.Children.Clear();
        var userId = _authSession.UserId!.Value;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var reviews = await db.Reviews
            .AsNoTracking()
            .Include(r => r.WorkshopService)
            .Include(r => r.MasterProfile)
            .ThenInclude(m => m!.User)
            .Where(r => r.ClientUserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        if (reviews.Count == 0)
        {
            MyReviewsPanel.Children.Add(new TextBlock
            {
                Text = "Вы ещё не оставляли отзывов.",
                Foreground = new SolidColorBrush(Color.Parse("#FFA6ADC8"))
            });
            return;
        }

        foreach (var r in reviews)
        {
            var target = r.WorkshopService != null
                ? $"Услуга: {r.WorkshopService.Name}"
                : $"Мастер: {r.MasterProfile!.User.DisplayName}";
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
                            Text = $"★{r.Rating}/5 · {target}",
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
            MyReviewsPanel.Children.Add(card);
        }
    }
}
