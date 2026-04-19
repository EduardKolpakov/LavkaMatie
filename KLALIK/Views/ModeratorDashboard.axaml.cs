using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using KLALIK.Data;
using KLALIK.Helpers;
using KLALIK.Models.Entities;
using KLALIK.Models.Enums;
using KLALIK.Services;
using Microsoft.EntityFrameworkCore;

namespace KLALIK.Views;

public partial class ModeratorDashboard : UserControl
{
    private readonly Window _hostWindow;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthSession _authSession;
    private readonly Action _logout;
    private WorkshopService? _selectedService;

    public ModeratorDashboard(Window hostWindow, IDbContextFactory<AppDbContext> dbFactory, AuthSession authSession,
        Action refreshShell, Action logout)
    {
        InitializeComponent();
        _hostWindow = hostWindow;
        _dbFactory = dbFactory;
        _authSession = authSession;
        _ = refreshShell;
        _logout = logout;
        LogoutButton.Click += (_, _) => _logout();
        ServicesListBox.SelectionChanged += async (_, _) => await OnServiceSelectedAsync();
        NewServiceButton.Click += async (_, _) => await NewServiceAsync();
        SaveServiceButton.Click += async (_, _) => await SaveServiceAsync();
        MastersCombo.SelectionChanged += async (_, _) => await RenderMasterServiceChecksAsync();
        SaveLinksButton.Click += async (_, _) => await SaveMasterLinksAsync();
        Loaded += (_, _) => _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await RefreshServicesListAsync();
        await FillCombosAsync();
        await RenderQualificationRequestsAsync();
        await RenderMasterServiceChecksAsync();
        await RenderAllReviewsAsync();
    }

    private async Task RefreshServicesListAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var services = await db.WorkshopServices
            .AsNoTracking()
            .Include(s => s.CollectionDirection)
            .Include(s => s.ServiceCategory)
            .OrderBy(s => s.Name)
            .ToListAsync();
        ServicesListBox.Items.Clear();
        foreach (var s in services)
            ServicesListBox.Items.Add(new ListBoxItem { Content = $"{s.Name} · {s.CollectionDirection.Name}", Tag = s.Id });
    }

    private async Task FillCombosAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        ServiceDirectionCombo.Items.Clear();
        foreach (var d in await db.CollectionDirections.OrderBy(d => d.Name).ToListAsync())
            ServiceDirectionCombo.Items.Add(new ComboBoxItem { Content = d.Name, Tag = d.Id });

        ServiceCategoryCombo.Items.Clear();
        foreach (var c in await db.ServiceCategories.OrderBy(c => c.Name).ToListAsync())
            ServiceCategoryCombo.Items.Add(new ComboBoxItem { Content = c.Name, Tag = c.Id });

        MastersCombo.Items.Clear();
        var masters = await db.MasterProfiles.Include(m => m.User).OrderBy(m => m.User.DisplayName).ToListAsync();
        foreach (var m in masters)
            MastersCombo.Items.Add(new ComboBoxItem { Content = m.User.DisplayName, Tag = m.Id });
        if (MastersCombo.Items.Count > 0)
            MastersCombo.SelectedIndex = 0;
    }

    private async Task OnServiceSelectedAsync()
    {
        if (ServicesListBox.SelectedItem is not ListBoxItem item || item.Tag is not int id)
        {
            _selectedService = null;
            ClearServiceForm();
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        _selectedService = await db.WorkshopServices
            .Include(s => s.CollectionDirection)
            .Include(s => s.ServiceCategory)
            .FirstAsync(s => s.Id == id);
        ServiceNameBox.Text = _selectedService.Name;
        ServiceDescBox.Text = _selectedService.Description;
        ServicePriceBox.Text = _selectedService.Price.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        ServiceHolidayCheck.IsChecked = _selectedService.IsHolidayRelated;
        ServiceUpdatedText.Text =
            $"Создано: {_selectedService.CreatedAtUtc:dd.MM.yyyy HH:mm} · обновлено: {_selectedService.UpdatedAtUtc:dd.MM.yyyy HH:mm}";

        SelectComboByTag(ServiceDirectionCombo, _selectedService.CollectionDirectionId);
        SelectComboByTag(ServiceCategoryCombo, _selectedService.ServiceCategoryId);
    }

    private static void SelectComboByTag(ComboBox combo, int tag)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem c && c.Tag is int t && t == tag)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private void ClearServiceForm()
    {
        ServiceNameBox.Text = string.Empty;
        ServiceDescBox.Text = string.Empty;
        ServicePriceBox.Text = string.Empty;
        ServiceHolidayCheck.IsChecked = false;
        ServiceUpdatedText.Text = string.Empty;
        ServiceDirectionCombo.SelectedIndex = ServiceDirectionCombo.Items.Count > 0 ? 0 : -1;
        ServiceCategoryCombo.SelectedIndex = ServiceCategoryCombo.Items.Count > 0 ? 0 : -1;
    }

    private async Task NewServiceAsync()
    {
        ServicesListBox.SelectedItem = null;
        _selectedService = null;
        ClearServiceForm();
        await Task.CompletedTask;
    }

    private async Task SaveServiceAsync()
    {
        var name = ServiceNameBox.Text?.Trim() ?? string.Empty;
        var desc = ServiceDescBox.Text?.Trim() ?? string.Empty;
        if (!decimal.TryParse(ServicePriceBox.Text?.Replace(',', '.'),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
        {
            await DialogHelper.AlertAsync(_hostWindow, "Укажите корректную цену.", "Услуга");
            return;
        }

        if (ServiceDirectionCombo.SelectedItem is not ComboBoxItem dItem || dItem.Tag is not int directionId)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Выберите направление коллекции.", "Услуга");
            return;
        }

        if (ServiceCategoryCombo.SelectedItem is not ComboBoxItem cItem || cItem.Tag is not int categoryId)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Выберите категорию.", "Услуга");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        if (_selectedService == null)
        {
            var entity = new WorkshopService
            {
                Name = name,
                Description = desc,
                Price = price,
                ImageAssetPath = null,
                CollectionDirectionId = directionId,
                ServiceCategoryId = categoryId,
                IsHolidayRelated = ServiceHolidayCheck.IsChecked == true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.WorkshopServices.Add(entity);
            await db.SaveChangesAsync();
            await DialogHelper.AlertAsync(_hostWindow, "Услуга создана.", "Готово");
        }
        else
        {
            var entity = await db.WorkshopServices.FirstAsync(s => s.Id == _selectedService.Id);
            entity.Name = name;
            entity.Description = desc;
            entity.Price = price;
            entity.CollectionDirectionId = directionId;
            entity.ServiceCategoryId = categoryId;
            entity.IsHolidayRelated = ServiceHolidayCheck.IsChecked == true;
            await db.SaveChangesAsync();
            await DialogHelper.AlertAsync(_hostWindow, "Изменения сохранены (дата обновления услуги обновлена).", "Готово");
        }

        await RefreshServicesListAsync();
        await OnServiceSelectedAsync();
    }

    private readonly List<CheckBox> _masterServiceChecks = new();

    private async Task RenderMasterServiceChecksAsync()
    {
        MasterServicesPanel.Children.Clear();
        _masterServiceChecks.Clear();
        if (MastersCombo.SelectedItem is not ComboBoxItem mItem || mItem.Tag is not int masterId)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var linked = await db.MasterServiceLinks.Where(l => l.MasterProfileId == masterId).Select(l => l.WorkshopServiceId)
            .ToListAsync();
        var services = await db.WorkshopServices.OrderBy(s => s.Name).ToListAsync();
        foreach (var s in services)
        {
            var cb = new CheckBox
            {
                Content = s.Name,
                Tag = s.Id,
                IsChecked = linked.Contains(s.Id),
                Foreground = Brushes.White
            };
            _masterServiceChecks.Add(cb);
            MasterServicesPanel.Children.Add(cb);
        }
    }

    private async Task SaveMasterLinksAsync()
    {
        if (MastersCombo.SelectedItem is not ComboBoxItem mItem || mItem.Tag is not int masterId)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.MasterServiceLinks.Where(l => l.MasterProfileId == masterId).ToListAsync();
        db.MasterServiceLinks.RemoveRange(existing);

        foreach (var cb in _masterServiceChecks.Where(c => c.IsChecked == true))
        {
            if (cb.Tag is int serviceId)
            {
                db.MasterServiceLinks.Add(new MasterServiceLink
                {
                    MasterProfileId = masterId,
                    WorkshopServiceId = serviceId
                });
            }
        }

        await db.SaveChangesAsync();
        await DialogHelper.AlertAsync(_hostWindow, "Привязки услуг к мастеру обновлены.", "Готово");
    }

    private async Task RenderQualificationRequestsAsync()
    {
        QualificationList.Items.Clear();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var items = await db.QualificationRequests
            .Include(q => q.MasterProfile)
            .ThenInclude(m => m!.User)
            .OrderByDescending(q => q.CreatedAtUtc)
            .Take(50)
            .ToListAsync();
        var levels = await db.QualificationLevels.OrderBy(l => l.SortOrder).ToListAsync();

        foreach (var q in items)
        {
            var panel = new StackPanel
            {
                Spacing = 6,
                Margin = new Avalonia.Thickness(0, 0, 0, 12),
                Children =
                {
                    new TextBlock
                    {
                        Text =
                            $"{q.MasterProfile!.User.DisplayName} · {q.Status} · {q.CreatedAtUtc:dd.MM.yyyy HH:mm}",
                        Foreground = Brushes.White,
                        FontWeight = FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = q.Note,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.Parse("#FF6C7086"))
                    }
                }
            };

            if (q.Status == QualificationRequestStatus.Pending)
            {
                var levelCombo = new ComboBox { MinWidth = 220 };
                foreach (var l in levels)
                    levelCombo.Items.Add(new ComboBoxItem { Content = l.Name, Tag = l.Id });
                levelCombo.SelectedIndex = Math.Max(0, levels.Count - 1);

                var approve = new Button { Content = "Поднять квалификацию", Tag = q.Id, Margin = new Avalonia.Thickness(0, 4, 0, 0) };
                approve.Click += async (_, _) => await ResolveQualificationAsync(q.Id, true, levelCombo);

                var reject = new Button { Content = "Отклонить", Tag = q.Id, Margin = new Avalonia.Thickness(8, 4, 0, 0) };
                reject.Click += async (_, _) => await ResolveQualificationAsync(q.Id, false, levelCombo);

                var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { approve, reject } };
                panel.Children.Add(levelCombo);
                panel.Children.Add(actions);
            }

            QualificationList.Items.Add(panel);
        }
    }

    private async Task ResolveQualificationAsync(int requestId, bool approve, ComboBox levelCombo)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var request = await db.QualificationRequests.Include(q => q.MasterProfile)
            .FirstAsync(q => q.Id == requestId);
        var moderatorId = _authSession.UserId!.Value;
        request.ResolvedAtUtc = DateTime.UtcNow;
        request.ResolverUserId = moderatorId;
        if (!approve)
        {
            request.Status = QualificationRequestStatus.Rejected;
            await db.SaveChangesAsync();
            await DialogHelper.AlertAsync(_hostWindow, "Заявка отклонена.", "Готово");
            await RenderQualificationRequestsAsync();
            return;
        }

        if (levelCombo.SelectedItem is not ComboBoxItem item || item.Tag is not int levelId)
        {
            await DialogHelper.AlertAsync(_hostWindow, "Выберите уровень квалификации.", "Квалификация");
            return;
        }

        request.Status = QualificationRequestStatus.Approved;
        var master = await db.MasterProfiles.FirstAsync(m => m.Id == request.MasterProfileId);
        master.QualificationLevelId = levelId;
        await db.SaveChangesAsync();
        await DialogHelper.AlertAsync(_hostWindow, "Квалификация мастера обновлена.", "Готово");
        await RenderQualificationRequestsAsync();
    }

    private async Task RenderAllReviewsAsync()
    {
        ModeratorReviewsPanel.Children.Clear();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var reviews = await db.Reviews
            .AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.WorkshopService)
            .Include(r => r.MasterProfile)
            .ThenInclude(m => m!.User)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        if (reviews.Count == 0)
        {
            ModeratorReviewsPanel.Children.Add(new TextBlock
            {
                Text = "Отзывов пока нет.",
                Foreground = new SolidColorBrush(Color.Parse("#FFA6ADC8"))
            });
            return;
        }

        foreach (var r in reviews)
        {
            var target = r.WorkshopService != null
                ? $"услуга «{r.WorkshopService.Name}»"
                : $"мастер {r.MasterProfile!.User.DisplayName}";
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
                            Text = $"{r.Client.DisplayName} · ★{r.Rating}/5 · {target}",
                            Foreground = Brushes.White,
                            FontWeight = FontWeight.SemiBold,
                            TextWrapping = TextWrapping.Wrap
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
            ModeratorReviewsPanel.Children.Add(card);
        }
    }
}
