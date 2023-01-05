# S3 Bucket Experiments in C#
📹 C# Functionality for performing various S3 Bucket Functions and downloading their recording files.

Created alongside the [Camera Experiments](https://github.com/mikeirvingweb/CameraExperiments) repo.
 
## How to use

✏️ Modify *Program.cs* to suit your setup.

`bucketName` - your bucket name.  
`uploadFolder` - a folder within your bucket.  
`localFilePath` - where your local fiels reside.

🎯 Call any of the contained functions:  
`Files.UploadAllFilesInFolder` - Upload Files in a Folder.  
`Files.UploadAllFilesInAllFolders` - Traverses sub folders.   
`Files.CreateFileListAsJSON` - Create a JSON File List.   
`S3.UploadObjectFromFileAsync` - Upload from a File.  
`S3.UploadObjectFromStringAsync` - Create a file from a string.  
`S3.ListingObjectsAsync` - List Objects in Bucket or Folder.  
`S3.CopyObjectAsync` - Copy an Object.  
`S3.DeleteObjectAsync` - Delete an Object.  
`S3.MoveObjectAsync` - Copy then Delete an Object.  

### Contributions

🍴 Feel free to Fork / Branch / Modify, raise any Pull Requests for changes.

#### Further reading  

🦔 Built as part of [.NET, IoT and Hedgehogs!](https://www.mike-irving.co.uk/web-design-blog/?blogid=122)
