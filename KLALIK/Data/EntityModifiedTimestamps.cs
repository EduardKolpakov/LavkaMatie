using KLALIK.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace KLALIK.Data;

/// <summary>
/// Проставляет время последнего изменения для сущностей при состоянии <see cref="EntityState.Modified"/>.
/// Вынесено для покрытия юнит-тестами без обращения к БД.
/// </summary>
public static class EntityModifiedTimestamps
{
    public static void ApplyWorkshopServiceAndBooking(ChangeTracker changeTracker, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(changeTracker);

        foreach (var entry in changeTracker.Entries<WorkshopService>()
                     .Where(x => x.State == EntityState.Modified))
            entry.Entity.UpdatedAtUtc = utcNow;

        foreach (var entry in changeTracker.Entries<Booking>()
                     .Where(x => x.State == EntityState.Modified))
            entry.Entity.UpdatedAtUtc = utcNow;
    }
}
