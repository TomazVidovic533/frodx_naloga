using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderIngestion.Domain.Models;

namespace OrderIngestion.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.ExternalId)
            .IsRequired();

        builder.HasIndex(o => o.ExternalId)
            .IsUnique();

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Completed)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt);
    }
}