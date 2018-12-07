using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
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
        protected readonly IAssetStorageService AssetStorageService;

        public AssetController(
            IConfiguration configuration,
            IAssetRepository assetRepository,
            IImageAssetRepository imageRepository,
            IAssetStorageService assetStorageService)
        {
            Configuration = configuration;
            AssetRepository = assetRepository;
            ImageRepository = imageRepository;
            AssetStorageService = assetStorageService;
        }

        protected string CloudContainerName { get; set; }
        protected string FileName { get; set; }
        protected string FileExtension { get; set; }
        protected string FilePath { get; set; }
        protected long TotalSize { get; set; }

        protected ClaimsIdentity ClaimsIdentity => User.Identity as ClaimsIdentity;
        protected int UserID => int.Parse(ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        [Route("image/{fileName}")]
        [HttpGet]
        public IActionResult DownloadImage(string fileName)
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
        public async Task<IActionResult> UploadAsync(List<IFormFile> files, string container = "")
        {
            var response = new List<AssetUploadResponse>();
            TotalSize = files.Sum(f => f.Length);

            if (container.HasValue())
            {
                CloudContainerName = container;
            }

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    continue;
                }

                var contentDirectory = Configuration["AppSettings:ContentDirectory"];
                var contentWebPath = Configuration["AppSettings:ContentWebPath"];
                FileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                FileName = (!string.IsNullOrWhiteSpace(FileName)) ? FileName : Convert.ToString(Guid.NewGuid());
                FilePath = Path.Combine(contentDirectory, FileName);

                var uploadRequest = new AssetUploadRequest()
                {
                    ContainerName = CloudContainerName,
                    File = file,
                    FilePath = this.FilePath
                };

                if (file.TryGetImageAsset(out ImageAsset image))
                {
                    // save the file to storage
                    var saveImageAsset = await AssetStorageService.SaveAssetAsync(uploadRequest);

                    if (saveImageAsset != AssetUploadResult.Success)
                    {
                        continue;
                    }

                    // save image metadata to data store
                    image.FileName = this.FileName;
                    image.ContentType = file.ContentType.ToLower();
                    image.DateCreated = DateTime.UtcNow;
                    image.CreatedBy = UserID;

                    var imageID = await ImageRepository.CreateAsync(image, UserID);

                    // return upload report to client
                    var imageAssetResponse = new AssetUploadResponse()
                    {
                        ID = imageID,
                        FileName = this.FileName,
                        Size = file.Length,
                        Uri = file.GetAssetUri(contentWebPath, this.FileName)
                    };

                    response.Add(imageAssetResponse);
                }
                else
                {
                    // save the file to storage
                    var saveAsset = await AssetStorageService.SaveAssetAsync(uploadRequest);

                    if (saveAsset != AssetUploadResult.Success)
                    {
                        continue;
                    }
                    
                    // save file metadata to data store
                    var asset = new Asset()
                    {
                        FileName = this.FileName,
                        ContentType = file.ContentType.ToLower(),
                        DateCreated = DateTime.UtcNow,
                        CreatedBy = UserID
                    };

                    var assetID = await AssetRepository.CreateAsync(asset, UserID);
                    
                    // return upload report to client
                    var assetResponse = new AssetUploadResponse()
                    {
                        ID = assetID,
                        FileName = this.FileName,
                        Size = file.Length,
                        Uri = file.GetAssetUri(contentWebPath, this.FileName)
                    };

                    response.Add(assetResponse);
                }
            }

            var assetUri = HttpUtility.UrlEncode(response[0].Uri, System.Text.Encoding.UTF8);
            return Created(assetUri, response);
        }
    }
}