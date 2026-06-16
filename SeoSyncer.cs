using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using western_backend.Models;

namespace western_backend
{
    public static class SeoSyncer
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly Dictionary<string, string> SubcatUrls = new Dictionary<string, string>
        {
            { "desking-workstation", "https://westernofficesolutions.com/products/modern-office-desk-design/" },
            { "executive-tables", "https://westernofficesolutions.com/products/executive-tables/" },
            { "reception-series", "https://westernofficesolutions.com/products/reception-series-tables/" },
            { "computer-tables", "https://westernofficesolutions.com/products/computer-tables/" },
            { "centre-tables", "https://westernofficesolutions.com/products/centre-tables-in-gurgon/" },
            { "conference-meeting", "https://westernofficesolutions.com/products/conference-tables/" },
            { "office-storage", "https://westernofficesolutions.com/products/office-storage/" },
            { "office-table-general", "https://westernofficesolutions.com/products/office-table/" },
            { "ceo-series-chairs", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/ceo-series-chairs/" },
            { "office-executive-chairs", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/office-executive-chairs-in-gurgaon/" },
            { "office-workstation", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/workstation-series-chairs/" },
            { "visitor-training", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/office-training-chair-gurgaon/" },
            { "waiting-area-series", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/waiting-area-series-chairs/" },
            { "cafeteria-bar-chairs", "https://westernofficesolutions.com/products/cafeteria-chairs-in-gurgaon/" },
            { "sofas-series", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/sofas/" },
            { "office-chairs-online", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/office-chairs-online-in-gurgaon/" },
            { "chair-manufacturer", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/" },
            { "home-furniture-series", "https://westernofficesolutions.com/products/home-furniture-series/" },
            { "modular-kitchen-series", "https://westernofficesolutions.com/products/modular-kitchen-series/" }
        };

        private static readonly Dictionary<string, string> CatUrls = new Dictionary<string, string>
        {
            { "office-furniture", "https://westernofficesolutions.com/products/modern-office-desk-design/" },
            { "chairs", "https://westernofficesolutions.com/office-chair-manufacturer-in-gurgaon/" },
            { "home-furniture", "https://westernofficesolutions.com/products/home-furniture-series/" }
        };

        public static async Task Sync(AppDbContext db)
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // 1. Categories
            Console.WriteLine("\n--- Syncing Categories ---");
            var categories = await db.Categories.ToListAsync();
            Console.WriteLine($"Found {categories.Count} categories in active database.");

            foreach (var cat in categories)
            {
                var targetKey = string.IsNullOrEmpty(cat.Slug) ? cat.Id : cat.Slug;
                if (CatUrls.TryGetValue(targetKey, out var url))
                {
                    var (title, desc) = await FetchSeoTags(url);
                    if (!string.IsNullOrEmpty(title))
                    {
                        cat.MetaTitle = title;
                        cat.MetaDescription = desc;
                        Console.WriteLine($"  Updated Category '{cat.Name}' -> Title: '{title.Substring(0, Math.Min(40, title.Length))}...', Desc: '{desc.Substring(0, Math.Min(40, desc.Length))}...'");
                    }
                }
                else
                {
                    Console.WriteLine($"  Skipping Category '{cat.Name}' (no live URL mapped).");
                }
            }
            await db.SaveChangesAsync();

            // 2. SubCategories
            Console.WriteLine("\n--- Syncing SubCategories ---");
            var subcategories = await db.SubCategories.ToListAsync();
            Console.WriteLine($"Found {subcategories.Count} subcategories in active database.");

            foreach (var sub in subcategories)
            {
                var targetKey = string.IsNullOrEmpty(sub.Slug) ? sub.Id : sub.Slug;
                if (SubcatUrls.TryGetValue(targetKey, out var url))
                {
                    var (title, desc) = await FetchSeoTags(url);
                    if (!string.IsNullOrEmpty(title))
                    {
                        sub.MetaTitle = title;
                        sub.MetaDescription = desc;
                        Console.WriteLine($"  Updated SubCategory '{sub.Name}' -> Title: '{title.Substring(0, Math.Min(40, title.Length))}...', Desc: '{desc.Substring(0, Math.Min(40, desc.Length))}...'");
                    }
                }
                else
                {
                    Console.WriteLine($"  Skipping SubCategory '{sub.Name}' (no live URL mapped).");
                }
            }
            await db.SaveChangesAsync();

            // 3. Products (Auto-generate optimized SEO based on existing DB relationships)
            Console.WriteLine("\n--- Auto-Generating Product SEO Metadata ---");
            var products = await db.Products.ToListAsync();
            Console.WriteLine($"Found {products.Count} products in active database.");

            var catMap = categories.ToDictionary(c => c.Id, c => c.Name);
            var subcatMap = subcategories.ToDictionary(s => s.Id, s => s.Name);

            int updatedProducts = 0;
            foreach (var prod in products)
            {
                var catName = catMap.TryGetValue(prod.Category, out var cName) ? cName : "Office Furniture";
                var subcatName = !string.IsNullOrEmpty(prod.SubCategory) && subcatMap.TryGetValue(prod.SubCategory, out var sName) ? sName : "";

                string metaTitle;
                string metaDesc;

                if (!string.IsNullOrEmpty(subcatName))
                {
                    metaTitle = $"{prod.Name} - {subcatName} | Western Office Solutions";
                    metaDesc = $"Buy premium {prod.Name} {subcatName.ToLower()} from Western Office Solutions, the leading office furniture manufacturer in Gurgaon & Delhi NCR. Ergonomic designs, premium finishes.";
                }
                else
                {
                    metaTitle = $"{prod.Name} - {catName} | Western Office Solutions";
                    metaDesc = $"Buy premium {prod.Name} from our {catName.ToLower()} range at Western Office Solutions. Top-quality ergonomic office furniture in Gurgaon & Delhi NCR. Call for prices.";
                }

                if (metaTitle.Length > 70)
                {
                    metaTitle = metaTitle.Substring(0, 67) + "...";
                }
                if (metaDesc.Length > 160)
                {
                    metaDesc = metaDesc.Substring(0, 157) + "...";
                }

                prod.MetaTitle = metaTitle;
                prod.MetaDescription = metaDesc;
                updatedProducts++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"Successfully generated SEO meta tags for {updatedProducts} products.");
        }

        private static async Task<(string Title, string Description)> FetchSeoTags(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);

                var titleMatch = Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.IgnoreCase);
                var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "";
                title = System.Net.WebUtility.HtmlDecode(title);

                var desc = GetMetaTagValue(html, "description");
                if (string.IsNullOrEmpty(desc))
                {
                    desc = GetMetaTagValue(html, "og:description");
                }

                return (title, desc);
            }
            catch (Exception e)
            {
                Console.WriteLine($"  Error crawling {url}: {e.Message}");
                return ("", "");
            }
        }

        private static string GetMetaTagValue(string html, string name)
        {
            var patterns = new[] {
                $@"<meta[^>]*?name=[""']{name}[""'][^>]*?content=[""'](.*?)[""']",
                $@"<meta[^>]*?content=[""'](.*?)[""'][^>]*?name=[""']{name}[""']",
                $@"<meta[^>]*?property=[""']{name}[""'][^>]*?content=[""'](.*?)[""']",
                $@"<meta[^>]*?content=[""'](.*?)[""'][^>]*?property=[""']{name}[""']"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                {
                    return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
                }
            }
            return "";
        }
    }
}
