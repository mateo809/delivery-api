using DeliveryApi.Auth;
using DeliveryApi.Contracts.Auth;
using DeliveryApi.Data;
using DeliveryApi.Domain;
using DeliveryApi.Services.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DeliveryApi.Services;

public sealed class AuthService(
    DeliveryDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    TokenService tokenService,
    TimeProvider timeProvider)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw DuplicateEmail();
        }

        var user = new User(email, request.FullName.Trim(), request.Role!.Value, timeProvider.GetUtcNow());
        user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw DuplicateEmail();
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null)
        {
            throw InvalidCredentials();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            throw InvalidCredentials();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(passwordHasher.HashPassword(user, request.Password));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return CreateAuthResponse(user);
    }

    public async Task<UserResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException("The current user no longer exists.");

        return UserResponse.From(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = tokenService.CreateToken(user);
        return new AuthResponse(token.Value, token.ExpiresAt, UserResponse.From(user));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static ConflictException DuplicateEmail() => new("An account with this email already exists.");

    private static AuthenticationFailedException InvalidCredentials() => new("Invalid email or password.");
}
