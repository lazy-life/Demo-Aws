using Amazon.CloudWatchLogs;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSPhotosAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Text.Json;

namespace AWSPhotosAPI.Controllers
{
    public class BaseController : ControllerBase
    {
        protected readonly IAmazonS3 _s3Client;
        protected readonly IAmazonCloudWatchLogs _loggerClient;
        protected readonly IAmazonSQS _sqsClient;
        public BaseController(IAmazonS3 s3Client, IAmazonCloudWatchLogs loggerClient, IAmazonSQS sqsClient)
        {
            _s3Client = s3Client;
            _loggerClient = loggerClient;
            _sqsClient = sqsClient;
        }

        /// <summary>
        /// Shows how to create a new Amazon S3 bucket.
        /// </summary>
        /// <param name="client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The name of the bucket to create.</param>
        /// <returns>A boolean value representing the success or failure of
        /// the bucket creation process.</returns>
        private async Task<bool> CreateBucketAsync(string bucketName)
        {
            try
            {
                var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
                if (bucketExists)
                {
                    return true;
                }

                var response = await _s3Client.PutBucketAsync(bucketName);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                //log
                return false;
            }
        }

        protected async Task<string> UploadFileAsync(IFormFile file, string bucketName, string? prefix)
        {
            try
            {
                var bucketExists = await CreateBucketAsync(bucketName);
                if (!bucketExists)
                {
                    return string.Empty;
                }
                var request = new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = string.IsNullOrEmpty(prefix) ? file.FileName : $"{prefix?.TrimEnd('/')}/{file.FileName}",
                    InputStream = file.OpenReadStream()
                };
                request.Metadata.Add("Content-Type", file.ContentType);
                var response = await _s3Client.PutObjectAsync(request);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    return file.FileName;
                }
            }
            catch (AmazonS3Exception ex)
            {

                return string.Empty;
            }
            return string.Empty;
        }

        protected async Task<string> SendMessage(Photo item, string userName, string msgGroupId)
        {
            Dictionary<string, MessageAttributeValue> messageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "Time",   new MessageAttributeValue { DataType = "String", StringValue = DateTime.Now.ToString() } },
                { "Author",  new MessageAttributeValue { DataType = "String", StringValue = userName } },
                { "Type",  new MessageAttributeValue { DataType = "String", StringValue = msgGroupId } },
            };

            var sendMessageRequest = new SendMessageRequest
            {
                MessageAttributes = messageAttributes,
                MessageBody = JsonSerializer.Serialize(item),
                QueueUrl = Config.queueUrl,
                MessageGroupId = msgGroupId,
                MessageDeduplicationId = Guid.NewGuid().ToString()
            };

            var responseSendMessage = await _sqsClient.SendMessageAsync(sendMessageRequest);
            return responseSendMessage.ToString();
        }

        protected async Task<List<S3ObjectDto>> GetAllFilesAsync(string prefix = null)
        {
            try
            {
                var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, Config.bucketName);
                if (!bucketExists)
                {
                    return new List<S3ObjectDto>();
                }
                var request = new ListObjectsV2Request()
                {
                    BucketName = Config.bucketName,
                    Prefix = prefix
                };
                var result = await _s3Client.ListObjectsV2Async(request);

                var s3Objects = result.S3Objects.Select(s =>
                {
                    var urlRequest = new GetPreSignedUrlRequest()
                    {
                        BucketName = Config.bucketName,
                        Key = s.Key,
                        Expires = DateTime.UtcNow.AddDays(1)
                    };
                    return new S3ObjectDto()
                    {
                        Name = s.Key.ToString(),
                        PresignedUrl = _s3Client.GetPreSignedURL(urlRequest),
                    };
                });

                return new List<S3ObjectDto>(s3Objects);
            }
            catch (Exception)
            {
                return new List<S3ObjectDto>();
            }
        }

    }
}
