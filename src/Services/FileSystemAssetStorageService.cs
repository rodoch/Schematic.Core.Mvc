using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Schematic.Core;

namespace Schematic.Core.Mvc
{
    public class FileSystemAssetStorageService : IAssetStorageService
    {
        //public async Task<FileStream> GetAssetAsync()
        //{
        //    using (var reader = new StreamReader(await selectedFile.OpenStreamForReadAsync()))
        //    {
        //        
        //    }
        //}

        public async Task<AssetUploadResult> SaveAssetAsync(AssetUploadRequest asset)
        {
            using (var stream = new FileStream(asset.FilePath, FileMode.Create))
            {
                try
                {
                    await asset.File.CopyToAsync(stream);
                    await stream.FlushAsync();
                    return AssetUploadResult.Success;
                }
                catch
                {
                    return AssetUploadResult.Failure;
                }
            }
        }
    }
}