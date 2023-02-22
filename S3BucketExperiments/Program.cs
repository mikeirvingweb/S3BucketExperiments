using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json;

string bucketName = "bucket-name", uploadFolder = "folder-name";

string s3URL = "https://" + bucketName + ".s3.amazonaws.com/";

var fileTypes = new String[] { "mp4", "txt" };

string localFilePath = @"local-file-path";

IAmazonS3 s3Client = new AmazonS3Client(RegionEndpoint.EUWest2);

await Files.UploadAllFilesInAllFolders(s3Client, bucketName, uploadFolder, localFilePath, fileTypes, true);

await Files.CreateFileListAsJSON(s3Client, bucketName, "", "videos.json", s3URL, new String[] { "mp4" });

await Files.CreateFileListAsJSON(s3Client, bucketName, uploadFolder, "upload.json", s3URL, new String[] { "mp4" });

class JSONFileInfo
{
    public string OriginalFileName { get; set; }
    public string Camera { get; set; }
    public string Date { get; set; }
    public string URL { get; set; }
}

class Files
{
    public static string FileNameToContentType(string fileName)
    {
        fileName = fileName.ToLower();

        // add more as neccessary
        if (fileName.EndsWith(".mp4"))
            return "video/mp4";
        if (fileName.EndsWith(".json"))
            return "application/json";
        else
            return "text/plain";
    }

    public static async Task UploadAllFilesInFolder(IAmazonS3 client, string bucketName, string folderName, string folder, string[]? fileTypes, bool delete)
    {
        foreach (string file in Directory.EnumerateFiles(folder))
        {
            Console.WriteLine(file);

            string fileName = Path.GetFileName(file);

            if (fileTypes != null && fileTypes.Length > 0)
            {
                foreach (var fileType in fileTypes)
                {
                    if (fileName.ToLower().EndsWith("." + fileType.ToLower()))
                    {
                        await S3.UploadObjectFromFileAsync(client, bucketName, folderName, fileName, file, FileNameToContentType(fileName));

                        if (delete)
                            File.Delete(file);
                    }
                }
            }
            else
            {
                await S3.UploadObjectFromFileAsync(client, bucketName, folderName, fileName, file, FileNameToContentType(fileName));

                if (delete)
                    File.Delete(file);
            }
            
        }
    }

    public static async Task UploadAllFilesInAllFolders(IAmazonS3 client, string bucketName, string unapprovedFolderName, string folder, string[]? fileTypes, bool delete)
    {
        foreach (string manualFolder in Directory.EnumerateDirectories(folder))
        {
            Console.WriteLine(manualFolder);

            foreach (string manualFolderSubFolder in Directory.EnumerateDirectories(manualFolder))
            {
                var folderDelimeter = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/" : "\\";

                var manualFolderSubFolderSafe = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)? manualFolderSubFolder.Replace("\\", "/") : manualFolderSubFolder;

                var subFolderName = manualFolderSubFolderSafe.Split(folderDelimeter).Last().ToLower();

                if(subFolderName == "approved")
                {
                    foreach (string file in Directory.EnumerateFiles(manualFolderSubFolderSafe))
                    {
                        if(!file.Split(folderDelimeter).Last().StartsWith("202"))
                        {
                            var newFileName = File.GetLastWriteTime(file).ToString("yyyy-MM-dd-HH-mm-ss") + "_" + manualFolder.Split(folderDelimeter).Last().Replace(" ", "-") + "_" + file.Split(folderDelimeter).Last().Replace("_", "-");

                            File.Move(file, manualFolderSubFolderSafe + folderDelimeter + newFileName);
                        }                        
                    }
                    
                    await Files.UploadAllFilesInFolder(client, bucketName, String.Empty, manualFolderSubFolderSafe, fileTypes, true);
                }
                else if (subFolderName == "unapproved")
                {
                    foreach (string file in Directory.EnumerateFiles(manualFolderSubFolderSafe))
                    {
                        if (!file.Split(folderDelimeter).Last().StartsWith("202"))
                        {
                            var newFileName = File.GetLastWriteTime(file).ToString("yyyy-MM-dd-HH-mm-ss") + "_" + manualFolder.Split(folderDelimeter).Last().Replace(" ", "-") + "_" + file.Split(folderDelimeter).Last().Replace("_", "-");

                            File.Move(file, manualFolderSubFolderSafe + folderDelimeter + newFileName);
                        }
                    }

                    await Files.UploadAllFilesInFolder(client, bucketName, unapprovedFolderName, manualFolderSubFolderSafe, fileTypes, true);
                }
                else if (subFolderName == "automated")
                {
                    await Files.UploadAllFilesInFolder(client, bucketName, unapprovedFolderName, manualFolderSubFolderSafe, fileTypes, true);
                }
            }
        }
    }

    public static async Task CreateFileListAsJSON(IAmazonS3 client, string bucketName, string folderName, string fileName, string s3URL, string[]? fileTypes)
    {
        var list = (await S3.ListingObjectsAsync(client, bucketName, folderName, fileTypes)).OrderByDescending(i => i);

        var fileInfoList = new List<JSONFileInfo>();
        
        foreach(var listItem in list)
        {
            if (!(String.IsNullOrEmpty(folderName)) || listItem.Contains("/") == false)
            {
                string[] fileSplit;

                if (!(String.IsNullOrEmpty(folderName)))
                {
                    fileSplit = listItem.Split("/")[1].Split("_");
                }
                else
                {
                    fileSplit = listItem.Split("_");
                }

                if (fileSplit.Length == 3)
                {
                    var fileInfo = new JSONFileInfo() { OriginalFileName = fileSplit[2], Camera = fileSplit[1], Date = fileSplit[0], URL = s3URL + listItem };

                    fileInfoList.Add(fileInfo);
                }
            }
        }

        await S3.UploadObjectFromStringAsync(client, bucketName, folderName, fileName, JsonSerializer.Serialize(fileInfoList), FileNameToContentType("videos.json"));
    }
}

class S3
{
    public static async Task<List<string>> ListingObjectsAsync(IAmazonS3 client, string bucketName, string folderName, string[]? fileTypes)
    {
        var list = new List<string>();

        try
        {
            ListObjectsV2Request request = new()
            {
                BucketName = bucketName,
                MaxKeys = 5,
                Prefix = (String.IsNullOrEmpty(folderName) ? "" : folderName + "/")
            };

            var response = new ListObjectsV2Response();

            do
            {
                response = await client.ListObjectsV2Async(request);

                foreach (var obj in response.S3Objects)
                {
                    Console.WriteLine($"{obj.Key,-35}{obj.LastModified.ToShortDateString(),10}{obj.Size,10}");

                    if (fileTypes != null && fileTypes.Length > 0)
                    {
                        foreach (var fileType in fileTypes)
                        {
                            if (obj.Key.ToLower().EndsWith("." + fileType.ToLower()))
                                list.Add(obj.Key);
                        }
                    }
                    else
                    {
                        list.Add(obj.Key);
                    }
                }

                // If the response is truncated, set the request ContinuationToken
                // from the NextContinuationToken property of the response.
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"Error encountered on server. Message:'{ex.Message}' getting list of objects.");
        }

        return list;
    }

    public static async Task UploadObjectFromFileAsync(
            IAmazonS3 client,
            string bucketName,
            string folderName,
            string objectName,
            string filePath,
            string contentType)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = (String.IsNullOrEmpty(folderName) ? "" : folderName + "/") + objectName,
                FilePath = filePath,
                ContentType = contentType
            };

            PutObjectResponse response = await client.PutObjectAsync(putRequest);
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

    public static async Task UploadObjectFromStringAsync(
            IAmazonS3 client,
            string bucketName,
            string folderName,
            string objectName,
            string content,
            string contentType)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = (String.IsNullOrEmpty(folderName) ? "" : folderName + "/") + objectName,
                ContentType = contentType,
                ContentBody = content
            };

            PutObjectResponse response = await client.PutObjectAsync(putRequest);
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

    public static async Task<CopyObjectResponse> CopyObjectAsync(
            IAmazonS3 client,
            string sourceKey,
            string destinationKey,
            string sourceBucketName,
            string destinationBucketName)
    {
        var response = new CopyObjectResponse();
        try
        {
            var request = new CopyObjectRequest
            {
                SourceBucket = sourceBucketName,
                SourceKey = sourceKey,
                DestinationBucket = destinationBucketName,
                DestinationKey = destinationKey
            };
            response = await client.CopyObjectAsync(request);
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"Error copying object: '{ex.Message}'");
        }

        return response;
    }

    public static async Task<DeleteObjectResponse> DeleteObjectAsync(
            IAmazonS3 client,
            string key,
            string bucketName)
    {
        var response = new DeleteObjectResponse();
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };
            response = await client.DeleteObjectAsync(request);
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"Error deleting object: '{ex.Message}'");
        }

        return response;
    }

    public static async Task MoveObjectAsync(
            IAmazonS3 client,
            string sourceKey,
            string destinationKey,
            string sourceBucketName,
            string destinationBucketName)
    {
        await CopyObjectAsync(client, sourceKey, destinationKey, sourceBucketName, destinationBucketName);
        await DeleteObjectAsync(client, sourceKey, sourceBucketName);
    }
}