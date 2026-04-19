using System.IO;
using KLALIK.Data;
using KLALIK.Models.Entities;
using KLALIK.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KLALIK.Services;

public class DatabaseInitializer
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DatabaseInitializer(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task MigrateAndSeedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        await SeedAsync(db, cancellationToken);
    }

    private static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Roles.AnyAsync(cancellationToken))
        {
            db.Roles.AddRange(
                new Role { Name = RoleNames.Client },
                new Role { Name = RoleNames.Master },
                new Role { Name = RoleNames.Moderator },
                new Role { Name = RoleNames.Administrator });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.CollectionDirections.AnyAsync(cancellationToken))
        {
            db.CollectionDirections.AddRange(
                new CollectionDirection { Name = "Аниме" },
                new CollectionDirection { Name = "Новый год" },
                new CollectionDirection { Name = "Хэллоуин" },
                new CollectionDirection { Name = "Киберпанк" },
                new CollectionDirection { Name = "Нуар" });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.ServiceCategories.AnyAsync(cancellationToken))
        {
            db.ServiceCategories.AddRange(
                new ServiceCategory { Name = "Косплей" },
                new ServiceCategory { Name = "Кастомизация" },
                new ServiceCategory { Name = "Праздники" });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.QualificationLevels.AnyAsync(cancellationToken))
        {
            db.QualificationLevels.AddRange(
                new QualificationLevel { Name = "Стажёр", SortOrder = 1 },
                new QualificationLevel { Name = "Мастер", SortOrder = 2 },
                new QualificationLevel { Name = "Ведущий мастер", SortOrder = 3 });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.WorkshopServices.AnyAsync(cancellationToken))
        {
            var baseDir = AppContext.BaseDirectory;
            var directions = await db.CollectionDirections.OrderBy(d => d.Id).ToListAsync(cancellationToken);
            var catCosplay = await db.ServiceCategories.SingleAsync(c => c.Name == "Косплей", cancellationToken);
            var catCustom = await db.ServiceCategories.SingleAsync(c => c.Name == "Кастомизация", cancellationToken);
            var catHoliday = await db.ServiceCategories.SingleAsync(c => c.Name == "Праздники", cancellationToken);

            var now = DateTime.UtcNow;
            var index = 0;
            var addedFromFiles = 0;

            void AddServicesFromFolder(string relativeFolder, ServiceCategory category, string titlePrefix)
            {
                var full = Path.Combine(baseDir, "Resources", "Cards", relativeFolder);
                if (!Directory.Exists(full))
                    return;
                var files = Directory.GetFiles(full, "*.jpg")
                    .Concat(Directory.GetFiles(full, "*.jpeg"))
                    .Concat(Directory.GetFiles(full, "*.png"))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                foreach (var file in files)
                {
                    var rel = Path.GetRelativePath(baseDir, file).Replace('\\', '/');
                    var direction = directions[index % directions.Count];
                    var isHoliday = direction.Name is "Новый год" or "Хэллоуин" || category.Id == catHoliday.Id;
                    var fileTitle = Path.GetFileNameWithoutExtension(file);
                    db.WorkshopServices.Add(new WorkshopService
                    {
                        Name = $"{titlePrefix}: {fileTitle}",
                        Description = category.Name == "Косплей"
                            ? "Создание и доработка костюма, реквизита и образа для косплея."
                            : "Индивидуальная кастомизация и пошив по вашим пожеланиям.",
                        Price = 1500 + index * 150,
                        ImageAssetPath = rel,
                        CollectionDirectionId = direction.Id,
                        ServiceCategoryId = category.Id,
                        IsHolidayRelated = isHoliday,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });
                    index++;
                    addedFromFiles++;
                }
            }

            AddServicesFromFolder("Cosplay", catCosplay, "Косплей");
            AddServicesFromFolder("Custom", catCustom, "Кастом");

            if (addedFromFiles == 0)
            {
                foreach (var direction in directions)
                {
                    db.WorkshopServices.Add(new WorkshopService
                    {
                        Name = $"Праздничное оформление — {direction.Name}",
                        Description = "Тематическое оформление образа к празднику.",
                        Price = 900 + direction.Id * 50,
                        ImageAssetPath = null,
                        CollectionDirectionId = direction.Id,
                        ServiceCategoryId = catHoliday.Id,
                        IsHolidayRelated = true,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(u => u.Email == "admin@matye.local", cancellationToken))
        {
            var roles = await db.Roles.ToDictionaryAsync(r => r.Name, cancellationToken);
            var level = await db.QualificationLevels.OrderBy(l => l.SortOrder).FirstAsync(cancellationToken);

            var admin = new AppUser
            {
                Email = "admin@matye.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                DisplayName = "Администратор",
                RoleId = roles[RoleNames.Administrator].Id,
                Balance = 0,
                CreatedAtUtc = DateTime.UtcNow
            };
            var moderator = new AppUser
            {
                Email = "mod@matye.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Mod123!"),
                DisplayName = "Модератор",
                RoleId = roles[RoleNames.Moderator].Id,
                Balance = 0,
                CreatedAtUtc = DateTime.UtcNow
            };
            var masterUser = new AppUser
            {
                Email = "master@matye.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Master123!"),
                DisplayName = "Мастер Иван",
                RoleId = roles[RoleNames.Master].Id,
                Balance = 0,
                CreatedAtUtc = DateTime.UtcNow
            };
            var client = new AppUser
            {
                Email = "client@matye.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client123!"),
                DisplayName = "Клиент Анна",
                RoleId = roles[RoleNames.Client].Id,
                Balance = 5000,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Users.AddRange(admin, moderator, masterUser, client);
            await db.SaveChangesAsync(cancellationToken);

            db.MasterProfiles.Add(new MasterProfile
            {
                UserId = masterUser.Id,
                QualificationLevelId = level.Id
            });
            await db.SaveChangesAsync(cancellationToken);

            var masterProfile = await db.MasterProfiles.SingleAsync(m => m.UserId == masterUser.Id, cancellationToken);
            var services = await db.WorkshopServices.OrderBy(s => s.Id).Take(8).ToListAsync(cancellationToken);
            foreach (var s in services)
            {
                db.MasterServiceLinks.Add(new MasterServiceLink
                {
                    MasterProfileId = masterProfile.Id,
                    WorkshopServiceId = s.Id
                });
            }

            db.BalanceTransactions.Add(new BalanceTransaction
            {
                UserId = client.Id,
                Amount = 5000,
                TransactionType = BalanceTransactionType.Adjustment,
                Note = "Стартовый баланс демо",
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
