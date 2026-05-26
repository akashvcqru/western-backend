using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Collections.Generic;
using western_backend.Models;

namespace western_backend
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<SubCategory> SubCategories => Set<SubCategory>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<GalleryItem> Gallery => Set<GalleryItem>();
        public DbSet<BlogPost> Blogs => Set<BlogPost>();
        public DbSet<Inquiry> Inquiries => Set<Inquiry>();
        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Testimonial> Testimonials => Set<Testimonial>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Setting PK
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasKey(e => e.Key);
            });

            // Configure User Email as index/unique if needed
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure BlogPost JSON columns
            modelBuilder.Entity<BlogPost>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

                entity.Property(e => e.Content)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
            });

            // Configure Product JSON columns
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Images)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

                entity.Property(e => e.Features)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<FeatureItem>>(v, (JsonSerializerOptions?)null) ?? new List<FeatureItem>());

                entity.Property(e => e.Specifications)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<SpecificationItem>>(v, (JsonSerializerOptions?)null) ?? new List<SpecificationItem>());

                entity.Property(e => e.Dimensions)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<DimensionItem>>(v, (JsonSerializerOptions?)null) ?? new List<DimensionItem>());

                entity.Property(e => e.Resources)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<ResourceItem>>(v, (JsonSerializerOptions?)null) ?? new List<ResourceItem>());

                entity.Property(e => e.Variants)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<VariantItem>>(v, (JsonSerializerOptions?)null) ?? new List<VariantItem>());

                entity.Property(e => e.Swatches)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<SwatchItem>>(v, (JsonSerializerOptions?)null) ?? new List<SwatchItem>());

                entity.Property(e => e.QuickSpecs)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
            });
        }
    }
}
