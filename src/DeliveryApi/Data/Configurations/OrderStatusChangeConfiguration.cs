using DeliveryApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeliveryApi.Data.Configurations;

internal sealed class OrderStatusChangeConfiguration : IEntityTypeConfiguration<OrderStatusChange>
{
    public void Configure(EntityTypeBuilder<OrderStatusChange> builder)
    {
        builder.ToTable("order_status_changes");

        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();

        builder.Property(change => change.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(change => change.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
