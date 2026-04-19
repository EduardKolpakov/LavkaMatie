namespace KLALIK.Models.Entities;

public class WorkshopService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageAssetPath { get; set; }
    public int CollectionDirectionId { get; set; }
    public int ServiceCategoryId { get; set; }
    public bool IsHolidayRelated { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public CollectionDirection CollectionDirection { get; set; } = null!;
    public ServiceCategory ServiceCategory { get; set; } = null!;
    public ICollection<MasterServiceLink> MasterLinks { get; set; } = new List<MasterServiceLink>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
