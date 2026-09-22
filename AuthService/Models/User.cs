namespace AuthService.Models;

public class User
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public required string Email {get; set;}
    public required string PasswordHash {get; set;} 
    public required string FirstName {get; set;} 
    public required string LastName {get; set;}
    public string Role {get; private set;} = "user"; // private field restricts modification
    public DateTime CreatedAt {get; private set;} = DateTime.UtcNow;

}