public class Users
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
}