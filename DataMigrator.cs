using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using western_backend.Models;

namespace western_backend
{
    public static class DataMigrator
    {
        public static void Migrate(string sqliteConnString, string sqlServerConnString)
        {
            Console.WriteLine("[Migration] Starting migration from SQLite to SQL Server...");
            Console.WriteLine($"[Migration] Source SQLite: {sqliteConnString}");
            Console.WriteLine($"[Migration] Target SQL Server: {sqlServerConnString}");

            var sqliteOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(sqliteConnString)
                .Options;

            var sqlServerOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(sqlServerConnString)
                .Options;

            using var sourceDb = new AppDbContext(sqliteOptions);
            using var targetDb = new AppDbContext(sqlServerOptions);

            // Ensure source database exists before trying to read from it
            if (!sourceDb.Database.CanConnect())
            {
                Console.WriteLine("[Migration] Error: Source SQLite database cannot be accessed.");
                return;
            }

            // Set command timeouts
            sourceDb.Database.SetCommandTimeout(300);
            targetDb.Database.SetCommandTimeout(300);

            // Ensure target database and tables are created
            Console.WriteLine("[Migration] Ensuring target database and schema exist...");
            targetDb.Database.EnsureCreated();

            // Migrate Users
            Console.WriteLine("[Migration] Migrating Users...");
            var users = sourceDb.Users.AsNoTracking().ToList();
            var existingUserIds = targetDb.Users.Select(u => u.Id).ToHashSet();
            foreach (var user in users)
            {
                if (!existingUserIds.Contains(user.Id))
                {
                    targetDb.Users.Add(user);
                }
            }
            targetDb.SaveChanges();

            // Migrate Categories
            Console.WriteLine("[Migration] Migrating Categories...");
            var categories = sourceDb.Categories.AsNoTracking().ToList();
            var existingCategoryIds = targetDb.Categories.Select(c => c.Id).ToHashSet();
            foreach (var cat in categories)
            {
                if (!existingCategoryIds.Contains(cat.Id))
                {
                    targetDb.Categories.Add(cat);
                }
            }
            targetDb.SaveChanges();

            // Migrate SubCategories
            Console.WriteLine("[Migration] Migrating SubCategories...");
            var subCategories = sourceDb.SubCategories.AsNoTracking().ToList();
            var existingSubCategoryIds = targetDb.SubCategories.Select(s => s.Id).ToHashSet();
            foreach (var subCat in subCategories)
            {
                if (!existingSubCategoryIds.Contains(subCat.Id))
                {
                    targetDb.SubCategories.Add(subCat);
                }
            }
            targetDb.SaveChanges();

            // Migrate Brands
            Console.WriteLine("[Migration] Migrating Brands...");
            var brands = sourceDb.Brands.AsNoTracking().ToList();
            var existingBrandIds = targetDb.Brands.Select(b => b.Id).ToHashSet();
            foreach (var brand in brands)
            {
                if (!existingBrandIds.Contains(brand.Id))
                {
                    targetDb.Brands.Add(brand);
                }
            }
            targetDb.SaveChanges();

            // Migrate Gallery items with identity insert handling
            Console.WriteLine("[Migration] Migrating Gallery items...");
            var galleryItems = sourceDb.Gallery.AsNoTracking().ToList();
            if (galleryItems.Any())
            {
                var existingGalleryIds = targetDb.Gallery.Select(g => g.Id).ToHashSet();
                using (var transaction = targetDb.Database.BeginTransaction())
                {
                    try
                    {
                        targetDb.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Gallery ON");
                        foreach (var item in galleryItems)
                        {
                            if (!existingGalleryIds.Contains(item.Id))
                            {
                                targetDb.Gallery.Add(item);
                            }
                        }
                        targetDb.SaveChanges();
                        targetDb.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Gallery OFF");
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Warning while migrating Gallery (trying without IDENTITY_INSERT): {ex.Message}");
                        transaction.Rollback();
                        
                        var existingGalleryTitles = targetDb.Gallery.Select(g => g.Title).ToHashSet();
                        foreach (var item in galleryItems)
                        {
                            if (!existingGalleryTitles.Contains(item.Title))
                            {
                                var newItem = new GalleryItem
                                {
                                    Title = item.Title,
                                    Category = item.Category,
                                    Image = item.Image
                                };
                                targetDb.Gallery.Add(newItem);
                            }
                        }
                        targetDb.SaveChanges();
                    }
                }
            }

            // Migrate Blogs
            Console.WriteLine("[Migration] Migrating Blogs...");
            var blogs = sourceDb.Blogs.AsNoTracking().ToList();
            var existingBlogIds = targetDb.Blogs.Select(b => b.Id).ToHashSet();
            foreach (var blog in blogs)
            {
                if (!existingBlogIds.Contains(blog.Id))
                {
                    targetDb.Blogs.Add(blog);
                }
            }
            targetDb.SaveChanges();

            // Migrate Settings
            Console.WriteLine("[Migration] Migrating Settings...");
            var settings = sourceDb.Settings.AsNoTracking().ToList();
            var existingSettingKeys = targetDb.Settings.Select(s => s.Key).ToHashSet();
            foreach (var setting in settings)
            {
                if (!existingSettingKeys.Contains(setting.Key))
                {
                    targetDb.Settings.Add(setting);
                }
            }
            targetDb.SaveChanges();

            // Migrate Products
            Console.WriteLine("[Migration] Migrating Products...");
            var products = sourceDb.Products.AsNoTracking().ToList();
            var existingProductIds = targetDb.Products.Select(p => p.Id).ToHashSet();
            foreach (var product in products)
            {
                if (!existingProductIds.Contains(product.Id))
                {
                    targetDb.Products.Add(product);
                }
            }
            targetDb.SaveChanges();

            // Migrate Testimonials
            Console.WriteLine("[Migration] Migrating Testimonials...");
            var testimonials = sourceDb.Testimonials.AsNoTracking().ToList();
            var existingTestimonialIds = targetDb.Testimonials.Select(t => t.Id).ToHashSet();
            foreach (var testimonial in testimonials)
            {
                if (!existingTestimonialIds.Contains(testimonial.Id))
                {
                    targetDb.Testimonials.Add(testimonial);
                }
            }
            targetDb.SaveChanges();

            // Migrate Catalogues
            Console.WriteLine("[Migration] Migrating Catalogues...");
            var catalogues = sourceDb.Catalogues.AsNoTracking().ToList();
            var existingCatalogueIds = targetDb.Catalogues.Select(c => c.Id).ToHashSet();
            foreach (var catalogue in catalogues)
            {
                if (!existingCatalogueIds.Contains(catalogue.Id))
                {
                    targetDb.Catalogues.Add(catalogue);
                }
            }
            targetDb.SaveChanges();

            // Migrate Inquiries with identity insert handling
            Console.WriteLine("[Migration] Migrating Inquiries...");
            var inquiries = sourceDb.Inquiries.AsNoTracking().ToList();
            if (inquiries.Any())
            {
                var existingInquiryIds = targetDb.Inquiries.Select(i => i.Id).ToHashSet();
                using (var transaction = targetDb.Database.BeginTransaction())
                {
                    try
                    {
                        targetDb.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Inquiries ON");
                        foreach (var item in inquiries)
                        {
                            if (!existingInquiryIds.Contains(item.Id))
                            {
                                targetDb.Inquiries.Add(item);
                            }
                        }
                        targetDb.SaveChanges();
                        targetDb.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Inquiries OFF");
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Warning while migrating Inquiries (trying without IDENTITY_INSERT): {ex.Message}");
                        transaction.Rollback();
                        
                        var existingInquiryEmails = targetDb.Inquiries.Select(i => i.Email).ToHashSet();
                        foreach (var item in inquiries)
                        {
                            if (!existingInquiryEmails.Contains(item.Email))
                            {
                                var newItem = new Inquiry
                                {
                                    Name = item.Name,
                                    Email = item.Email,
                                    Phone = item.Phone,
                                    Subject = item.Subject,
                                    Message = item.Message,
                                    Date = item.Date,
                                    Status = item.Status
                                };
                                targetDb.Inquiries.Add(newItem);
                            }
                        }
                        targetDb.SaveChanges();
                    }
                }
            }

            Console.WriteLine("[Migration] Migration completed successfully!");
        }
    }
}
