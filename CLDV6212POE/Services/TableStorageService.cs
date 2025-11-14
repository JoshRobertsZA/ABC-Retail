using Azure;
using Azure.Data.Tables;
using CLDV6212POE.Models;
using System.Reflection;


namespace CLDV6212POE.Services;

public class TableStorageService<T> where T : class, new()
{
    private readonly TableClient _tableClient;

    public TableStorageService(string? connectionString, string tableName)
    {
        _tableClient = new TableClient(connectionString, tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<List<T>> GetAllEntitiesAsync()
    {
        var list = new List<T>();
        await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
        {
            list.Add(MapFromTableEntity(entity));
        }
        return list;
    }

    public async Task<T?> GetEntityAsync(string partitionKey, string rowKey)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
            return MapFromTableEntity(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> StoreEntityAsync(T entity, string? partitionKey = null, string? rowKey = null)
    {
        try
        {
            var tableEntity = MapToTableEntity(entity, partitionKey, rowKey);
            await _tableClient.AddEntityAsync(tableEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task UpdateEntityAsync(T entity, string partitionKey, string rowKey)
    {
        var tableEntity = MapToTableEntity(entity, partitionKey, rowKey);
        await _tableClient.UpdateEntityAsync(tableEntity, ETag.All, TableUpdateMode.Replace);
    }

    public async Task DeleteEntityAsync(string partitionKey, string rowKey)
    {
        await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    private T MapFromTableEntity(TableEntity te)
    {
        var result = new T();
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            if (te.TryGetValue(prop.Name, out var value) && value != null)
            {
                if (TryConvert(value, prop.PropertyType, out var converted))
                {
                    prop.SetValue(result, converted);
                }
            }
        }
        return result;
    }

    private TableEntity MapToTableEntity(T entity, string partitionKey, string rowKey)
    {
        var tableEntity = new TableEntity(partitionKey, rowKey);

        foreach (var property in typeof(T).GetProperties())
        {
            if (property.Name == nameof(TableEntity.PartitionKey) ||
                property.Name == nameof(TableEntity.RowKey) ||
                property.Name == nameof(TableEntity.Timestamp) ||
                property.Name == nameof(TableEntity.ETag))
                continue; // ✅ Skip system fields

            var value = property.GetValue(entity);
            if (value != null)
                tableEntity[property.Name] = value;
        }

        return tableEntity;
    }

    private static bool TryConvert(object value, Type targetType, out object? converted)
    {
        var isNullable = targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>);
        var underlying = isNullable ? Nullable.GetUnderlyingType(targetType)! : targetType;

        try
        {
            if (underlying.IsAssignableFrom(value.GetType()))
            {
                converted = value;
                return true;
            }

            converted = Convert.ChangeType(value, underlying);
            return true;
        }
        catch
        {
            converted = null;
            return false;
        }
    }

    public async Task<Customer?> GetCustomerByUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Invalid userId passed to GetCustomerByUserAsync.");

        var safeKey = userId.Replace("/", "_").Replace("\\", "_").Replace("#", "_")
            .Replace("?", "_").Replace("[", "_").Replace("]", "_");

        var query = $"PartitionKey eq '{safeKey}'";

        await foreach (var entity in _tableClient.QueryAsync<Customer>(query))
        {
            return entity;
        }

        return null;
    }

    public TableClient GetTableClient()
    {
        return _tableClient;
    }
}
