namespace KLALIK.Models.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Role Role { get; set; } = null!;
    public MasterProfile? MasterProfile { get; set; }
    public ICollection<Booking> ClientBookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<BalanceTransaction> BalanceTransactions { get; set; } = new List<BalanceTransaction>();
    public ICollection<QualificationRequest> ResolvedQualificationRequests { get; set; } = new List<QualificationRequest>();
}
