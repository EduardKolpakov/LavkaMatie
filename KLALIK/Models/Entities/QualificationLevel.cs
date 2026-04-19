namespace KLALIK.Models.Entities;

public class QualificationLevel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<MasterProfile> MasterProfiles { get; set; } = new List<MasterProfile>();
}
