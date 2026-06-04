using System;
using System.IO;
using System.Text.RegularExpressions;

namespace western_backend
{
    public static class FileStorageService
    {
        private static readonly string UploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB limit
        public static bool EnforceLimits { get; set; } = true;

        public static bool IsBase64DataUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return value.StartsWith("data:") && value.Contains(";base64,");
        }

        public static string SaveBase64File(string base64DataUrl, string moduleName, string? existingFilePath = null)
        {
            if (!IsBase64DataUrl(base64DataUrl))
            {
                return base64DataUrl; // Return as-is if it's already a URL or path
            }

            try
            {
                // Parse the data URL
                // Format: data:[mime/type];base64,[data]
                var match = Regex.Match(base64DataUrl, @"^data:(?<mime>[\w/\-\+\.]+);base64,(?<data>.+)$");
                if (!match.Success)
                {
                    throw new ArgumentException("Invalid base64 data URL format.");
                }

                string mimeType = match.Groups["mime"].Value;
                string base64Data = match.Groups["data"].Value;

                // Validate and map mime type to extension
                string extension = GetExtensionFromMimeType(mimeType);
                if (string.IsNullOrEmpty(extension))
                {
                    throw new InvalidDataException($"Mime type '{mimeType}' is not allowed.");
                }

                // Decode base64
                byte[] fileBytes = Convert.FromBase64String(base64Data);

                // Validate file size
                long limit = MaxFileSizeBytes;
                bool isImage = mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
                if (EnforceLimits && isImage)
                {
                    limit = 100 * 1024; // 100 KB limit for images
                }

                if (fileBytes.Length > limit)
                {
                    if (EnforceLimits && isImage)
                    {
                        throw new InvalidDataException("Image size exceeds the limit of 100 KB.");
                    }
                    else
                    {
                        throw new InvalidDataException($"File size exceeds the limit of {MaxFileSizeBytes / (1024 * 1024)} MB.");
                    }
                }

                // Create module folder if it doesn't exist
                string moduleFolder = Path.Combine(UploadsFolder, moduleName.ToLower().Trim());
                if (!Directory.Exists(moduleFolder))
                {
                    Directory.CreateDirectory(moduleFolder);
                }

                // Generate unique filename
                string uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string physicalPath = Path.Combine(moduleFolder, uniqueFileName);

                // Write file to disk
                File.WriteAllBytes(physicalPath, fileBytes);

                // Delete old file if present to clean up disk space
                if (!string.IsNullOrEmpty(existingFilePath))
                {
                    DeleteFile(existingFilePath);
                }

                // Return web path (e.g. /uploads/category/unique-id.png)
                return $"/uploads/{moduleName.ToLower().Trim()}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileStorageService] Error saving file: {ex.Message}");
                throw;
            }
        }

        public static void DeleteFile(string? webPath)
        {
            if (string.IsNullOrEmpty(webPath) || !webPath.StartsWith("/uploads/"))
            {
                return;
            }

            try
            {
                // Normalize path to prevent directory traversal
                string relativePath = webPath.TrimStart('/');
                string physicalPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
                string uploadsFullPath = Path.GetFullPath(UploadsFolder);

                // Security check: ensure path is within uploads folder
                if (physicalPath.StartsWith(uploadsFullPath, StringComparison.OrdinalIgnoreCase) && File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                    Console.WriteLine($"[FileStorageService] Deleted old file: {physicalPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileStorageService] Error deleting file '{webPath}': {ex.Message}");
            }
        }

        private static string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType.ToLower().Trim() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/svg+xml" => ".svg",
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                "application/zip" => ".zip",
                "application/x-zip-compressed" => ".zip",
                "text/plain" => ".txt",
                _ => string.Empty
            };
        }
    }
}
