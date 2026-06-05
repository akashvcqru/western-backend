using System.Collections.Generic;

namespace western_backend.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }

    public class Category
    {
        public string Id { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Image { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string? Location { get; set; } = "Header";
    }

    public class SubCategory
    {
        public string Id { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }

    public class Brand
    {
        public string Id { get; set; } = string.Empty; // Guid string
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
    }

    public class GalleryItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }

    public class BlogPost
    {
        public string Id { get; set; } = string.Empty; // Slug
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string ReadTime { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Author { get; set; } = "Admin";
        public string AuthorRole { get; set; } = "Western Interio Admin";
        public List<string> Tags { get; set; } = new();
        public List<string> Content { get; set; } = new();
        public string? LinkText { get; set; } = string.Empty;
        public string? Hyperlink { get; set; } = string.Empty;
    }

    public class Inquiry
    {
        public long Id { get; set; } // Can represent timestamp or autoincrement ID
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
    }

    public class Setting
    {
        public string Key { get; set; } = string.Empty; // e.g. bdm_settings_contact, bdm_settings_social
        public string Value { get; set; } = string.Empty; // JSON serialization
    }

    public class Testimonial
    {
        public string Id { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Quote { get; set; } = string.Empty;
        public int Rating { get; set; } = 5;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public System.DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;
        public string Image { get; set; } = string.Empty;
    }

    public class Catalogue
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string PdfData { get; set; } = string.Empty;
        public string PdfFileName { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public System.DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;
    }

    public class Product
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? SubCategory { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int Stock { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
        public string? Image { get; set; }
        public string? CatNo { get; set; }
        public string? BlueprintImage { get; set; }
        public string? Material { get; set; }
        public string? Finish { get; set; }
        public string? Size { get; set; }
        public List<FeatureItem> Features { get; set; } = new();
        public List<SpecificationItem> Specifications { get; set; } = new();
        public List<DimensionItem> Dimensions { get; set; } = new();
        public List<ResourceItem> Resources { get; set; } = new();
        public List<VariantItem> Variants { get; set; } = new();
        public List<SwatchItem> Swatches { get; set; } = new();
        public string? DetailsTitle { get; set; }
        public string? DetailsText1 { get; set; }
        public string? DetailsText2 { get; set; }
        public List<string> QuickSpecs { get; set; } = new();
    }

    public class FeatureItem
    {
        public string Title { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
    }

    public class SpecificationItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class DimensionItem
    {
        public string Name { get; set; } = string.Empty;
        public string Range { get; set; } = string.Empty;
        public string Coord { get; set; } = string.Empty;
    }

    public class ResourceItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string? FileData { get; set; }
        public string? FileName { get; set; }
    }

    public class VariantItem
    {
        public string Label { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
    }

    public class SwatchItem
    {
        public string Category { get; set; } = string.Empty;
        public List<SwatchOption> Options { get; set; } = new();
    }

    public class SwatchOption
    {
        public string Name { get; set; } = string.Empty;
        public string Hex { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public bool Border { get; set; }
    }

    public class Service
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Icon { get; set; }
        public string Image { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public System.DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;
    }
}
