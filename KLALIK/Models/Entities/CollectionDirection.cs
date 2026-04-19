namespace KLALIK.Models.Entities;

public class CollectionDirection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<WorkshopService> Services { get; set; } = new List<WorkshopService>();
}
