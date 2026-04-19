using KLALIK.Models.Enums;

namespace KLALIK.Models.Entities;

public class QualificationRequest
{
    public int Id { get; set; }
    public int MasterProfileId { get; set; }
    public QualificationRequestStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public int? ResolverUserId { get; set; }

    public MasterProfile MasterProfile { get; set; } = null!;
    public AppUser? Resolver { get; set; }
}
