using KLALIK.Models.Enums;

namespace KLALIK.Models.Entities;

public class Booking
{
    public int Id { get; set; }
    public int ClientUserId { get; set; }
    public int MasterProfileId { get; set; }
    public int WorkshopServiceId { get; set; }
    public int QueueNumber { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public AppUser Client { get; set; } = null!;
    public MasterProfile MasterProfile { get; set; } = null!;
    public WorkshopService WorkshopService { get; set; } = null!;
}
