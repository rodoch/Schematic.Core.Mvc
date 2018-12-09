using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Schematic.Core.Mvc
{
    public class AzureBlobAssetStorageService : IAssetStorageService
    {
        protected IOptionsMonitor<SchematicSettings> Settings;

        public AzureBlobAssetStorageService(IOptionsMonitor<SchematicSettings> settings)
        {
            Settings = settings;
        }

        protected async Task<CloudBlobContainer> GetContainerAsync(string containerName)
        {
            var storageConnectionString = Settings.CurrentValue.CloudStorage.AzureStorage.StorageAccount;

            if (CloudStorageAccount.TryParse(storageConnectionString, out CloudStorageAccount storageAccount))
            {
                var cloudBlobClient = storageAccount.CreateCloudBlobClient();
                var container = cloudBlobClient.GetContainerReference(containerName);

                if (!await container.ExistsAsync())
                {
                    throw new StorageException();
                }

                return container;
            }
            else
            {
                return null;
            }
        }

        public async Task<byte[]> GetAssetAsync(AssetDownloadRequest asset)
        {
            var container = await GetContainerAsync(asset.ContainerName);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(asset.FileName);

            if (!await blockBlob.ExistsAsync())
            {
                return null;
            }
            
            using (var blobStream = blockBlob.OpenRead())
            {
                blobStream.Seek(0, SeekOrigin.Begin);
                byte[] output = new byte[blobStream.Length];
                await blobStream.ReadAsync(output, 0, output.Length);
                return output;
            }
        }

        public async Task<AssetUploadResult> SaveAssetAsync(AssetUploadRequest asset)
        {
            var container = await GetContainerAsync(asset.ContainerName);

            throw new System.NotImplementedException();
        }
    }
}