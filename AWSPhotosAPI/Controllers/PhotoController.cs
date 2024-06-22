using Amazon.CloudWatchLogs;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using AWSPhotosAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.Xml;

namespace AWSPhotosAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : BaseController
    {
        public PhotoController(IAmazonS3 s3Client, IAmazonCloudWatchLogs loggerClient, IAmazonSQS sqsClient)
            : base(s3Client, loggerClient, sqsClient)
        {
        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile file, string? prefix, string userName)
        {
            if (file != null)
            {
                DateTime timeUpload = DateTime.UtcNow;
                string result = await UploadFileAsync(file, Config.bucketName, prefix);
                if (!string.IsNullOrEmpty(result))
                {
                    Photo photo = new Photo();
                    photo.BucketName = Config.bucketName;
                    photo.UploadDate = timeUpload;
                    photo.PhotoName = result;
                    photo.UserNameUp = userName;
                    var resultUpload = await SendMessage(photo, userName, "upload");
                    return Ok(resultUpload);
                }
            }
            return BadRequest();
        }

        [HttpGet("GetAllFiles")]
        public async Task<IActionResult> GetAllFiles()
        {
            List<S3ObjectDto> files = await GetAllFilesAsync();
            if (files == null || !files.Any())
            {
                return NotFound(); // Trả về 404 Not Found nếu không có file nào trong bucket
            }

            return Ok(files.Select(f => new S3ObjectDto
            {
                Name = f.Name,
                PresignedUrl = f.PresignedUrl
            }));
        }
    }
}
