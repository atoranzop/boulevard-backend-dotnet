using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public Guid Id {get; set; } = Guid.NewGuid();
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Profile> Profiles { get; set; } = new List<Profile>();
    public ICollection<UserStore> UserStores { get; set; } = new List<UserStore>();
}