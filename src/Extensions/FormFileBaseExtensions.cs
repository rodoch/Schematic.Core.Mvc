using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Schematic.Core.Mvc
{
    public static class FormFileBaseExtensions
    {
        public const int ImageMinimumBytes = 512;

        public static bool IsImage(this IFormFile file)
        {
            // Check the image mime types
            if (file.ContentType.ToLower() != "image/jpg"
                && file.ContentType.ToLower() != "image/jpeg"
                && file.ContentType.ToLower() != "image/pjpeg"
                && file.ContentType.ToLower() != "image/gif"
                && file.ContentType.ToLower() != "image/x-png"
                && file.ContentType.ToLower() != "image/png")
            {
                return false;
            }

            // Check the image extension
            if (Path.GetExtension(file.FileName).ToLower() != ".jpg"
                && Path.GetExtension(file.FileName).ToLower() != ".png"
                && Path.GetExtension(file.FileName).ToLower() != ".gif"
                && Path.GetExtension(file.FileName).ToLower() != ".jpeg")
            {
                return false;
            }

            // Attempt to read the file and check the first bytes
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    if (!stream.CanRead)
                    {
                        return false;
                    }
                    
                    if (file.Length < ImageMinimumBytes)
                    {
                        return false;
                    }

                    byte[] buffer = new byte[ImageMinimumBytes];
                    stream.Read(buffer, 0, ImageMinimumBytes);
                    string content = System.Text.Encoding.UTF8.GetString(buffer);
                    
                    if (Regex.IsMatch(content,
                        @"<script|<html|<head|<title|<body|<pre|<table|<a\s+href|<img|<plaintext|<cross\-domain\-policy",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline))
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            // Try to instantiate new RGB image
            // If exception is thrown we can assume that it's not a valid image
            using (var stream = file.OpenReadStream())
            {
                try
                {
                    using (Image<Rgba32> image = Image.Load(stream)) {}
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    stream.Position = 0;
                }
            }

            return true;
        }
    }
}