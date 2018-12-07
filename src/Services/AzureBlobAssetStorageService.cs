using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Schematic.Core.Mvc
{
    public class AzureBlobAssetStorageService : IAssetStorageService
    {
        public Task<byte[]> GetAssetAsync(AssetDownloadRequest asset)
        {
            throw new System.NotImplementedException();
        }

        public async Task<AssetUploadResult> SaveAssetAsync(AssetUploadRequest asset)
        {
            return AssetUploadResult.Success;
        }
        /* 
        protected readonly string StorageAccount;

        public AzureBlobAssetStorageService(ISchematicSettings settings)
        {
            StorageAccount = settings.ApplicationDescription;
        }

        public async Task<bool> SaveAssetAsync(IFormFile file, string containerName)
        {
            var cloudCachedStorageAccount = CloudStorageAccount.Parse(StorageAccount);
            var cloudBlobClient = cloudCachedStorageAccount.CreateCloudBlobClient();
            var container = cloudBlobClient.GetContainerReference(containerName);

            if (!await container.ExistsAsync())
            {
                throw new StorageException();
            }

            var blockBlob = container.GetBlockBlobReference(resourceId);

            if (blockBlob.Exists())
            {
                using (var memoryStream = MemoryStreamPool.Shared.GetStream())
                {
                    await blockBlob.DownloadToStreamAsync(memoryStream).ConfigureAwait(false);
                    return memoryStream.ToArray();
                }
            }
        }*/
    }
}