using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Schematic.Core;

namespace Schematic.Core.Mvc
{
    public class FileSystemAssetStorageService : IAssetStorageService
    {
        public async Task<byte[]> GetAssetAsync(AssetDownloadRequest asset)
        {
            if (!File.Exists(asset.FilePath))
            {
                return null;
            }

            using (var reader = new FileStream(asset.FilePath, FileMode.Open, FileAccess.Read))
            {
                reader.Seek(0, SeekOrigin.Begin);
                byte[] output = new byte[reader.Length];
                var stream = await reader.ReadAsync(output, 0, output.Length);
                return output;
            }
        }

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