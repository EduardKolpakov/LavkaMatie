namespace KLALIK.Models.Entities;

public class MasterServiceLink
{
    public int Id { get; set; }
    public int MasterProfileId { get; set; }
    public int WorkshopServiceId { get; set; }

    public MasterProfile MasterProfile { get; set; } = null!;
    public WorkshopService WorkshopService { get; set; } = null!;
}
