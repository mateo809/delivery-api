using DeliveryApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliveryApi.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Email).HasMaxLength(256);
        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.FullName).HasMaxLength(100);
        builder.Property(user => user.PasswordHash).HasMaxLength(512);
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
    }
}
