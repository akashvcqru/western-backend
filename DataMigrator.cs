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

            // Ensure target database and tables are created
            Console.WriteLine("[Migration] Ensuring target database and schema exist...");
            targetDb.Database.EnsureCreated();

            // Migrate Users
            Console.WriteLine("[Migration] Migrating Users...");
            var users = sourceDb.Users.AsNoTracking().ToList();
            foreach (var user in users)
            {
                if (!targetDb.Users.Any(u => u.Id == user.Id))
                {
                    targetDb.Users.Add(user);
                }
            }
            targetDb.SaveChanges();

            // Migrate Categories
            Console.WriteLine("[Migration] Migrating Categories...");
            var categories = sourceDb.Categories.AsNoTracking().ToList();
            foreach (var cat in categories)
            {
                if (!targetDb.Categories.Any(c => c.Id == cat.Id))
                {
                    targetDb.Categories.Add(cat);
                }
            }
            targetDb.SaveChanges();

            // Migrate SubCategories
            Console.WriteLine("[Migration] Migrating SubCategories...");
            var subCategories = sourceDb.SubCategories.AsNoTracking().ToList();
            foreach (var subCat in subCategories)
            {
                if (!targetDb.SubCategories.Any(s => s.Id == subCat.Id))
                {
                    targetDb.SubCategories.Add(subCat);
                }
            }
            targetDb.SaveChanges();

            // Migrate Brands
            Console.WriteLine("[Migration] Migrating Brands...");
            var brands = sourceDb.Brands.AsNoTracking().ToList();
            foreach (var brand in brands)
            {
                if (!targetDb.Brands.Any(b => b.Id == brand.Id))
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
                using (var transaction = targetDb.Database.BeginTransaction())
                {
                    try
                    {
                        targetDb.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Gallery ON");
                        foreach (var item in galleryItems)
                        {
                            if (!targetDb.Gallery.Any(g => g.Id == item.Id))
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
                        
                        foreach (var item in galleryItems)
                        {
                            if (!targetDb.Gallery.Any(g => g.Title == item.Title && g.Image == item.Image))
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
            foreach (var blog in blogs)
            {
                if (!targetDb.Blogs.Any(b => b.Id == blog.Id))
                {
                    targetDb.Blogs.Add(blog);
                }
            }
            targetDb.SaveChanges();

            // Migrate Settings
            Console.WriteLine("[Migration] Migrating Settings...");
            var settings = sourceDb.Settings.AsNoTracking().ToList();
            foreach (var setting in settings)
            {
                if (!targetDb.Settings.Any(s => s.Key == setting.Key))
                {
                    targetDb.Settings.Add(setting);
                }
            }
            targetDb.SaveChanges();

            // Migrate Products
            Console.WriteLine("[Migration] Migrating Products...");
            var products = sourceDb.Products.AsNoTracking().ToList();
            foreach (var product in products)
            {
                if (!targetDb.Products.Any(p => p.Id == product.Id))
                {
                    targetDb.Products.Add(product);
                }
            }
            targetDb.SaveChanges();

            // Migrate Testimonials
            Console.WriteLine("[Migration] Migrating Testimonials...");
            var testimonials = sourceDb.Testimonials.AsNoTracking().ToList();
            foreach (var testimonial in testimonials)
            {
                if (!targetDb.Testimonials.Any(t => t.Id == testimonial.Id))
                {
                    targetDb.Testimonials.Add(testimonial);
                }
            }
            targetDb.SaveChanges();

            // Migrate Catalogues
            Console.WriteLine("[Migration] Migrating Catalogues...");
            var catalogues = sourceDb.Catalogues.AsNoTracking().ToList();
            foreach (var catalogue in catalogues)
            {
                if (!targetDb.Catalogues.Any(c => c.Id == catalogue.Id))
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
                using (var transaction = targetDb.Database.BeginTransaction())
                {
                    try
                    {
                        targetDb.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Inquiries ON");
                        foreach (var item in inquiries)
                        {
                            if (!targetDb.Inquiries.Any(i => i.Id == item.Id))
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
                        
                        foreach (var item in inquiries)
                        {
                            if (!targetDb.Inquiries.Any(i => i.Email == item.Email && i.Date == item.Date && i.Message == item.Message))
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
