public class StoreResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StoreListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class StoreListResponse
{
    public ICollection<StoreListItem> Stores { get; set; } = new List<StoreListItem>();
}

public class CreateStoreRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IFormFile? Logo { get; set; }
}

public class UpdateStoreRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AssignUserToStoreRequest
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Owner";
}

public class UpdateLogoRequest
{
    public IFormFile Logo { get; set; } = null!;
}

public class AddWorkerRequest
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; } = UserRole.Salesperson;
}