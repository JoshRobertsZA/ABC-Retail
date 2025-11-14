using Azure.Storage.Queues;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CLDV6212POE.Models;
using static System.Net.WebRequestMethods;
using JsonSerializer = System.Text.Json.JsonSerializer;
using QueueMessageViewModel = Function.Models.QueueMessageViewModel;

namespace CLDV6212POE.Services
{
    public class FunctionConnector
    {
        private readonly string _connectionString;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FunctionConnector>? _logger;

        // Azure Table endpoints
        private readonly string _storeCustomerUrl = "https://st10265742.azurewebsites.net/api/StoreCustomer";
        private readonly string _storeProductUrl = "https://st10265742.azurewebsites.net/api/StoreProduct";

        // Azure Blob endpoint
        private readonly string _writeToBlobUrl = "https://st10265742.azurewebsites.net/api/WriteToBlob";

        // Azure Queue endpoints
        private readonly string _sendMessageUrl = "https://st10265742.azurewebsites.net/api/customer/send";
        private readonly string _receiveMessageUrl = "https://st10265742.azurewebsites.net/api/ReceiveQueueMessages";

        // Azure File Share endpoint
        private readonly string _writeToFileUrl = "https://st10265742.azurewebsites.net/api/WriteToFile";

        public FunctionConnector(HttpClient httpClient, ILogger<FunctionConnector>? logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _connectionString = "DefaultEndpointsProtocol=https;AccountName=st10265742cldv6212;AccountKey=cDSpUNo8JoOegnZsQLeTToZKIM+Fu2HJAT4KfdzRjLQjdRlGWFQ+vhTT32Sdn3yk1gs/OtiYBZRp+AStijNgMw==;EndpointSuffix=core.windows.net";
        }

        #region Azure Table

        // Save a customer entity via the Azure Function.
        public async Task<bool> StoreCustomerAsync(object customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_storeCustomerUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Storing Customer failed: {StatusCode} - {Content}", response.StatusCode,
                        errorContent);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception in StoreCustomerAsync");
                return false;
            }
        }

        // Save a product entity via the Azure Function.
        public async Task<bool> StoreProductAsync(object product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_storeProductUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("Storing Product failed: {StatusCode} - {Content}", response.StatusCode,
                        errorContent);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception in StoreProductAsync");
                return false;
            }
        }


        #endregion

        #region Azure Blob

        // Upload image directly to blob storage.
        public async Task<string?> UploadToBlobAsync(IFormFile file)
        {
            try
            {
                _logger?.LogInformation("Uploading file {FileName} ({Size} bytes) to {Url}", file.FileName, file.Length,
                    _writeToBlobUrl);
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                var response = await _httpClient.PostAsync(_writeToBlobUrl, streamContent);
                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return null;
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("url", out var u) ? u.GetString() : null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error uploading blob");
                return null;
            }
        }

        #endregion

        #region Azure Queue

        // Sends a message to the Azure queue
        public async Task SendMessageAsync<T>(T message)
        {
            var queueClient = new QueueClient(
                _connectionString,
                "order-queue",
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
            );

            await queueClient.CreateIfNotExistsAsync();

            // Serialize message to JSON
            string json = JsonSerializer.Serialize(message);

            await queueClient.SendMessageAsync(json);

            _logger.LogInformation("Sending message to queue: {Json}", json);
        }


        // Sends message to customer-queue
        public async Task SendCustomerMessageAsync(Customer customer)
        {
            // Create a QueueClient for the "customer-queue"
            var queueClient = new QueueClient(_connectionString, "customer-queue");

            // Ensure the queue exists
            await queueClient.CreateIfNotExistsAsync();

            // Serialize the customer object to JSON
            string messageJson = JsonSerializer.Serialize(customer);

            // Send the message to the queue
            await queueClient.SendMessageAsync(messageJson);
        }


        // Receive (10) messages from the Azure queue
        public async Task<List<QueueMessageViewModel>> ReceiveMessagesAsync()
        {
            var queueClient = new QueueClient(_connectionString, "customer-queue");
            await queueClient.CreateIfNotExistsAsync();

            // Peek at up to 10 messages (does not remove them)
            var peekedMessages = await queueClient.PeekMessagesAsync(10);

            var messages = peekedMessages.Value
                .Select(m => new QueueMessageViewModel
                {
                    MessageId = m.MessageId,
                    MessageText = m.Body.ToString()
                })
                .ToList();

            return messages;
        }


        #endregion

        #region Azure File Share

        // Upload a file to Azure Files share.
        public async Task<string?> UploadFileAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return null;
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                content.Add(streamContent, "file", file.FileName ?? "contracts");
                var response = await _httpClient.PostAsync(_writeToFileUrl, content);
                if (!response.IsSuccessStatusCode) return null;
                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("fileName", out var fn)) return fn.GetString();
                if (doc.RootElement.TryGetProperty("files", out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
                    return arr[0].GetString();
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception uploading file to Azure Files");
                return null;
            }
        }

        #endregion
    }
}