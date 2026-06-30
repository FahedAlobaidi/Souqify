using Microsoft.Extensions.Logging;
using Souqify.Application.DTOs.Cart;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Application.Models;
using System.ComponentModel.DataAnnotations;

namespace Souqify.Application.Services
{
    /// <summary>
    /// Guest cart service backed entirely by Redis (Postgres is never consulted for
    /// cart existence). The cache stores only frozen "what the user saw" facts
    /// (PriceAtAdded, Quantity); live price and stock are re-fetched and re-validated
    /// against the catalog on every read and write.
    /// </summary>
    public class CartService : ICartService
    {
        private readonly ICacheStore _cacheStore;
        private readonly IProductQueries _productQueries;
        private readonly ILogger<CartService> _logger;

        public CartService(ICacheStore cacheStore, IProductQueries productQueries, ILogger<CartService> logger)
        {
            _cacheStore = cacheStore;
            _productQueries = productQueries;
            _logger = logger;
        }

        /// <summary>
        /// Returns the guest's cart with live price/stock re-validated against the catalog.
        /// Returns an empty cart both when none exists and when Redis is unavailable —
        /// both are safe "nothing to show" states rather than failures.
        /// </summary>
        public async Task<CartDto> GetCartAsync(Guid customerId, bool isGuest)
        {
            CachedCart? cart;

            try
            {
                cart = await _cacheStore.GetDataAsync<CachedCart>(customerId.ToString());
            }
            catch (Exception ex)
            {
                // Degrade gracefully: a Redis outage shouldn't 500 a cart view.
                _logger.LogWarning(ex, "Redis unavailable while fetching cart for {CustomerId}, returning empty cart", customerId);
                cart = null;
            }

            if (cart == null)
            {
                return new CartDto
                {
                    Id = Guid.NewGuid(),
                    CartItems = new List<CartItemsDto>(),
                    TotalPrice = 0
                };
            }

            var variantIdsInCacheCart = cart.CartItems.Select(ci => ci.VariantId).ToList();

            var liveCartItems = (await _productQueries.GetCartItemDetailsByVariantIdsAsync(variantIdsInCacheCart)).ToList();

            // Read path stays pure: dead items are dropped from the response, but the
            // throwaway list is discarded — we never write back here. Cache cleanup is
            // left to the next write (and the 7-day TTL as a backstop).
            liveCartItems = MergeLiveAndCachedItems(cart.CartItems, liveCartItems, new List<CachedCartItems>());

            var cartDto = new CartDto();
            cartDto.Id = cart.Id;
            cartDto.CartItems = liveCartItems;
            cartDto.TotalPrice = liveCartItems.Sum(cartItem => cartItem.lineTotal);

            return cartDto;
        }

        /// <summary>
        /// Adds an item: creates the cart on first add, bumps quantity if the variant is
        /// already present, or appends a new line otherwise. Every branch re-fetches live
        /// data for the whole cart in a single batched call before responding.
        /// </summary>
        public async Task<CartDto> AddCartAsync(Guid customerId, CreateCartDto createCartDto)
        {
            if (createCartDto == null)
                throw new BadRequestException("create cart went wrong");

            // Guest carts live only in Redis — null means "no cart yet", not an error.
            var cart = await _cacheStore.GetDataAsync<CachedCart>(customerId.ToString());

            if (cart == null)
            {
                // ── CASE 1: first item ever — nothing to merge into ──────────────────
                var requestedVariantIds = new List<Guid> { createCartDto.CartItem.VariantId };

                var liveItemDetails = (await _productQueries.GetCartItemDetailsByVariantIdsAsync(requestedVariantIds)).ToList();

                var newItemLiveData = liveItemDetails.FirstOrDefault();

                // Empty result = variant missing, deactivated, or parent product deactivated;
                // the query's IsActive filter collapses all three into one null check.
                if (newItemLiveData == null)
                    throw new BadRequestException("No variant matches the variantId or productId");

                // Frozen snapshot persisted to Redis. PriceAtAdded is captured once here
                // and never re-read; Quantity comes from the request — price is never
                // trusted from the client.
                var newCachedItem = new CachedCartItems
                {
                    Id = Guid.NewGuid(),
                    VariantId = newItemLiveData.VariantId,
                    ProductId = newItemLiveData.ProductId,
                    Brand = newItemLiveData.Brand,
                    ProductName = newItemLiveData.ProductName,
                    PriceAtAdded = newItemLiveData.CurrentPrice,
                    Color = newItemLiveData.Color,
                    Size = newItemLiveData.Size,
                    MainImgUrl = newItemLiveData.MainImgUrl,
                    Quantity = createCartDto.CartItem.Quantity
                };

                // Fill the response DTO's cache-side fields. Brand new line, so
                // PriceAtAdded == CurrentPrice and PriceChanged is false by definition.
                newItemLiveData.Id = newCachedItem.Id;
                newItemLiveData.PriceAtAdded = newCachedItem.PriceAtAdded;
                newItemLiveData.PriceChanged = false;
                newItemLiveData.Quantity = newCachedItem.Quantity;
                newItemLiveData.lineTotal = newItemLiveData.PriceAtAdded * newItemLiveData.Quantity;
                newItemLiveData.ExceedsStock = newCachedItem.Quantity > newItemLiveData.AvailableStock;

                // ExceedsStock also covers out-of-stock: with Quantity >= 1, stock 0
                // always exceeds. (Holds only while the validator guarantees Quantity >= 1.)
                if (newItemLiveData.ExceedsStock)
                    throw new BadRequestException("There are no variants in stock or you exceeded the available quantity");

                var newCart = new CachedCart
                {
                    GuestId = customerId,
                    CartItems = new List<CachedCartItems> { newCachedItem },
                    Id = Guid.NewGuid(),
                };

                // TotalPrice is computed once in SetCart, never stored on the cache model.
                return await SetCart(newCart, liveItemDetails);
            }
            else
            {
                // Match on (VariantId, ProductId) to decide bump vs. new line.
                var existingCartItem = cart.CartItems
                    .Where(ci => ci.VariantId == createCartDto.CartItem.VariantId && ci.ProductId == createCartDto.CartItem.ProductId)
                    .FirstOrDefault();

                List<CartItemsDto> liveItemDetails = new List<CartItemsDto>();

                if (existingCartItem == null)
                {
                    // ── CASE 2: cart exists, new variant ─────────────────────────────
                    // One batched call for existing items + the new id (avoids N+1).
                    var requestedVariantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();
                    requestedVariantIds.Add(createCartDto.CartItem.VariantId);

                    liveItemDetails = (await _productQueries.GetCartItemDetailsByVariantIdsAsync(requestedVariantIds)).ToList();

                    // Merge reports cached items with no live match (deactivated since added);
                    // prune them from the cart so they aren't re-persisted.
                    var removeItemsFromCacheList = new List<CachedCartItems>();

                    liveItemDetails = MergeLiveAndCachedItems(cart.CartItems, liveItemDetails, removeItemsFromCacheList);

                    RemoveItemsFromCachedCart(removeItemsFromCacheList, cart.CartItems);

                    var newItemLiveData = liveItemDetails.Where(liveItem => liveItem.VariantId == createCartDto.CartItem.VariantId).FirstOrDefault();

                    // Acted-on variant has no live row → unavailable; reject explicitly.
                    if (newItemLiveData == null)
                        throw new BadRequestException("No variant matches the variantId or productId");

                    var newCachedItem = new CachedCartItems
                    {
                        Id = Guid.NewGuid(),
                        ProductId = newItemLiveData.ProductId,
                        VariantId = newItemLiveData.VariantId,
                        PriceAtAdded = newItemLiveData.CurrentPrice,
                        Size = newItemLiveData.Size,
                        Color = newItemLiveData.Color,
                        Brand = newItemLiveData.Brand,
                        MainImgUrl = newItemLiveData.MainImgUrl,
                        ProductName = newItemLiveData.ProductName,
                        Quantity = createCartDto.CartItem.Quantity,
                    };

                    newItemLiveData.PriceAtAdded = newCachedItem.PriceAtAdded;
                    newItemLiveData.PriceChanged = false;
                    newItemLiveData.Quantity = newCachedItem.Quantity;
                    newItemLiveData.lineTotal = newItemLiveData.PriceAtAdded * newItemLiveData.Quantity;
                    newItemLiveData.ExceedsStock = newCachedItem.Quantity > newItemLiveData.AvailableStock;

                    if (newItemLiveData.ExceedsStock)
                        throw new BadRequestException("You exceeded the available stock");

                    cart.CartItems.Add(newCachedItem);
                    return await SetCart(cart, liveItemDetails);
                }
                else
                {
                    // ── CASE 3: variant already in cart — bump quantity ──────────────
                    // Still re-fetch every item: price/stock re-validation runs on every
                    // write, not just for the line being touched.
                    var requestedVariantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();

                    liveItemDetails = (await _productQueries.GetCartItemDetailsByVariantIdsAsync(requestedVariantIds)).ToList();

                    // Bump before the merge: existingCartItem is the same reference held in
                    // cart.CartItems, so the merge reads the already-updated quantity.
                    existingCartItem.Quantity = existingCartItem.Quantity + createCartDto.CartItem.Quantity;

                    var removeItemsFromCacheList = new List<CachedCartItems>();

                    liveItemDetails = MergeLiveAndCachedItems(cart.CartItems, liveItemDetails, removeItemsFromCacheList);

                    if (removeItemsFromCacheList.Count > 0)
                    {
                        // Acted-on item went dead → reject. Bystanders → prune silently.
                        if (removeItemsFromCacheList.Any(item => item.VariantId == existingCartItem.VariantId))
                        {
                            throw new BadRequestException("This item has been removed or deactivated");
                        }
                        RemoveItemsFromCachedCart(removeItemsFromCacheList, cart.CartItems);
                    }

                    var exceededItem = liveItemDetails.Where(ci => ci.VariantId == existingCartItem.VariantId && ci.ExceedsStock).FirstOrDefault();

                    if (exceededItem != null)
                        throw new BadRequestException("You exceeded the available stock");

                    // Price drift is enforced at checkout per ADR, not on cart edits —
                    // kept here as a deliberate reminder of the rejected alternative.
                    //if (liveDataByVariantId[createCartDto.VariantId].PriceChanged)
                    //    throw new BadRequestException("the price of this product has been changed");

                    return await SetCart(cart, liveItemDetails);
                }
            }
        }

        

        /// <summary>
        /// Removes a single line by its cart-item id. Idempotent: a missing cart or
        /// missing item returns false rather than throwing — the desired end state
        /// (item gone) already holds.
        /// </summary>
        public async Task<CartDto> DeleteCartItemAsync(Guid customerId, Guid cartItemId)
        {
            var cart = await _cacheStore.GetDataAsync<CachedCart>(customerId.ToString());

            if (cart == null)
                return new CartDto
                {
                    Id = Guid.NewGuid(),
                    CartItems = new List<CartItemsDto>(),
                    TotalPrice = 0
                };

            var cachedCartItem = cart.CartItems.Where(ci => ci.Id == cartItemId).FirstOrDefault();

            if (cachedCartItem == null)
                return await GetCartAsync(customerId, false);

            cart.CartItems.Remove(cachedCartItem);

            await _cacheStore.SetDataAsync<CachedCart>(customerId.ToString(), cart, TimeSpan.FromDays(7));

            return await GetCartAsync(customerId, false);
        }

        public async Task<CartDto> DecreaseCartItemQuantityAsync(Guid customerId,Guid cartItemId)
        {
            var cart = await _cacheStore.GetDataAsync<CachedCart>(customerId.ToString());

            if (cart == null)
            {
                return new CartDto
                {
                    Id = Guid.NewGuid(),
                    CartItems = new List<CartItemsDto>(),
                    TotalPrice = 0
                };
            }

            var cartItem = cart.CartItems.Where(ci => ci.Id == cartItemId).FirstOrDefault();
            if (cartItem == null)
                return await GetCartAsync(customerId, false);

            if(cartItem.Quantity > 1)
            {
                cartItem.Quantity = cartItem.Quantity - 1;

                await _cacheStore.SetDataAsync(customerId.ToString(), cart, TimeSpan.FromDays(7));

                return await GetCartAsync(customerId, false);
            }
            else
            {
                cart.CartItems.RemoveAll(ci => ci.Id == cartItem.Id);

                await _cacheStore.SetDataAsync(customerId.ToString(), cart, TimeSpan.FromDays(7));

                return await GetCartAsync(customerId,false);
            }
        }

        /// <summary>
        /// Clears the entire cart. Idempotent: clearing a non-existent cart is a no-op
        /// success, since the goal ("no cart") already holds.
        /// </summary>
        public async Task<CartDto> DeleteCartAsync(Guid customerId)
        {
            await _cacheStore.RemoveDataAsync(customerId.ToString());

            return new CartDto
            {
                Id = Guid.NewGuid(),
                CartItems = new List<CartItemsDto>(),
                TotalPrice = 0
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Persists the cart to Redis and builds the response DTO. A write failure is a
        /// real failure (no fallback store for guests): it's logged with context and
        /// re-thrown rather than masked as a fake success.
        /// </summary>
        private async Task<CartDto> SetCart(CachedCart cart, List<CartItemsDto> cartItemsDtoList)
        {
            try
            {
                await _cacheStore.SetDataAsync((cart.GuestId).ToString(), cart, TimeSpan.FromDays(7));

                var cartDto = new CartDto
                {
                    Id = cart.Id,
                    CartItems = cartItemsDtoList,
                    TotalPrice = cartItemsDtoList.Sum(ci => ci.lineTotal)
                };

                return cartDto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save cart to Redis for guest {GuestId}", cart.GuestId);
                throw;
            }
        }

        /// <summary>
        /// Overlays frozen cache facts (PriceAtAdded, Quantity) onto live catalog facts,
        /// matched by VariantId, and computes PriceChanged / lineTotal / ExceedsStock.
        /// Cached items with no live match (deactivated since added) are collected into
        /// <paramref name="removeItemList"/> for the caller to prune, and excluded from
        /// the returned list.
        /// </summary>
        private List<CartItemsDto> MergeLiveAndCachedItems(List<CachedCartItems> cachedCartItems, List<CartItemsDto> liveCartItems, List<CachedCartItems> removeItemList)
        {
            var liveDataByVariantId = liveCartItems.ToDictionary(d => d.VariantId);

            foreach (var item in cachedCartItems)
            {
                // No live row = item went unavailable after it was cached → flag for removal.
                if (liveDataByVariantId.TryGetValue(item.VariantId, out var liveData))
                {
                    liveData.Id = item.Id;
                    liveData.PriceAtAdded = item.PriceAtAdded;
                    liveData.Quantity = item.Quantity;
                    liveData.PriceChanged = liveData.PriceAtAdded != liveData.CurrentPrice;
                    liveData.lineTotal = liveData.CurrentPrice * liveData.Quantity;
                    liveData.ExceedsStock = item.Quantity > liveData.AvailableStock;
                }
                else
                {
                    removeItemList.Add(item);
                    _logger.LogInformation("Cart item failed to be retrieved with Id {VariantId}", item.VariantId);
                }
            }

            liveCartItems = liveDataByVariantId.Values.ToList();

            return liveCartItems;
        }

        /// <summary>
        /// Removes from the cache model every item whose VariantId appears in the
        /// remove list. Matched by VariantId (meaning), not reference. Mutates in place.
        /// </summary>
        private void RemoveItemsFromCachedCart(List<CachedCartItems> removeItemList, List<CachedCartItems> cartItems)
        {
            cartItems.RemoveAll(ci => removeItemList.Any(r => r.VariantId == ci.VariantId));
        }
    }
}