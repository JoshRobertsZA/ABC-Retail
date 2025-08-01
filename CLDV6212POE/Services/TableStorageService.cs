using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE.Services
{
    public class TableStorageService<T> where T : class, ITableEntity, new()
    {
        private readonly TableClient _tableClient;

        public TableStorageService(string connectionString, string tableName)
        {
            _tableClient = new TableClient(connectionString, tableName);
            _tableClient.CreateIfNotExists();
        }


        public async Task AddEntityAsync(T entity)
        {
            await _tableClient.AddEntityAsync(entity);
        }


        public async Task<List<T>> GetAllEntitiesAsync()
        {
            var entities = new List<T>();

            await foreach (var entity in _tableClient.QueryAsync<T>())
            {
                entities.Add(entity);
            }

            return entities;
        }


        public async Task<T?> GetEntityAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return default;
            }
        }


        public async Task UpdateEntityAsync(T entity)
        {
            await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
        }


        public async Task DeleteEntityAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}