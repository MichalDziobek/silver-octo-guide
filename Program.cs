using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace silver_octo_guide
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            await ProcessAsync();
        }

        private static async Task ProcessAsync()
        {
            CloudBlobContainer cloudBlobContainer = null;
            var fileNames = new List<string>();

            string storageConnectionString = Console.ReadLine();

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out CloudStorageAccount storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    cloudBlobContainer = cloudBlobClient.GetContainerReference("misows" + Guid.NewGuid().ToString());
                    await cloudBlobContainer.CreateAsync();
                    Console.WriteLine("Created container '{0}'", cloudBlobContainer.Name);
                    Console.WriteLine();

                    // Set the permissions so the blobs are public. 
                    BlobContainerPermissions permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    };
                    await cloudBlobContainer.SetPermissionsAsync(permissions);

                    var r = new Random();
                    // Create a file in your local MyDocuments folder to upload to a blob.

                    for (int i = 0; i < 10; i++)
                    {
                        fileNames.Add("MISOWS_" + Guid.NewGuid().ToString() + ".txt");
                        var content = new byte[1024 * 1024 * 10];
                        r.NextBytes(content);
                        File.WriteAllBytes(fileNames[i], content);
                    }



                    // Get a reference to the blob address, then upload the file to the blob.
                    // Use the value of localFileName for the blob name.
                    var beforeUpload = DateTime.Now;
                    foreach (var filename in fileNames)
                    {
                        CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(filename);
                        await cloudBlockBlob.UploadFromFileAsync(filename); 
                    }
                    var afterUpload = DateTime.Now;
                    var uploadDuration = afterUpload - beforeUpload;
                    Console.WriteLine($"Upload duration = {uploadDuration.TotalMilliseconds} ms");

                    // List the blobs in the container.
                    Console.WriteLine("Listing blobs in container.");
                    BlobContinuationToken blobContinuationToken = null;
                    do
                    {
                        var results = await cloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                        // Get the value of the continuation token returned by the listing call.
                        blobContinuationToken = results.ContinuationToken;
                        foreach (IListBlobItem item in results.Results)
                        {
                            Console.WriteLine(item.Uri);
                        }
                    } while (blobContinuationToken != null); // Loop while the continuation token is not null.
                    Console.WriteLine();

                    // Download the blob to a local file, using the reference created earlier. 
                    // Append the string "_DOWNLOADED" before the .txt extension so that you can see both files in MyDocuments.

                    var beforeDownload = DateTime.Now;
                    foreach (var filename in fileNames)
                    {
                        CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(filename);
                        await cloudBlockBlob.DownloadToFileAsync(filename.Replace(".txt", "_DOWNLOADED.txt"), FileMode.Create); 
                    }
                    var afterDownload = DateTime.Now;
                    var downloadDuration = afterDownload - beforeDownload;
                    Console.WriteLine($"Download duration = {downloadDuration.TotalMilliseconds} ms");

                }
                catch (StorageException ex)
                {
                    Console.WriteLine("Error returned from the service: {0}", ex.Message);
                }
                finally
                {
                    Console.WriteLine("Press any key to delete the sample files and example container.");
                    Console.ReadLine();
                    // Clean up resources. This includes the container and the two temp files.
                    Console.WriteLine("Deleting the container and any blobs it contains");
                    if (cloudBlobContainer != null)
                    {
                        await cloudBlobContainer.DeleteIfExistsAsync();
                    }
                    Console.WriteLine("Deleting the local source file and local downloaded files");
                    Console.WriteLine();
                    foreach (var filename in fileNames)
                    {
                        File.Delete(filename);
                        File.Delete(filename.Replace(".txt", "_DOWNLOADED.txt")); 
                    }
                }
            }
            else
            {
                Console.WriteLine("Incorrect connection string");
            }
        }
    }
}
