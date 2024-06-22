namespace AWSPhotosAPI.Queue
{
    using Amazon.SQS;
    using Amazon.SQS.Model;
    using AWSPhotosAPI.Models;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class SqsBackgroundService : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<SqsBackgroundService> _logger;

        public SqsBackgroundService(IAmazonSQS sqsClient, ILogger<SqsBackgroundService> logger)
        {
            _sqsClient = sqsClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var receiveMessageRequest = new ReceiveMessageRequest
                {
                    QueueUrl = Config.queueUrl,
                    MaxNumberOfMessages = 10, // Số lượng tin nhắn tối đa muốn nhận mỗi lần gọi
                    WaitTimeSeconds = 20, // Thời gian chờ (long polling) để nhận tin nhắn nếu không có tin nhắn ngay lập tức
                    MessageAttributeNames = new List<string> { "All" }
                };

                var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);

                foreach (var message in receiveMessageResponse.Messages)
                {
                    // Xử lý thông điệp ở đây
                    if (await HandleMessage(message))
                    {
                        // Xóa thông điệp khỏi hàng đợi sau khi xử lý xong
                        var deleteMessageRequest = new DeleteMessageRequest
                        {
                            QueueUrl = Config.queueUrl,
                            ReceiptHandle = message.ReceiptHandle
                        };
                        await _sqsClient.DeleteMessageAsync(deleteMessageRequest, stoppingToken);
                    }
                }

                // Chờ 30 giây trước khi kiểm tra lại
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task<bool> HandleMessage(Message message)
        {
            try
            {
                // Thêm logic xử lý thông điệp ở đây
                var item = JsonSerializer.Deserialize<Photo>(message.Body);
                if (message.MessageAttributes.ContainsKey("Type") && message.MessageAttributes["Type"].StringValue.Equals("upload"))
                {
                    using (AWSPhotoContext context = new AWSPhotoContext())
                    {
                        context.Photos.Add(item);
                        context.SaveChanges();
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
