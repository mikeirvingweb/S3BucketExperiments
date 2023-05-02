using Amazon.S3.Model;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3BucketExperiments
{
    internal class S3
    {
        public static async Task<List<string>> ListingObjectsAsync(IAmazonS3 client, string bucketName, string folderName, string prefix, string[]? fileTypes)
        {
            var list = new List<string>();

            try
            {
                ListObjectsV2Request request = new()
                {
                    BucketName = bucketName,
                    MaxKeys = 5,
                    Prefix = (String.IsNullOrEmpty(folderName) ? "" : folderName + "/") + prefix
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
}
