using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
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
            context.Database.SetCommandTimeout(300);
            context.Database.EnsureCreated();

            // Run raw SQL migrations for SubCategories table & columns (SQLite only)
            if (context.Database.IsSqlite())
            {
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

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS Testimonials (
                        Id TEXT PRIMARY KEY,
                        Author TEXT,
                        Designation TEXT,
                        Company TEXT,
                        Quote TEXT,
                        Rating INTEGER,
                        Category TEXT,
                        Status TEXT,
                        CreatedAt TEXT,
                        Image TEXT
                    );");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS Catalogues (
                        Id TEXT PRIMARY KEY,
                        Title TEXT,
                        Description TEXT,
                        Category TEXT,
                        Image TEXT,
                        PdfData TEXT,
                        PdfFileName TEXT,
                        Status TEXT,
                        CreatedAt TEXT
                    );");

                context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS Services (
                        Id TEXT PRIMARY KEY,
                        Slug TEXT,
                        Title TEXT,
                        Description TEXT,
                        Category TEXT,
                        Icon TEXT,
                        Image TEXT,
                        Status TEXT,
                        CreatedAt TEXT
                    );");

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN SubCategory TEXT;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN TrustBadges TEXT;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD COLUMN Location TEXT;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Testimonials ADD COLUMN Image TEXT;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Blogs ADD COLUMN LinkText TEXT;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Blogs ADD COLUMN Hyperlink TEXT;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD COLUMN Position INTEGER DEFAULT 0;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE SubCategories ADD COLUMN Position INTEGER DEFAULT 0;");
                }
                catch { /* Ignored if column already exists */ }

                try { context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD COLUMN MetaTitle TEXT;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD COLUMN MetaDescription TEXT;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE SubCategories ADD COLUMN MetaTitle TEXT;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE SubCategories ADD COLUMN MetaDescription TEXT;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN MetaTitle TEXT;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN MetaDescription TEXT;"); } catch { }
            }
            else
            {
                // SQL Server table migrations
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Services]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [Services] (
                            [Id] nvarchar(450) NOT NULL,
                            [Slug] nvarchar(max) NOT NULL,
                            [Title] nvarchar(max) NOT NULL,
                            [Description] nvarchar(max) NULL,
                            [Category] nvarchar(max) NULL,
                            [Icon] nvarchar(max) NULL,
                            [Image] nvarchar(max) NOT NULL,
                            [Status] nvarchar(max) NOT NULL,
                            [CreatedAt] datetime2 NOT NULL,
                            CONSTRAINT [PK_Services] PRIMARY KEY ([Id])
                        );
                    END");

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD Location nvarchar(max) NULL;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD TrustBadges nvarchar(max) NULL;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Testimonials ADD Image nvarchar(max) NULL;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Blogs ADD LinkText nvarchar(max) NULL;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Blogs ADD Hyperlink nvarchar(max) NULL;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD Position int NOT NULL DEFAULT 0;");
                }
                catch { /* Ignored if column already exists */ }

                try
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE SubCategories ADD Position int NOT NULL DEFAULT 0;");
                }
                catch { /* Ignored if column already exists */ }

                try { context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD MetaTitle nvarchar(max) NULL;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD MetaDescription nvarchar(max) NULL;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE SubCategories ADD MetaTitle nvarchar(max) NULL;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE SubCategories ADD MetaDescription nvarchar(max) NULL;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD MetaTitle nvarchar(max) NULL;"); } catch { }
                try { context.Database.ExecuteSqlRaw("ALTER TABLE Products ADD MetaDescription nvarchar(max) NULL;"); } catch { }
            }

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
                    supportEmail = "info@westerninterio.in",
                    phoneNumber = "+91-9540641111, +91-9540017776",
                    storeAddress = "Plot No. 06, Gali No.-06, Kadipur Industrial Area, Gurugram, Haryana – 122505"
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

            // Build hierarchy map from navigation.json
            var subCatToParentMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var navPath = Path.Combine(dataPath, "navigation.json");
            if (File.Exists(navPath))
            {
                try
                {
                    var navJson = File.ReadAllText(navPath);
                    using (var doc = JsonDocument.Parse(navJson))
                    {
                        foreach (var catElem in doc.RootElement.EnumerateArray())
                        {
                            string catId = catElem.GetProperty("id").GetString() ?? "";
                            if (catElem.TryGetProperty("columns", out var colsElem))
                            {
                                foreach (var colElem in colsElem.EnumerateArray())
                                {
                                    if (colElem.TryGetProperty("items", out var itemsElem))
                                    {
                                        foreach (var itemElem in itemsElem.EnumerateArray())
                                        {
                                            string subSlug = itemElem.GetProperty("slug").GetString() ?? "";
                                            if (!string.IsNullOrEmpty(subSlug))
                                            {
                                                subCatToParentMap[subSlug] = catId;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing navigation.json: {ex.Message}");
                }
            }

            // 3. Seed Root Categories
            if (!context.Categories.Any())
            {
                try
                {
                    var rootCategories = new List<Category>
                    {
                        new Category
                        {
                            Id = "office-furniture",
                            Slug = "office-furniture",
                            Name = "Office Furniture",
                            Description = "Elevate your work environment with our ergonomic desking systems, executive series tables, and collaborative storage units.",
                            Image = "https://images.unsplash.com/photo-1497366216548-37526070297c?q=80&w=2070&auto=format&fit=crop",
                            Status = "Active",
                            Location = "Header"
                        },
                        new Category
                        {
                            Id = "home-furniture",
                            Slug = "home-furniture",
                            Name = "Home Furniture",
                            Description = "Craft a sanctuary of style and comfort. Handcrafted tables, modular kitchens, and elegant storage layouts for modern living.",
                            Image = "https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?q=80&w=2070&auto=format&fit=crop",
                            Status = "Active",
                            Location = "Header"
                        },
                        new Category
                        {
                            Id = "chairs",
                            Slug = "chairs",
                            Name = "Chairs",
                            Description = "Engineered for absolute posture support and long-term seating comfort. Explore our CEO, executive, and staff collections.",
                            Image = "https://images.unsplash.com/photo-1580481072645-022f9a6dbf27?q=80&w=2070&auto=format&fit=crop",
                            Status = "Active",
                            Location = "Header"
                        },
                        new Category
                        {
                            Id = "interior-design",
                            Slug = "interior-design",
                            Name = "Interior Design",
                            Description = "Transform your corporate space. Complete turnkey workspace layouts, partitions, false ceilings, and flooring design solutions.",
                            Image = "https://images.unsplash.com/photo-1505797149-43b0069ec26b?q=80&w=2071&auto=format&fit=crop",
                            Status = "Active",
                            Location = "Header"
                        }
                    };
                    context.Categories.AddRange(rootCategories);
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error seeding categories: {ex.Message}");
                }
            }

            // Backfill null/empty Location values to "Header" for existing categories
            try
            {
                var nullLocationCats = context.Categories.Where(c => c.Location == null || c.Location == "").ToList();
                if (nullLocationCats.Any())
                {
                    foreach (var cat in nullLocationCats)
                    {
                        cat.Location = "Header";
                    }
                    context.SaveChanges();
                    Console.WriteLine($"[Seeding] Backfilled {nullLocationCats.Count} categories with 'Header' location.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seeding] Error backfilling categories: {ex.Message}");
            }

            // 3b. Seed SubCategories
            if (!context.SubCategories.Any())
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
                            var subCats = rawCats.Select(c => {
                                string id = c.GetProperty("id").GetString() ?? "";
                                string slug = c.TryGetProperty("slug", out var s) ? (s.GetString() ?? id) : id;
                                string name = c.GetProperty("name").GetString() ?? "";
                                string description = c.GetProperty("description").GetString() ?? "";
                                string image = c.GetProperty("image").GetString() ?? "";
                                
                                // Find parent category
                                if (!subCatToParentMap.TryGetValue(slug, out var parentId))
                                {
                                    parentId = GetParentCategoryFallback(slug);
                                }
                                
                                return new SubCategory
                                {
                                    Id = id,
                                    Slug = slug,
                                    Name = name,
                                    Description = description,
                                    Image = image,
                                    CategoryId = parentId,
                                    Status = "Active"
                                };
                            }).ToList();
                            context.SubCategories.AddRange(subCats);
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding subcategories: {ex.Message}");
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

                                // Map category and subcategory
                                string originalCategory = p.Category;
                                p.SubCategory = originalCategory;
                                if (!subCatToParentMap.TryGetValue(originalCategory, out var parentId))
                                {
                                    parentId = GetParentCategoryFallback(originalCategory);
                                }
                                p.Category = parentId;
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
                foreach (var cat in cats)
                {
                    var catId = cat.Id.ToLower();
                    var catSlug = (cat.Slug ?? "").ToLower();
                    cat.Count = context.Products.Count(p => 
                        p.Category.ToLower() == catId || 
                        p.Category.ToLower() == catSlug
                    );
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating category counts: {ex.Message}");
            }

            // 10. Seed Testimonials from site-content.json
            if (!context.Testimonials.Any())
            {
                var filePath = Path.Combine(dataPath, "site-content.json");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        using (var doc = JsonDocument.Parse(json))
                        {
                            if (doc.RootElement.TryGetProperty("testimonialsPage", out var testimonialsPageElem) &&
                                testimonialsPageElem.TryGetProperty("items", out var itemsElem))
                            {
                                var testimonials = new List<Testimonial>();
                                foreach (var item in itemsElem.EnumerateArray())
                                {
                                    testimonials.Add(new Testimonial
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Author = item.GetProperty("author").GetString() ?? "",
                                        Designation = item.GetProperty("designation").GetString() ?? "",
                                        Company = item.GetProperty("company").GetString() ?? "",
                                        Quote = item.GetProperty("quote").GetString() ?? "",
                                        Rating = item.TryGetProperty("rating", out var r) ? r.GetInt32() : 5,
                                        Category = item.TryGetProperty("category", out var c) ? (c.GetString() ?? "") : "",
                                        Status = "Active",
                                        CreatedAt = DateTime.UtcNow,
                                        Image = ""
                                    });
                                }
                                context.Testimonials.AddRange(testimonials);
                                context.SaveChanges();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding testimonials: {ex.Message}");
                    }
                }
            }

            // 11. Seed Catalogues from download-center.json
            if (!context.Catalogues.Any())
            {
                var filePath = Path.Combine(dataPath, "download-center.json");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        using (var doc = JsonDocument.Parse(json))
                        {
                            var catalogues = new List<Catalogue>();
                            foreach (var item in doc.RootElement.EnumerateArray())
                            {
                                catalogues.Add(new Catalogue
                                  {
                                    Id = Guid.NewGuid().ToString(),
                                    Title = item.GetProperty("title").GetString() ?? "",
                                    Description = item.GetProperty("description").GetString() ?? "",
                                    Category = item.GetProperty("category").GetString() ?? "",
                                    Image = item.GetProperty("image").GetString() ?? "",
                                    PdfData = "",
                                    PdfFileName = "",
                                    Status = "Active",
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                            context.Catalogues.AddRange(catalogues);
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding catalogues: {ex.Message}");
                    }
                }
            }

            // 12. Seed Services
            if (!context.Services.Any())
            {
                var services = new List<Service>
                {
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "modular-partition", Title = "Modular Partitions", Description = "Flexible, soundproof, and reconfigurable workspace divider systems optimized for dynamic floor planning.", Category = "partitions", Icon = "Layers", Image = "https://images.unsplash.com/photo-1497215728101-856f4ea42174?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "glass-gypsum-partition", Title = "Glass & Gypsum Partitions", Description = "Seamless double-glazed glass panels and sturdy gypsum structures that balance spatial light and acoustic isolation.", Category = "partitions", Icon = "Columns", Image = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "solid-wall-partition", Title = "Solid Wall Partitions", Description = "Sturdy, fully sound-insulated floor-to-ceiling solid partitions designed to secure executive privacy and boardroom focus.", Category = "partitions", Icon = "Maximize", Image = "https://images.unsplash.com/photo-1497366216548-37526070297c?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "aluminum-partitions", Title = "Aluminum Partitions", Description = "Highly durable, sleek-profile aluminum frames customized with composite panel integrations for modern partitions.", Category = "partitions", Icon = "Paintbrush", Image = "https://images.unsplash.com/photo-1513694203232-719a280e022f?q=80&w=2069&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "wooden-flooring", Title = "Wooden Flooring", Description = "Premium engineered oak, walnut, and teak floor layouts tailored to elevate leadership suites and high-end boardrooms.", Category = "flooring", Icon = "Trees", Image = "https://images.unsplash.com/photo-1581850518616-bcb8186c3f30?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "vinyl-flooring", Title = "Vinyl Flooring", Description = "Heavy-duty resilient sheets and interlocking plank systems engineered to withstand heavy, high-traffic commercial environments.", Category = "flooring", Icon = "Shield", Image = "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "vitrified-tiles", Title = "Vitrified Tiles", Description = "Luxury double-charged vitrified tile grids styled for impressive high-gloss reception lobbies and corporate cafeterias.", Category = "flooring", Icon = "LayoutGrid", Image = "https://images.unsplash.com/photo-1615066390971-03e4e1c36ddf?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "false-flooring", Title = "False Flooring", Description = "Raised modular access floor paneling designed to route hidden electrical, server cooling, and telecom networking grids.", Category = "flooring", Icon = "Grid", Image = "https://images.unsplash.com/photo-1595844730298-b960ff98fee0?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "false-ceiling-design", Title = "False Ceiling Design", Description = "Aesthetic suspended ceiling rafters, ambient grid ceilings, and drop light designs integrating smart office utilities.", Category = "ceiling", Icon = "Cloud", Image = "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "ac-ducting", Title = "AC & Ducting", Description = "Precision-designed central HVAC ventilation, VRV systems, and custom duct installations for optimal indoor airflow.", Category = "ceiling", Icon = "Wind", Image = "https://images.unsplash.com/photo-1581094288338-2314dddb7ec4?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "electrical-networking", Title = "Electrical & Networking", Description = "Certified commercial power cabling, safety distribution boards, and structured server room networking installations.", Category = "infrastructure", Icon = "Zap", Image = "https://images.unsplash.com/photo-1517502884422-41eaead166d4?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "office-interiors", Title = "Office Interiors", Description = "End-to-end turnkey office layout execution, spatial fitouts, and strategic employee desks architecture.", Category = "infrastructure", Icon = "Layout", Image = "https://images.unsplash.com/photo-1497366454035-f200968a6e72?q=80&w=2070&auto=format&fit=crop", Status = "Active" },
                    new Service { Id = Guid.NewGuid().ToString(), Slug = "interiors-decoration", Title = "Interiors & Decoration", Description = "Tailored brand styling elements, custom wall graphic claddings, ergonomic accents, and premium office artifacts.", Category = "infrastructure", Icon = "Sparkles", Image = "https://images.unsplash.com/photo-1618219908412-a29a1bb7b86e?q=80&w=2070&auto=format&fit=crop", Status = "Active" }
                };
                context.Services.AddRange(services);
                context.SaveChanges();
            }

            // 13. Run Base64 data migration to files
            try
            {
                MigrateBase64ToFiles(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error migrating legacy base64 data: {ex.Message}");
            }
        }

        public static void MigrateBase64ToFiles(AppDbContext context)
        {
            Console.WriteLine("[Migration] Starting migration of base64 data to physical files...");
            context.Database.SetCommandTimeout(300);
            FileStorageService.EnforceLimits = false;
            try
            {
            
            // 1. Categories
            var categories = context.Categories.ToList();
            foreach (var cat in categories)
            {
                if (FileStorageService.IsBase64DataUrl(cat.Image))
                {
                    try
                    {
                        cat.Image = FileStorageService.SaveBase64File(cat.Image, "category");
                        Console.WriteLine($"[Migration] Migrated image for category: {cat.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate image for category {cat.Name}: {ex.Message}");
                    }
                }
            }

            // 2. SubCategories
            var subCategories = context.SubCategories.ToList();
            foreach (var sub in subCategories)
            {
                if (FileStorageService.IsBase64DataUrl(sub.Image))
                {
                    try
                    {
                        sub.Image = FileStorageService.SaveBase64File(sub.Image, "subcategory");
                        Console.WriteLine($"[Migration] Migrated image for subcategory: {sub.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate image for subcategory {sub.Name}: {ex.Message}");
                    }
                }
            }

            // 3. Brands
            var brands = context.Brands.ToList();
            foreach (var brand in brands)
            {
                if (FileStorageService.IsBase64DataUrl(brand.Url))
                {
                    try
                    {
                        brand.Url = FileStorageService.SaveBase64File(brand.Url, "brand");
                        Console.WriteLine($"[Migration] Migrated image for brand: {brand.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate image for brand {brand.Name}: {ex.Message}");
                    }
                }
            }

            // 4. Gallery
            var galleryItems = context.Gallery.ToList();
            foreach (var item in galleryItems)
            {
                if (FileStorageService.IsBase64DataUrl(item.Image))
                {
                    try
                    {
                        item.Image = FileStorageService.SaveBase64File(item.Image, "gallery");
                        Console.WriteLine($"[Migration] Migrated image for gallery item: {item.Title}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate image for gallery item {item.Title}: {ex.Message}");
                    }
                }
            }

            // 5. Blogs
            var blogs = context.Blogs.ToList();
            foreach (var blog in blogs)
            {
                if (FileStorageService.IsBase64DataUrl(blog.Image))
                {
                    try
                    {
                        blog.Image = FileStorageService.SaveBase64File(blog.Image, "blog");
                        Console.WriteLine($"[Migration] Migrated image for blog: {blog.Title}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate image for blog {blog.Title}: {ex.Message}");
                    }
                }
            }

            // 6. Catalogues
            var catalogues = context.Catalogues.ToList();
            foreach (var cat in catalogues)
            {
                if (FileStorageService.IsBase64DataUrl(cat.Image))
                {
                    try
                    {
                        cat.Image = FileStorageService.SaveBase64File(cat.Image, "catalogue");
                        Console.WriteLine($"[Migration] Migrated cover image for catalogue: {cat.Title}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate cover image for catalogue {cat.Title}: {ex.Message}");
                    }
                }
                if (FileStorageService.IsBase64DataUrl(cat.PdfData))
                {
                    try
                    {
                        cat.PdfData = FileStorageService.SaveBase64File(cat.PdfData, "catalogue/pdfs");
                        Console.WriteLine($"[Migration] Migrated PDF data for catalogue: {cat.Title}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate PDF data for catalogue {cat.Title}: {ex.Message}");
                    }
                }
            }

            // 7. Products
            var products = context.Products.ToList();
            foreach (var prod in products)
            {
                bool changed = false;
                if (FileStorageService.IsBase64DataUrl(prod.Image ?? ""))
                {
                    try
                    {
                        prod.Image = FileStorageService.SaveBase64File(prod.Image!, "product");
                        changed = true;
                        Console.WriteLine($"[Migration] Migrated primary image for product: {prod.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate primary image for product {prod.Name}: {ex.Message}");
                    }
                }

                if (prod.Images != null && prod.Images.Count > 0)
                {
                    for (int i = 0; i < prod.Images.Count; i++)
                    {
                        if (FileStorageService.IsBase64DataUrl(prod.Images[i]))
                        {
                            try
                            {
                                prod.Images[i] = FileStorageService.SaveBase64File(prod.Images[i], "product");
                                changed = true;
                                Console.WriteLine($"[Migration] Migrated secondary image {i+1} for product: {prod.Name}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Migration] Failed to migrate secondary image {i+1} for product {prod.Name}: {ex.Message}");
                            }
                        }
                    }
                }

                if (FileStorageService.IsBase64DataUrl(prod.BlueprintImage ?? ""))
                {
                    try
                    {
                        prod.BlueprintImage = FileStorageService.SaveBase64File(prod.BlueprintImage!, "product");
                        changed = true;
                        Console.WriteLine($"[Migration] Migrated blueprint image for product: {prod.Name}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate blueprint image for product {prod.Name}: {ex.Message}");
                    }
                }

                if (prod.Resources != null && prod.Resources.Count > 0)
                {
                    foreach (var res in prod.Resources)
                    {
                        if (FileStorageService.IsBase64DataUrl(res.FileData ?? ""))
                        {
                            try
                            {
                                res.FileData = FileStorageService.SaveBase64File(res.FileData!, "product/resources");
                                changed = true;
                                Console.WriteLine($"[Migration] Migrated resource '{res.Title}' for product: {prod.Name}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Migration] Failed to migrate resource '{res.Title}' for product {prod.Name}: {ex.Message}");
                            }
                        }
                    }
                }

                if (changed)
                {
                    context.Entry(prod).State = EntityState.Modified;
                }
            }

            // 8. Services
            var dbServices = context.Services.ToList();
            foreach (var service in dbServices)
            {
                if (FileStorageService.IsBase64DataUrl(service.Image))
                {
                    try
                    {
                        service.Image = FileStorageService.SaveBase64File(service.Image, "service");
                        Console.WriteLine($"[Migration] Migrated image for service: {service.Title}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate image for service {service.Title}: {ex.Message}");
                    }
                }
            }

            // 9. Settings
            var settings = context.Settings.ToList();
            foreach (var setting in settings)
            {
                if (setting.Value.Contains("data:") && setting.Value.Contains(";base64,"))
                {
                    try
                    {
                        var node = JsonNode.Parse(setting.Value);
                        var moduleName = setting.Key.ToLower().Replace("bdm_settings_", "");
                        var processedNode = ProcessSettingJsonNode(node, moduleName);
                        if (processedNode != null)
                        {
                            setting.Value = processedNode.ToJsonString();
                            context.Entry(setting).State = EntityState.Modified;
                            Console.WriteLine($"[Migration] Migrated base64 images in setting: {setting.Key}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Migration] Failed to migrate setting {setting.Key}: {ex.Message}");
                    }
                }
            }

            context.SaveChanges();
            Console.WriteLine("[Migration] Base64 migration completed successfully.");
            }
            finally
            {
                FileStorageService.EnforceLimits = true;
            }
        }

        private static JsonNode? ProcessSettingJsonNode(JsonNode? node, string moduleName)
        {
            if (node == null) return null;

            if (node is JsonObject obj)
            {
                var keys = obj.Select(k => k.Key).ToList();
                foreach (var key in keys)
                {
                    var val = obj[key];
                    if (val is JsonValue jsonVal && jsonVal.TryGetValue<string>(out var strValue))
                    {
                        if (FileStorageService.IsBase64DataUrl(strValue))
                        {
                            obj[key] = FileStorageService.SaveBase64File(strValue, moduleName);
                        }
                    }
                    else
                    {
                        ProcessSettingJsonNode(val, moduleName);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    var val = arr[i];
                    if (val is JsonValue jsonVal && jsonVal.TryGetValue<string>(out var strValue))
                    {
                        if (FileStorageService.IsBase64DataUrl(strValue))
                        {
                            arr[i] = FileStorageService.SaveBase64File(strValue, moduleName);
                        }
                    }
                    else
                    {
                        ProcessSettingJsonNode(val, moduleName);
                    }
                }
            }

            return node;
        }

        private static string GetParentCategoryFallback(string subCatSlug)
        {
            var s = subCatSlug.ToLower();
            if (s.Contains("chair")) return "chairs";
            if (s.Contains("sofa")) return "chairs";
            if (s.Contains("desk") || s.Contains("table") || s.Contains("workstation") || s.Contains("storage") || s.Contains("partition")) return "office-furniture";
            if (s.Contains("interior") || s.Contains("design") || s.Contains("ceiling")) return "interior-design";
            return "home-furniture";
        }
    }
}
