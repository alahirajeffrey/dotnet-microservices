using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{   
    private readonly AuthDbContext _db;
    private readonly JwtService _jwtService;
    private readonly RabbitMqLogPublisher _logPublisher;

    public AuthController(AuthDbContext db, JwtService jwtService, RabbitMqLogPublisher logPublisher)
    {
        _db = db;
        _jwtService = jwtService;
        _logPublisher = logPublisher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // check if user exists
        var userExists = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (userExists != null)
        {
            return BadRequest("User already exists");
        }

        // create new user object
        var newUser = new User
        {
            FirstName= request.FirstName,
            LastName= request.LastName,
            Email= request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                request.Password
            )
        };

        // save new user and return response
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        await _logPublisher.PublishAsync(
            "AuthService",
            "UserRegistered",
            $"User {newUser.Email} registered successfully",
            new { userId = newUser.Id, email = newUser.Email });

        return Ok(new
        {
            status= "Success",
            message= "User registered successfully"
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // check if user exists
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return BadRequest("Invalid credentials");
        }

        // verify user password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return BadRequest("Invalid credentials");
        }

        // generate jwt token and set it in cookies
        var jwt = _jwtService.GenerateToken(user);
        Response.Cookies.Append("jwt", jwt, new CookieOptions
        {
            HttpOnly = true
        });

        await _logPublisher.PublishAsync(
            "AuthService",
            "UserLoggedIn",
            $"User {user.Email} logged in successfully",
            new { userId = user.Id, email = user.Email });

        return Ok(new
        {
            status = "Success",
            message= "Login successful"
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // remove token from cookies
        Response.Cookies.Delete("jwt");
        return Ok(new
        {
            status = "Success",
            message= "Logout successful"
        });
    }

    [HttpPatch("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        var userEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email);

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            (userId != null && u.Id.ToString() == userId) ||
            (userEmail != null && u.Email == userEmail));

        if (user == null)
        {
            return BadRequest("User not found");
        }

        // verify user password
        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
        {
            return BadRequest("Invalid password");
        }

        // hash new password and save
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();

        // remove token from cookies
        Response.Cookies.Delete("jwt");

        await _logPublisher.PublishAsync(
            "AuthService",
            "PasswordChanged",
            $"Password changed for user {user.Email}",
            new { userId = user.Id, email = user.Email });

        return Ok(new
        {
            status = "Success",
            message = "Password changed successfully"
        });
    }
}