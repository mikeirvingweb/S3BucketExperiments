using Amazon;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.S3.Model;
using S3BucketExperiments;
using System.Runtime.InteropServices;
using System.Text.Json;

string bucketName = "macclesfield-hedgehogs", uploadFolder = "upload";

string s3URL = "https://" + bucketName + ".s3.amazonaws.com/";

var fileTypes = new String[] { "mp4", "avi", "txt" };

string localFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)? @"/cameras" : @"/cameras";

IAmazonS3 s3Client = new AmazonS3Client(RegionEndpoint.EUWest2);

//await Files.UploadAllFilesInFolder(s3Client, bucketName, uploadFolder, localFilePath, fileTypes, true);

await Files.UploadAllFilesInAllFolders(s3Client, bucketName, uploadFolder, localFilePath, fileTypes, true);

//await S3.UploadObjectFromFileAsync(s3Client, bucketName, uploadFolder, "test-upload.txt", localFilePath + "\test-upload.txt", "text/plain");

//var list = await S3.ListingObjectsAsync(s3Client, bucketName, "", fileTypes);


//await S3.CopyObjectAsync(s3Client, uploadFolder + "/" + "2022-10-02-09-03-22_Ceyomur-CY95_VD-00001.MP4", "2022-10-02-09-03-22_Ceyomur-CY95_VD-00001.MP4", bucketName, bucketName);
//await S3.DeleteObjectAsync(s3Client, uploadFolder + "/" + "2022-10-02-09-03-22_Ceyomur-CY95_VD-00001.MP4", bucketName);

//await S3.MoveObjectAsync(s3Client, uploadFolder + "/" + "/2022-10-02-09-03-22_Ceyomur-CY95_VD-00001.MP4", "2022-10-02-09-03-22_Ceyomur-CY95_VD-00001.MP4", bucketName, bucketName);


// now doing this by executing remote lambda,
//await Files.CreateFileListAsJSON(s3Client, bucketName, "", "20", "videos.json", s3URL, new String[] { "mp4" });
//await Files.CreateFileListAsJSON(s3Client, bucketName, uploadFolder, "", "upload.json", s3URL, new String[] { "mp4" });

AmazonLambdaClient lambdaClient = new AmazonLambdaClient(RegionEndpoint.EUWest2);
lambdaClient.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest() { FunctionName = "LambdaS3CreateVideoThumbnails", Payload = String.Empty });

Thread.Sleep(5000);

Console.WriteLine("done");