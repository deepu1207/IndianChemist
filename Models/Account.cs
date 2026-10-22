namespace IndianChemist.Models;

public class Account
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public bool Verified
    { get; set; }
    public string? VerificationHash
    { get; set; }
    public DateTime VerificationExpires
    { get; set; }
    public string? RefreshHash
    { get; set; }
    public DateTime RefreshExpires
    { get; set; }
}
