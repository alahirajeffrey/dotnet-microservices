namespace AuthService.Models;

public enum Role
{
    User,
    Admin

}

public class User
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public required string Email {get; set;}
    public required string PasswordHash {get; set;} 
    public required string FirstName {get; set;} 
    public required string LastName {get; set;}
    public Role Role {get; set;} = Role.User;
    public DateTime CreatedAt {get; private set;} = DateTime.UtcNow;

}

