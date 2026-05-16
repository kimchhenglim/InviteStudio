using InviteStudio.Application.Entities;
using InviteStudio.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InviteStudio.Application.Services;

public class AuthService
{
    private readonly InviteStudioDbContext _dbContext;
    private readonly PasswordHasher _passwordHasher;

    public AuthService(InviteStudioDbContext dbContext, PasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(entity => entity.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return _passwordHasher.VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public async Task<(bool IsSuccess, string? ErrorMessage, User? User)> RegisterAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var exists = await _dbContext.Users.AnyAsync(entity => entity.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            return (false, "Email is already registered.", null);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, null, user);
    }
}
