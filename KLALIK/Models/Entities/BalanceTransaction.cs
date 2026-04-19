using KLALIK.Models.Enums;

namespace KLALIK.Models.Entities;

public class BalanceTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public BalanceTransactionType TransactionType { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public AppUser User { get; set; } = null!;
}
