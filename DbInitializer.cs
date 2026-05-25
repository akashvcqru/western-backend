using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using western_backend.Models;

namespace western_backend
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context, string dataPath)
        {
            context.Database.EnsureCreated();

            // Run raw SQL migrations for SubCategories table & columns
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS SubCategories (
                    Id TEXT PRIMARY KEY,
                    Slug TEXT,
                    Name TEXT,
                    Description TEXT,
                    Image TEXT,
                    CategoryId TEXT,
                    Status TEXT
                );");

            try
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN SubCategory TEXT;");
            }
            catch { /* Ignored if column already exists */ }

            // 1. Seed Admin User
            if (!context.Users.Any())
            {
                var hasher = new PasswordHasher<User>();
                var admin = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "admin@westernofficesolutions.com"
                };
                admin.PasswordHash = hasher.HashPassword(admin, "admin123");
                context.Users.Add(admin);
                context.SaveChanges();
            }

            // 2. Seed Settings
            if (!context.Settings.Any(s => s.Key.ToLower() == "bdm_settings_contact"))
            {
                var contactSettings = new
                {
                    supportEmail = "info@bawadittamal.com",
                    phoneNumber = "+91 98765 43210",
                    storeAddress = "123, Building Material Market, New Delhi, 110001"
                };
                context.Settings.Add(new Setting { Key = "bdm_settings_contact", Value = JsonSerializer.Serialize(contactSettings) });
            }

            if (!context.Settings.Any(s => s.Key.ToLower() == "bdm_settings_social"))
            {
                var socialSettings = new
                {
                    instagramUrl = "",
                    facebookUrl = "",
                    twitterUrl = ""
                };
                context.Settings.Add(new Setting { Key = "bdm_settings_social", Value = JsonSerializer.Serialize(socialSettings) });
            }

            if (!context.Settings.Any(s => s.Key.ToLower() == "bdm_settings_slider"))
            {
                var sliderSettings = new
                {
                    heading = "Welcome to Western Interio",
                    description = "Think to design beyond. Please upload showcase images or configure slide settings in the admin panel to populate this slider.",
                    buttonText = "Start Your Project",
                    buttonLink = "#quote",
                    images = new string[] {
                        "https://images.unsplash.com/photo-1497366754035-f200968a6e72?q=80&w=2070&auto=format&fit=crop"
                    }
                };
                context.Settings.Add(new Setting { Key = "bdm_settings_slider", Value = JsonSerializer.Serialize(sliderSettings) });
            }
            context.SaveChanges();

            // Options for JSON Deserialization
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // 3. Seed Categories
            if (!context.Categories.Any())
            {
                var filePath = Path.Combine(dataPath, "categories.json");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var rawCats = JsonSerializer.Deserialize<List<JsonElement>>(json, jsonOptions);
                        if (rawCats != null)
                        {
                            var cats = rawCats.Select(c => {
                                string id = c.GetProperty("id").GetString() ?? "";
                                string slug = c.TryGetProperty("slug", out var s) ? (s.GetString() ?? id) : id;
                                return new Category
                                {
                                    Id = id,
                                    Slug = slug,
                                    Name = c.GetProperty("name").GetString() ?? "",
                                    Description = c.GetProperty("description").GetString() ?? "",
                                    Image = c.GetProperty("image").GetString() ?? "",
                                    Count = 0,
                                    Status = "Active"
                                };
                            }).ToList();
                            context.Categories.AddRange(cats);
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding categories: {ex.Message}");
                    }
                }
            }

            // 4. Seed Brands
            if (!context.Brands.Any())
            {
                var filePath = Path.Combine(dataPath, "brands.json");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var rawBrands = JsonSerializer.Deserialize<List<JsonElement>>(json, jsonOptions);
                        if (rawBrands != null)
                        {
                            var brands = rawBrands.Select(b => new Brand
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = b.GetProperty("name").GetString() ?? "",
                                Url = b.GetProperty("url").GetString() ?? "",
                                Link = b.GetProperty("link").GetString() ?? ""
                            }).ToList();
                            context.Brands.AddRange(brands);
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding brands: {ex.Message}");
                    }
                }
            }

            // 5. Seed Gallery
            if (!context.Gallery.Any())
            {
                var filePath = Path.Combine(dataPath, "gallery.json");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var items = JsonSerializer.Deserialize<List<GalleryItem>>(json, jsonOptions);
                        if (items != null)
                        {
                            context.Gallery.AddRange(items);
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding gallery: {ex.Message}");
                    }
                }
            }

            // 6. Seed Blogs
            if (!context.Blogs.Any())
            {
                var filePath = Path.Combine(dataPath, "blogs.json");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var blogs = JsonSerializer.Deserialize<List<BlogPost>>(json, jsonOptions);
                        if (blogs != null)
                        {
                            context.AddRange(blogs);
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding blogs: {ex.Message}");
                    }
                }
            }

            // 7. Seed Inquiries
            if (!context.Inquiries.Any())
            {
                var mockInquiries = new List<Inquiry>();
                context.Inquiries.AddRange(mockInquiries);
                context.SaveChanges();
            }

            // 8. Seed Products
            if (!context.Products.Any())
            {
                var filePath = Path.Combine(dataPath, "products.json");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var products = JsonSerializer.Deserialize<List<Product>>(json, jsonOptions);
                        if (products != null)
                        {
                            foreach (var p in products)
                            {
                                if (string.IsNullOrEmpty(p.Slug))
                                {
                                    p.Slug = p.Id;
                                }
                                if (p.Stock == 0)
                                {
                                    p.Stock = 15;
                                }
                            }
                            context.Products.AddRange(products);
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding products: {ex.Message}");
                    }
                }
            }

            // 9. Update Category Product Counts
            try
            {
                var cats = context.Categories.ToList();
                var prods = context.Products.ToList();
                foreach (var cat in cats)
                {
                    cat.Count = prods.Count(p => 
                        string.Equals(p.Category, cat.Id, StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(p.Category, cat.Slug, StringComparison.OrdinalIgnoreCase)
                    );
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating category counts: {ex.Message}");
            }
        }
    }
}
