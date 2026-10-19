namespace DeliveryApi.Domain;

public sealed class User
{
    public User(string email, string fullName, UserRole role, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        Email = email;
        FullName = fullName;
        Role = role;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string FullName { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
