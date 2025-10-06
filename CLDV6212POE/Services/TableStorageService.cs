using System.Reflection;
using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE.Services;

public class TableStorageService<T> where T : class, ITableEntity, new()
{
    private static readonly string[] ReservedITableEntityProps =
    [
        nameof(ITableEntity.PartitionKey),
        nameof(ITableEntity.RowKey),
        nameof(ITableEntity.Timestamp),
        nameof(ITableEntity.ETag)
    ];

    private readonly TableClient _tableClient;

    public TableStorageService(string connectionString, string tableName)
    {
        _tableClient = new TableClient(connectionString, tableName);
        _tableClient.CreateIfNotExists();
    }

    /// <summary>
    /// Returns all entities in the table.
    /// </summary>
    public async Task<List<T>> GetAllEntitiesAsync()
    {
        try
        {
            var entities = new List<T>();
            await foreach (var entity in _tableClient.QueryAsync<T>())
            {
                entities.Add(entity);
            }
            return entities;
        }
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
        {
            return await GetAllEntitiesWithSafeConversionAsync();
        }
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
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
        {
            var te = await GetTableEntityAsync(partitionKey, rowKey);
            return te == null ? default : ConvertTableEntityToT(te);
        }
    }


    /// <summary>
    /// Retrieves an entity by partition key and row key as TableEntity.
    /// </summary>
    public async Task<TableEntity?> GetTableEntityAsync(string partitionKey, string rowKey)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
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


    /// <summary>
    /// Fallback: query as TableEntity and convert to T with safe conversions.
    /// </summary>
    private async Task<List<T>> GetAllEntitiesWithSafeConversionAsync()
    {
        var list = new List<T>();
        await foreach (var te in _tableClient.QueryAsync<TableEntity>())
        {
            list.Add(ConvertTableEntityToT(te));
        }
        return list;
    }


    private static T ConvertTableEntityToT(TableEntity te)
    {
        var result = new T
        {
            PartitionKey = te.PartitionKey,
            RowKey = te.RowKey,
            Timestamp = te.Timestamp,
            ETag = te.ETag
        };

        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && !ReservedITableEntityProps.Contains(p.Name));

        foreach (var prop in props)
        {
            if (!te.TryGetValue(prop.Name, out var value) || value is null)
                continue;

            if (TryConvert(value, prop.PropertyType, out var converted))
            {
                try
                {
                    prop.SetValue(result, converted);
                }
                catch {}
            }
        }

        return result;
    }


    private static bool TryConvert(object value, Type targetType, out object? converted)
    {
        var isNullable = targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>);
        var underlying = isNullable ? Nullable.GetUnderlyingType(targetType)! : targetType;

        try
        {
            if (underlying == typeof(string))
            {
                converted = value.ToString();
                return true;
            }
            if (underlying == typeof(int))
            {
                if (value is int i) { converted = i; return true; }
                if (value is long l) { converted = checked((int)l); return true; }
                if (value is string s && int.TryParse(s, out var pi)) { converted = pi; return true; }
            }
            if (underlying == typeof(long))
            {
                if (value is long l2) { converted = l2; return true; }
                if (value is int i2) { converted = (long)i2; return true; }
                if (value is string s2 && long.TryParse(s2, out var pl)) { converted = pl; return true; }
            }
            if (underlying == typeof(double))
            {
                if (value is double d) { converted = d; return true; }
                if (value is float f) { converted = (double)f; return true; }
                if (value is string s3 && double.TryParse(s3, out var pd)) { converted = pd; return true; }
            }
            if (underlying == typeof(decimal))
            {
                if (value is decimal de) { converted = de; return true; }
                if (value is double dd) { converted = (decimal)dd; return true; }
                if (value is string s4 && decimal.TryParse(s4, out var pde)) { converted = pde; return true; }
            }
            if (underlying == typeof(bool))
            {
                if (value is bool b) { converted = b; return true; }
                if (value is string s5 && bool.TryParse(s5, out var pb)) { converted = pb; return true; }
                if (value is int ib) { converted = ib != 0; return true; }
            }
            if (underlying == typeof(DateTimeOffset))
            {
                if (value is DateTimeOffset dto) { converted = dto; return true; }
                if (value is DateTime dt) { converted = new DateTimeOffset(dt); return true; }
                if (value is string s6 && DateTimeOffset.TryParse(s6, out var pdt)) { converted = pdt; return true; }
            }
            if (underlying == typeof(DateTime))
            {
                if (value is DateTime dt2) { converted = dt2; return true; }
                if (value is DateTimeOffset dto2) { converted = dto2.UtcDateTime; return true; }
                if (value is string s7 && DateTime.TryParse(s7, out var pdt2)) { converted = pdt2; return true; }
            }
            if (underlying == typeof(Guid))
            {
                if (value is Guid g) { converted = g; return true; }
                if (value is string s8 && Guid.TryParse(s8, out var pg)) { converted = pg; return true; }
            }

            // Fallback using Convert.ChangeType for IConvertible types
            if (value is IConvertible)
            {
                converted = Convert.ChangeType(value, underlying);
                return true;
            }
        }
        catch
        {
            // ignore and fall through
        }

        converted = null;
        return false;
    }
}