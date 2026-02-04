using System.ComponentModel.DataAnnotations;

public class UserStore
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public UserRole Role { get; set; } = UserRole.Owner;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}