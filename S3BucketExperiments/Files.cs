using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace S3BucketExperiments
{
    internal class Files
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

                    var manualFolderSubFolderSafe = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? manualFolderSubFolder.Replace("\\", "/") : manualFolderSubFolder;

                    var subFolderName = manualFolderSubFolderSafe.Split(folderDelimeter).Last().ToLower();

                    if (subFolderName == "approved")
                    {
                        foreach (string file in Directory.EnumerateFiles(manualFolderSubFolderSafe))
                        {
                            if (!(file.Split(folderDelimeter).Last().Contains("_" + manualFolder.Split(folderDelimeter).Last().Replace(" ", "-") + "_")))
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
                            if (!(file.Split(folderDelimeter).Last().Contains("_" + manualFolder.Split(folderDelimeter).Last().Replace(" ", "-") + "_")))
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

        public static async Task CreateFileListAsJSON(IAmazonS3 client, string bucketName, string folderName, string prefix, string fileName, string s3URL, string[]? fileTypes)
        {
            var list = (await S3.ListingObjectsAsync(client, bucketName, folderName, prefix, fileTypes)).OrderByDescending(i => i);

            var fileInfoList = new List<JSONFileInfo>();

            foreach (var listItem in list)
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
}
