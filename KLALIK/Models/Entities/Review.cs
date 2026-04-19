namespace KLALIK.Models.Entities;

public class Review
{
    public int Id { get; set; }
    public int ClientUserId { get; set; }
    public int? WorkshopServiceId { get; set; }
    public int? MasterProfileId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public AppUser Client { get; set; } = null!;
    public WorkshopService? WorkshopService { get; set; }
    public MasterProfile? MasterProfile { get; set; }
}
