using KLALIK.Data;
using KLALIK.Models.Entities;
using KLALIK.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KLALIK.Tests;

public class EntityModifiedTimestampsTests
{
    private static readonly DateTime T0 = new(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TStamp = new(2026, 4, 19, 12, 30, 45, DateTimeKind.Utc);

    private static DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseSnakeCaseNamingConvention()
            .Options;

    [Fact]
    public void UT_TS_01_ModifiedWorkshopService_UpdatedAtUtc_SetToProvidedTime()
    {
        using var db = new AppDbContext(CreateOptions());
        var dir = new CollectionDirection { Name = "Направление" };
        var cat = new ServiceCategory { Name = "Категория" };
        db.CollectionDirections.Add(dir);
        db.ServiceCategories.Add(cat);
        db.SaveChanges();

        var ws = new WorkshopService
        {
            Name = "Услуга",
            Description = "Описание",
            Price = 100,
            CollectionDirectionId = dir.Id,
            ServiceCategoryId = cat.Id,
            IsHolidayRelated = false,
            CreatedAtUtc = T0,
            UpdatedAtUtc = T0
        };
        db.WorkshopServices.Add(ws);
        db.SaveChanges();

        ws.Name = "Услуга (изменено)";
        EntityModifiedTimestamps.ApplyWorkshopServiceAndBooking(db.ChangeTracker, TStamp);

        Assert.Equal(TStamp, ws.UpdatedAtUtc);
    }

    [Fact]
    public void UT_TS_02_ModifiedBooking_UpdatedAtUtc_SetToProvidedTime()
    {
        using var db = new AppDbContext(CreateOptions());
        var booking = SeedMinimalBookingGraph(db);
        db.SaveChanges();

        booking.Status = BookingStatus.Confirmed;
        EntityModifiedTimestamps.ApplyWorkshopServiceAndBooking(db.ChangeTracker, TStamp);

        Assert.Equal(TStamp, booking.UpdatedAtUtc);
    }

    [Fact]
    public void UT_TS_03_AddedWorkshopService_NotTouchedByStampMethod()
    {
        using var db = new AppDbContext(CreateOptions());
        var dir = new CollectionDirection { Name = "D" };
        var cat = new ServiceCategory { Name = "C" };
        db.AddRange(dir, cat);
        db.SaveChanges();

        var ws = new WorkshopService
        {
            Name = "Новая",
            Description = "X",
            Price = 1,
            CollectionDirectionId = dir.Id,
            ServiceCategoryId = cat.Id,
            IsHolidayRelated = false,
            CreatedAtUtc = TStamp,
            UpdatedAtUtc = TStamp
        };
        db.WorkshopServices.Add(ws);
        EntityModifiedTimestamps.ApplyWorkshopServiceAndBooking(db.ChangeTracker,
            new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(TStamp, ws.UpdatedAtUtc);
    }

    [Fact]
    public void UT_TS_04_UnchangedWorkshopService_UpdatedAtUtc_NotChanged()
    {
        using var db = new AppDbContext(CreateOptions());
        var dir = new CollectionDirection { Name = "D" };
        var cat = new ServiceCategory { Name = "C" };
        db.AddRange(dir, cat);
        db.SaveChanges();

        var ws = new WorkshopService
        {
            Name = "S",
            Description = "D",
            Price = 1,
            CollectionDirectionId = dir.Id,
            ServiceCategoryId = cat.Id,
            IsHolidayRelated = false,
            CreatedAtUtc = T0,
            UpdatedAtUtc = T0
        };
        db.WorkshopServices.Add(ws);
        db.SaveChanges();

        var before = ws.UpdatedAtUtc;
        EntityModifiedTimestamps.ApplyWorkshopServiceAndBooking(db.ChangeTracker, TStamp);

        Assert.Equal(before, ws.UpdatedAtUtc);
    }

    [Fact]
    public void UT_TS_05_BothWorkshopServiceAndBookingModified_SameTimestamp()
    {
        using var db = new AppDbContext(CreateOptions());
        var booking = SeedMinimalBookingGraph(db);
        db.SaveChanges();

        var ws = db.WorkshopServices.Single();
        booking.Status = BookingStatus.Completed;
        ws.Description = "Новое описание";

        EntityModifiedTimestamps.ApplyWorkshopServiceAndBooking(db.ChangeTracker, TStamp);

        Assert.Equal(TStamp, ws.UpdatedAtUtc);
        Assert.Equal(TStamp, booking.UpdatedAtUtc);
    }

    private static Booking SeedMinimalBookingGraph(AppDbContext db)
    {
        var role = new Role { Name = "Client" };
        db.Roles.Add(role);
        db.SaveChanges();

        var client = new AppUser
        {
            Email = "c@test.local",
            PasswordHash = "x",
            DisplayName = "C",
            RoleId = role.Id,
            Balance = 0,
            CreatedAtUtc = T0
        };
        var masterUser = new AppUser
        {
            Email = "m@test.local",
            PasswordHash = "x",
            DisplayName = "M",
            RoleId = role.Id,
            Balance = 0,
            CreatedAtUtc = T0
        };
        db.Users.AddRange(client, masterUser);
        db.SaveChanges();

        var level = new QualificationLevel { Name = "L1", SortOrder = 1 };
        db.QualificationLevels.Add(level);
        db.SaveChanges();

        var mp = new MasterProfile { UserId = masterUser.Id, QualificationLevelId = level.Id };
        db.MasterProfiles.Add(mp);

        var dir = new CollectionDirection { Name = "Dir" };
        var cat = new ServiceCategory { Name = "Cat" };
        db.CollectionDirections.Add(dir);
        db.ServiceCategories.Add(cat);
        db.SaveChanges();

        var ws = new WorkshopService
        {
            Name = "WS",
            Description = "D",
            Price = 100,
            CollectionDirectionId = dir.Id,
            ServiceCategoryId = cat.Id,
            IsHolidayRelated = false,
            CreatedAtUtc = T0,
            UpdatedAtUtc = T0
        };
        db.WorkshopServices.Add(ws);
        db.SaveChanges();

        var booking = new Booking
        {
            ClientUserId = client.Id,
            MasterProfileId = mp.Id,
            WorkshopServiceId = ws.Id,
            QueueNumber = 1,
            Status = BookingStatus.Pending,
            ScheduledAtUtc = null,
            CreatedAtUtc = T0,
            UpdatedAtUtc = T0
        };
        db.Bookings.Add(booking);
        return booking;
    }
}
