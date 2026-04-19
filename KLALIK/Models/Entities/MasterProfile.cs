namespace KLALIK.Models.Entities;

public class MasterProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int QualificationLevelId { get; set; }

    public AppUser User { get; set; } = null!;
    public QualificationLevel QualificationLevel { get; set; } = null!;
    public ICollection<MasterServiceLink> ServiceLinks { get; set; } = new List<MasterServiceLink>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<QualificationRequest> QualificationRequests { get; set; } = new List<QualificationRequest>();
}
