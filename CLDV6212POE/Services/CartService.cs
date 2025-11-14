using CLDV6212POE.Models;

namespace CLDV6212POE.Services
{
    public class CartService
    {
        private readonly TableStorageService<Cart> _cartTable;

        public CartService(TableStorageService<Cart> cartTable)
        {
            _cartTable = cartTable;
        }

        // Add item or increase quantity
        public async Task AddToCartAsync(Cart cartItem)
        {
            var existing = await _cartTable.GetEntityAsync(cartItem.PartitionKey, cartItem.ProductId);

            if (existing != null)
            {
                existing.Quantity += 1;
                await _cartTable.UpdateEntityAsync(existing, existing.PartitionKey, existing.RowKey);
            }
            else
            {
                cartItem.RowKey = cartItem.ProductId;
                cartItem.AddedDate = DateTime.UtcNow;
                await _cartTable.StoreEntityAsync(cartItem, cartItem.PartitionKey, cartItem.RowKey);
            }
        }

        // Get all cart items for a specific user
        public async Task<List<Cart>> GetUserCartAsync(string userId)
        {
            var allItems = await _cartTable.GetAllEntitiesAsync();
            return allItems
                .Where(x => x.PartitionKey == userId)
                .OrderByDescending(x => x.AddedDate)
                .ToList();
        }

        // Remove a single product from cart
        public async Task RemoveFromCartAsync(string userId, string productId)
        {
            await _cartTable.DeleteEntityAsync(userId, productId);
        }

        // Clear entire cart for user
        public async Task ClearCartAsync(string userId)
        {
            var items = await GetUserCartAsync(userId);
            foreach (var item in items)
                await _cartTable.DeleteEntityAsync(userId, item.RowKey);
        }

        // Get all items in a user's cart using TableClient query
        public async Task<IEnumerable<Cart>> GetAllAsync(string customerId)
        {
            var allItems = await _cartTable.GetAllEntitiesAsync();
            return allItems.Where(c => c.PartitionKey == customerId).ToList();
        }

    }
}
