using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;
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
        public DbSet<Catalogue> Catalogues => Set<Catalogue>();
        public DbSet<Service> Services => Set<Service>();

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

            // Value comparers
            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );

            var featureListComparer = new ValueComparer<List<FeatureItem>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Count == c2.Count && c1.Zip(c2, (x, y) => x.Title == y.Title && x.Desc == y.Desc).All(b => b)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, HashCode.Combine(v.Title, v.Desc))),
                c => c.Select(item => new FeatureItem { Title = item.Title, Desc = item.Desc }).ToList()
            );

            var specListComparer = new ValueComparer<List<SpecificationItem>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Count == c2.Count && c1.Zip(c2, (x, y) => x.Label == y.Label && x.Value == y.Value).All(b => b)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, HashCode.Combine(v.Label, v.Value))),
                c => c.Select(item => new SpecificationItem { Label = item.Label, Value = item.Value }).ToList()
            );

            var dimListComparer = new ValueComparer<List<DimensionItem>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Count == c2.Count && c1.Zip(c2, (x, y) => x.Name == y.Name && x.Range == y.Range && x.Coord == y.Coord).All(b => b)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, HashCode.Combine(v.Name, v.Range, v.Coord))),
                c => c.Select(item => new DimensionItem { Name = item.Name, Range = item.Range, Coord = item.Coord }).ToList()
            );

            var resourceListComparer = new ValueComparer<List<ResourceItem>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Count == c2.Count && c1.Zip(c2, (x, y) => x.Id == y.Id && x.Title == y.Title && x.Desc == y.Desc && x.Format == y.Format && x.Size == y.Size && x.FileData == y.FileData && x.FileName == y.FileName).All(b => b)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, HashCode.Combine(v.Id, v.Title, v.Desc, v.Format, v.Size, v.FileData, v.FileName))),
                c => c.Select(item => new ResourceItem { Id = item.Id, Title = item.Title, Desc = item.Desc, Format = item.Format, Size = item.Size, FileData = item.FileData, FileName = item.FileName }).ToList()
            );

            var variantListComparer = new ValueComparer<List<VariantItem>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Count == c2.Count && c1.Zip(c2, (x, y) => x.Label == y.Label && x.Options.SequenceEqual(y.Options)).All(b => b)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, HashCode.Combine(v.Label, v.Options.Count))),
                c => c.Select(item => new VariantItem { Label = item.Label, Options = item.Options.ToList() }).ToList()
            );

            var swatchListComparer = new ValueComparer<List<SwatchItem>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Count == c2.Count && c1.Zip(c2, (x, y) => x.Category == y.Category && x.Options.Zip(y.Options, (o1, o2) => o1.Name == o2.Name && o1.Hex == o2.Hex && o1.Desc == o2.Desc && o1.Border == o2.Border).All(b => b)).All(b => b)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, HashCode.Combine(v.Category, v.Options.Count))),
                c => c.Select(item => new SwatchItem { Category = item.Category, Options = item.Options.Select(o => new SwatchOption { Name = o.Name, Hex = o.Hex, Desc = o.Desc, Border = o.Border }).ToList() }).ToList()
            );

            var trustBadgeListComparer = new ValueComparer<List<TrustBadgeItem>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Count == c2.Count && c1.Zip(c2, (x, y) => x.Title == y.Title && x.Desc == y.Desc && x.Icon == y.Icon).All(b => b)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, HashCode.Combine(v.Title, v.Desc, v.Icon))),
                c => c.Select(item => new TrustBadgeItem { Title = item.Title, Desc = item.Desc, Icon = item.Icon }).ToList()
            );

            // Configure BlogPost JSON columns
            modelBuilder.Entity<BlogPost>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(e => e.Content)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Metadata.SetValueComparer(stringListComparer);
            });

            // Configure Product JSON columns
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Images)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(e => e.Features)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<FeatureItem>>(v, (JsonSerializerOptions?)null) ?? new List<FeatureItem>())
                    .Metadata.SetValueComparer(featureListComparer);

                entity.Property(e => e.Specifications)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<SpecificationItem>>(v, (JsonSerializerOptions?)null) ?? new List<SpecificationItem>())
                    .Metadata.SetValueComparer(specListComparer);

                entity.Property(e => e.Dimensions)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<DimensionItem>>(v, (JsonSerializerOptions?)null) ?? new List<DimensionItem>())
                    .Metadata.SetValueComparer(dimListComparer);

                entity.Property(e => e.Resources)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<ResourceItem>>(v, (JsonSerializerOptions?)null) ?? new List<ResourceItem>())
                    .Metadata.SetValueComparer(resourceListComparer);

                entity.Property(e => e.Variants)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<VariantItem>>(v, (JsonSerializerOptions?)null) ?? new List<VariantItem>())
                    .Metadata.SetValueComparer(variantListComparer);

                entity.Property(e => e.Swatches)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<SwatchItem>>(v, (JsonSerializerOptions?)null) ?? new List<SwatchItem>())
                    .Metadata.SetValueComparer(swatchListComparer);

                entity.Property(e => e.QuickSpecs)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(e => e.TrustBadges)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<TrustBadgeItem>>(v, (JsonSerializerOptions?)null) ?? new List<TrustBadgeItem>())
                    .Metadata.SetValueComparer(trustBadgeListComparer);
            });
        }
    }
}
