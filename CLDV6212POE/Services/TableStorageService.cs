using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE.Services;

public class TableStorageService<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _tableClient;

    public TableStorageService(string connectionString, string tableName)
    {
        _tableClient = new TableClient(connectionString, tableName);
        _tableClient.CreateIfNotExists();
    }


    /// <summary>
    /// Adds a new entity (row) to the table.
    /// </summary>
    public async Task AddEntityAsync(T entity)
    {
        await _tableClient.AddEntityAsync(entity);
    }


    /// <summary>
    /// Returns all entities in the table.
    /// </summary>
    public async Task<List<T>> GetAllEntitiesAsync()
    {
        var entities = new List<T>();

        await foreach (var entity in _tableClient.QueryAsync<T>()) entities.Add(entity);

        return entities;
    }


    /// <summary>
    /// Retrieves an entity by partition key and row key.
    /// </summary>
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


    /// <summary>
    /// Updates an existing entity in the table.
    /// </summary>
    public async Task UpdateEntityAsync(T entity)
    {
        await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
    }


    /// <summary>
    /// Deletes an entity from the table.
    /// </summary>
    public async Task DeleteEntityAsync(string partitionKey, string rowKey)
    {
        await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }
}