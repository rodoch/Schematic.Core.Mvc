using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Schematic.Core;
using Schematic.Core.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Schematic.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class AssetController : Controller
    {
        protected readonly IConfiguration Configuration;
        protected readonly IAssetRepository AssetRepository;
        protected readonly IImageAssetRepository ImageRepository;

        public AssetController(
            IConfiguration configuration,
            IAssetRepository assetRepository,
            IImageAssetRepository imageRepository)
        {
            Configuration = configuration;
            AssetRepository = assetRepository;
            ImageRepository = imageRepository;
        }

        protected string FileName { get; set; }
        protected string FilePath { get; set; }
        protected int ImageHeight { get; set; }
        protected int ImageWidth { get; set; }
        protected long TotalSize { get; set; }

        protected ClaimsIdentity ClaimsIdentity => User.Identity as ClaimsIdentity;
        protected int UserID => int.Parse(ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        [Route("image/{fileName}")]
        [HttpGet]
        public IActionResult ImageProxy(string fileName)
        {
            FilePath = Path.Combine(Configuration["AppSettings:ContentDirectory"], fileName);

            if (!System.IO.File.Exists(FilePath))
            {
                return NotFound();
            }

            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(fileName, out string contentType))
            {
                contentType = "application/octet-stream";
            }

            var image = System.IO.File.OpenRead(FilePath);
            return File(image, contentType);
        }

        [Route("upload")]
        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            TotalSize = files.Sum(f => f.Length);
            var response = new List<AssetUploadResponse>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    FileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');

                    var fileExtension = Path.GetExtension(FileName);
                    
                    FileName = (!string.IsNullOrWhiteSpace(FileName)) ? FileName : Convert.ToString(Guid.NewGuid());
                    FilePath = Path.Combine(Configuration["AppSettings:ContentDirectory"], FileName);

                    if (!file.IsImage())
                    {
                        var asset = new Asset()
                        {
                            FileName = this.FileName,
                            ContentType = file.ContentType.ToLower(),
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = UserID
                        };

                        using (var stream = new FileStream(FilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                            await stream.FlushAsync();

                            var assetID = await AssetRepository.Create(asset, UserID);

                            var assetResponse = new AssetUploadResponse()
                            {
                                ID = assetID,
                                FileName = this.FileName,
                                Size = file.Length,
                                Uri = GetAssetUri(this.FileName)
                            };

                            response.Add(assetResponse);
                        }
                    }
                    else
                    {
                        using (var stream = new MemoryStream())
                        {
                            await file.CopyToAsync(stream);

                            if (stream.Position == stream.Length)
                            {
                                stream.Position = stream.Seek(0, SeekOrigin.Begin);
                            }

                            try
                            {
                                Image<Rgba32> image = Image.Load(stream);

                                if (image != null)
                                {
                                    ImageHeight = image.Height;
                                    ImageWidth = image.Width;
                                }
                            }
                            catch (NullReferenceException)
                            {
                                return null;
                            }
                        }

                        var asset = new ImageAsset()
                        {
                            FileName = this.FileName,
                            ContentType = file.ContentType.ToLower(),
                            Height = ImageHeight,
                            Width = ImageWidth,
                            DateCreated = DateTime.UtcNow,
                            CreatedBy = UserID
                        };

                        using (var stream = new FileStream(FilePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                            await stream.FlushAsync();
                            var imageID = await ImageRepository.Create(asset, UserID);

                            var assetResponse = new AssetUploadResponse()
                            {
                                ID = imageID,
                                FileName = this.FileName,
                                Size = file.Length,
                                Uri = GetAssetUri(this.FileName)
                            };

                            response.Add(assetResponse);
                        }
                    }
                }
            }

            return Created(response[0].Uri, response);
        }

        protected string GetAssetUri(string fileName)
        {
            var path = Configuration["AppSettings:ContentWebPath"];

            if (!path.EndsWith(@"/"))
            {
                path = path + "/";
            }

            return path + fileName;
        }
    }

    public class AssetUploadResponse
    {
        public int ID { get; set; }

        public string FileName { get; set; }

        public long Size { get; set; }

        public string Uri { get; set; }
    }
}