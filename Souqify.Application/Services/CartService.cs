using Microsoft.Extensions.Logging;
using Souqify.Application.DTOs.Cart;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Application.Models;
using Souqify.Domain.Entities;
using System.Net.Http.Headers;

namespace Souqify.Application.Services
{
    /// <summary>
    /// Cart service for both guest and logged-in users.
    ///
    /// Guests: the cart lives only in Redis (Postgres is never consulted). Logged-in
    /// users: Postgres is the source of truth with Redis as a cache-aside layer. In both
    /// cases the cache stores only the frozen "what the user saw" facts (PriceAtAdded,
    /// Quantity); live price and stock are re-fetched from the catalog and re-validated
    /// on every read and write.
    /// </summary>
    public class CartService : ICartService
    {
        private readonly ICacheStore _cacheStore;
        private readonly IProductQueries _productQueries;
        private readonly ICartRepository _cartRepository;
        private readonly ILogger<CartService> _logger;

        // Named so the TTL policy lives in one place instead of being sprinkled as magic
        // numbers across every write.
        private static readonly TimeSpan GuestCartTtl = TimeSpan.FromDays(7);
        private static readonly TimeSpan UserCartCacheTtl = TimeSpan.FromDays(30);

        public CartService(ICacheStore cacheStore, IProductQueries productQueries, ICartRepository cartRepository, ILogger<CartService> logger)
        {
            _cacheStore = cacheStore;
            _productQueries = productQueries;
            _cartRepository = cartRepository;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Reads
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the guest's cart with live price/stock re-validated against the catalog.
        /// Returns an empty cart both when none exists and when Redis is unavailable — both
        /// are safe "nothing to show" states rather than failures.
        /// </summary>
        public async Task<CartDto> GetGuestCartAsync(Guid guestId)
        {
            CachedCart? cart;

            try
            {
                cart = await _cacheStore.GetDataAsync<CachedCart>(CartKey(guestId));
            }
            catch (Exception ex)
            {
                // Degrade gracefully: a Redis outage shouldn't 500 a cart view.
                _logger.LogWarning(ex, "Redis unavailable while fetching cart for {CustomerId}, returning empty cart", guestId);
                cart = null;
            }

            if (cart == null)
                return EmptyCart();

            var liveItems = await GetLiveItemsAsync(cart.CartItems.Select(ci => ci.VariantId));

            // Read path stays pure: dead lines are dropped from the response, but we never
            // write the cleaned cart back — the throwaway remove-list is discarded. The next
            // write (and the TTL) handle real cleanup.
            var merged = MergeCachedItemsIntoLive(cart.CartItems, liveItems, new List<CachedCartItem>());

            return BuildCartDto(cart.Id, merged);
        }

        /// <summary>
        /// Returns the logged-in user's cart. Cache-aside: serve from Redis if present,
        /// otherwise fall back to Postgres and warm the cache. Live price/stock is
        /// re-validated on every read.
        /// </summary>
        public async Task<CartDto> GetUserCartAsync(Guid userId)
        {
            var cachedCart = await _cacheStore.GetDataAsync<CachedCart>(CartKey(userId));

            // Cache hit — serve from it (still re-validated against the live catalog).
            if (cachedCart != null)
            {
                var liveItems = await GetLiveItemsAsync(cachedCart.CartItems.Select(ci => ci.VariantId));
                var merged = MergeCachedItemsIntoLive(cachedCart.CartItems, liveItems, new List<CachedCartItem>());

                return BuildCartDto(cachedCart.Id, merged);
            }

            // Cache miss — fall back to the source of truth.
            var cartEnt = await _cartRepository.GetCartAsync(userId);

            if (cartEnt == null)
                return EmptyCart();

            // Entity exists but the cache was cold — build the DTO, then warm the cache.
            var live = await GetLiveItemsAsync(cartEnt.CartItems.Select(ci => ci.ProductVariantId));
            var mergedFromEntity = MergeEntityItemsIntoLive(cartEnt.CartItems.ToList(), live);
            var toCache = CreateCacheCart(mergedFromEntity, userId);

            try
            {
                return await SetCart(toCache, mergedFromEntity, userId, UserCartCacheTtl);
            }
            catch (Exception ex)
            {
                // Cache warm failed — Postgres already has the data, so still return the cart.
                _logger.LogWarning(ex, "Failed to warm cache for user cart {UserId}; serving from DB", userId);
                return BuildCartDto(cartEnt.Id, mergedFromEntity);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Add
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds an item to a guest cart: creates the cart on first add, bumps quantity if
        /// the variant is already present, or appends a new line otherwise. Every branch
        /// re-fetches live data for the whole cart in a single batched call before responding.
        /// </summary>
        public async Task<CartDto> AddGuestCartAsync(Guid guestId, CreateCartDto createCartDto)
        {
            if (createCartDto == null)
                throw new BadRequestException("create cart went wrong");

            // Guest carts live only in Redis — null means "no cart yet", not an error.
            var cart = await _cacheStore.GetDataAsync<CachedCart>(CartKey(guestId));

            // ── CASE 1: no cart yet — this is the very first item ──────────────────
            if (cart == null)
            {
                var liveDtos = await GetLiveItemsAsync(new[] { createCartDto.CartItem.VariantId });
                var newItemDto = liveDtos.FirstOrDefault();

                // Empty result = variant missing, deactivated, or its product deactivated
                // (the query's IsActive filter collapses all three into one null).
                if (newItemDto == null)
                    throw new BadRequestException("No variant matches the variantId or productId");

                var cachedItem = BuildCachedItem(newItemDto, createCartDto.CartItem.Quantity);
                ApplyNewLineToDto(newItemDto, cachedItem);

                if (newItemDto.ExceedsStock)
                    throw new BadRequestException("There are no variants in stock or you exceeded the available quantity");

                var newCart = new CachedCart
                {
                    Id = Guid.NewGuid(),
                    GuestId = guestId,
                    CartItems = new List<CachedCartItem> { cachedItem }
                };

                return await SetCart(newCart, liveDtos, guestId, GuestCartTtl);
            }

            // Cart exists — already a line for this variant, or a brand-new one?
            var existingCachedItem = cart.CartItems.FirstOrDefault(
                ci => ci.VariantId == createCartDto.CartItem.VariantId
                   && ci.ProductId == createCartDto.CartItem.ProductId);

            // ── CASE 2: cart exists, brand-new variant ─────────────────────────────
            if (existingCachedItem == null)
            {
                // One batched fetch: existing lines + the new id (avoids N+1).
                var variantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();
                variantIds.Add(createCartDto.CartItem.VariantId);

                var liveDtos = await GetLiveItemsAsync(variantIds);

                // Delete any cached line whose product went dead since it was added.
                var toRemove = new List<CachedCartItem>();
                liveDtos = MergeCachedItemsIntoLive(cart.CartItems, liveDtos, toRemove);
                RemoveItemsFromCachedCart(toRemove, cart.CartItems);

                var newItemDto = liveDtos.FirstOrDefault(l => l.VariantId == createCartDto.CartItem.VariantId);

                if (newItemDto == null)
                    throw new BadRequestException("No variant matches the variantId or productId");

                var cachedItem = BuildCachedItem(newItemDto, createCartDto.CartItem.Quantity);
                ApplyNewLineToDto(newItemDto, cachedItem);

                if (newItemDto.ExceedsStock)
                    throw new BadRequestException("You exceeded the available stock");

                cart.CartItems.Add(cachedItem);
                return await SetCart(cart, liveDtos, guestId, GuestCartTtl);
            }

            // ── CASE 3: variant already in the cart — bump its quantity ────────────
            // Re-fetch everything: price/stock is re-validated on every write, not just for
            // the touched line.
            var allVariantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();
            var liveAllDtos = await GetLiveItemsAsync(allVariantIds);

            // Bump before the merge: existingItem is the same reference stored in
            // cart.CartItems, so the merge sees the updated quantity.
            existingCachedItem.Quantity += createCartDto.CartItem.Quantity;

            var deadCachedItems = new List<CachedCartItem>();
            liveAllDtos = MergeCachedItemsIntoLive(cart.CartItems, liveAllDtos, deadCachedItems);

            if (deadCachedItems.Count > 0)
            {
                // Touched line went dead → reject. Bystanders → prune silently.
                if (deadCachedItems.Any(item => item.VariantId == existingCachedItem.VariantId))
                    throw new BadRequestException("This item has been removed or deactivated");

                RemoveItemsFromCachedCart(deadCachedItems, cart.CartItems);
            }

            var exceededDto = liveAllDtos.FirstOrDefault(ci => ci.VariantId == existingCachedItem.VariantId && ci.ExceedsStock);
            if (exceededDto != null)
                throw new BadRequestException("You exceeded the available stock");

            // Price drift is enforced at checkout per ADR, not on cart edits — the rejected
            // alternative is kept here as a deliberate reminder:
            //   if (liveAll.First(l => l.VariantId == existingItem.VariantId).PriceChanged)
            //       throw new BadRequestException("the price of this product has been changed");

            return await SetCart(cart, liveAllDtos, guestId, GuestCartTtl);
        }

        /// <summary>
        /// Adds an item to a logged-in user's cart in Postgres, then invalidates the cache
        /// so the next read re-hydrates from the persisted state.
        /// </summary>
        public async Task<CartDto> AddUserCartAsync(Guid userId, CreateCartDto createCartDto)
        {
            var cartEnt = await _cartRepository.GetCartAsync(userId);
            var isNewCart = cartEnt == null;
            cartEnt ??= new Cart(userId);

            var existingItem = cartEnt.CartItems.FirstOrDefault(
                ci => ci.ProductVariantId == createCartDto.CartItem.VariantId);

            var liveAllDtos = (await GetLiveItemsAsync(new[] { createCartDto.CartItem.VariantId })).FirstOrDefault()
                    ?? throw new BadRequestException("Variant not found or unavailable");
            

            if (existingItem != null)
            {
                liveAllDtos.Quantity = createCartDto.CartItem.Quantity + existingItem.Quantity;

                if (liveAllDtos.ExceedsStock)
                    throw new BadRequestException("There are not enough vailable stocks");

                existingItem.IncreaseQuantity(createCartDto.CartItem.Quantity);
            }
            else
            {
                
                liveAllDtos.Quantity = createCartDto.CartItem.Quantity;

                if (liveAllDtos.ExceedsStock)
                    throw new BadRequestException("There are not enough available stocks");

                var newItemEnt = new CartItem(
                    liveAllDtos.ProductId,
                    liveAllDtos.VariantId,
                    cartEnt.Id,
                    createCartDto.CartItem.Quantity,
                    liveAllDtos.CurrentPrice
                    );

                cartEnt.AddItem(newItemEnt);
            }

            if (isNewCart)
                await _cartRepository.AddCartAsync(cartEnt);

            await _cartRepository.SaveChangesAsync();
            await InvalidateUserCartCache(userId);

            return await GetUserCartAsync(userId);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Decrease
        // ─────────────────────────────────────────────────────────────────────

        public async Task<CartDto> DecreaseUserCartItemsQuantityAsync(Guid userId, Guid cartItemId)
        {
            var cartEnt = await _cartRepository.GetCartAsync(userId)
                ?? throw new BadRequestException("Cart already deleted or not found");

            var cartItem = cartEnt.CartItems.FirstOrDefault(ci => ci.Id == cartItemId)
                ?? throw new BadRequestException("Cart itemno  already deleted or not found");

            if (cartItem.Quantity > 1)
                cartItem.DecreaseQuantity();
            else
                cartEnt.RemoveItem(cartItem);

            if (cartEnt.CartItems.Count == 0)
                await _cartRepository.DeleteCartAsync(cartEnt);


            await _cartRepository.SaveChangesAsync();
            await InvalidateUserCartCache(userId);

            return await GetUserCartAsync(userId);
        }

        public async Task<CartDto> DecreaseGuestCartItemQuantityAsync(Guid guestId, Guid cartItemId)
        {
            var cart = await _cacheStore.GetDataAsync<CachedCart>(CartKey(guestId));

            if (cart == null)
                return EmptyCart();

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem == null)
                return await GetGuestCartAsync(guestId);

            if (cartItem.Quantity > 1)
                cartItem.Quantity -= 1;
            else
                cart.CartItems.RemoveAll(ci => ci.Id == cartItem.Id);

            await _cacheStore.SetDataAsync(CartKey(guestId), cart, GuestCartTtl);

            return await GetGuestCartAsync(guestId);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Delete item
        // ─────────────────────────────────────────────────────────────────────

        public async Task<CartDto> DeleteUserCartItemAsync(Guid userId, Guid cartItemId)
        {
            var cartEnt = await _cartRepository.GetCartAsync(userId)
                ?? throw new InvalidOperationException("Cart missing for authenticated user");

            var cartItem = cartEnt.CartItems.FirstOrDefault(ci => ci.Id == cartItemId)
                ?? throw new InvalidOperationException("Cart item cant be deleted, its already deleted");

            cartEnt.RemoveItem(cartItem);

            await _cartRepository.SaveChangesAsync();
            await InvalidateUserCartCache(userId);

            return await GetUserCartAsync(userId);
        }

        /// <summary>
        /// Removes a single line by its cart-item id. Idempotent: a missing cart or a missing
        /// item returns the current cart rather than throwing — the desired end state
        /// (item gone) already holds.
        /// </summary>
        public async Task<CartDto> DeleteGuestCartItemAsync(Guid guestId, Guid cartItemId)
        {
            var cart = await _cacheStore.GetDataAsync<CachedCart>(CartKey(guestId));

            if (cart == null)
                return EmptyCart();

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem == null)
                return await GetGuestCartAsync(guestId);

            cart.CartItems.Remove(cartItem);

            await _cacheStore.SetDataAsync(CartKey(guestId), cart, GuestCartTtl);

            return await GetGuestCartAsync(guestId);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Delete cart
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Clears the entire guest cart. Idempotent: clearing a non-existent cart is a no-op
        /// success, since the goal ("no cart") already holds.
        /// </summary>
        public async Task<CartDto> DeleteGuestCartAsync(Guid customerId)
        {
            await _cacheStore.RemoveDataAsync(CartKey(customerId));
            return EmptyCart();
        }

        public async Task<CartDto> DeleteUserCartAsync(Guid userId)
        {
            var cartEnt = await _cartRepository.GetCartAsync(userId)
                ?? throw new InvalidOperationException("Cart missing for authenticated user");

            await _cartRepository.DeleteCartAsync(cartEnt);

            await _cartRepository.SaveChangesAsync();

            await InvalidateUserCartCache(userId);
            return EmptyCart();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Merge (guest → user on login)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Folds a guest's Redis cart into the user's Postgres cart on login: creates the
        /// user cart if absent, otherwise bumps-or-adds each guest line. Both caches are
        /// dropped afterward so the next read re-hydrates from the merged DB state.
        /// </summary>
        public async Task MergeGuestCartAsync(Guid? guestId, Guid userId)
        {
            // Nothing to merge if there's no guest identity.
            if (guestId == null || guestId == Guid.Empty)
                return;

            var cachedCart = await _cacheStore.GetDataAsync<CachedCart>(CartKey(guestId.Value));

            // Guest never had a cart — nothing to carry over.
            if (cachedCart == null)
                return;

            var cartEnt = await _cartRepository.GetCartAsync(userId);

            if (cartEnt == null)
            {
                // User had no cart — create one straight from the guest's lines.
                cartEnt = new Cart(userId);

                foreach (var item in cachedCart.CartItems)
                {
                    var cartItem = new CartItem(item.ProductId, item.VariantId, cartEnt.Id, item.Quantity, item.PriceAtAdded);
                    cartEnt.AddItem(cartItem);
                }

                await _cartRepository.AddCartAsync(cartEnt);
            }
            else
            {
                // User already has a cart — fold each guest line in: bump if the variant is
                // present, otherwise add it as a new line.
                var itemsByVariant = cartEnt.CartItems.ToDictionary(ci => ci.ProductVariantId);

                foreach (var item in cachedCart.CartItems)
                {
                    if (itemsByVariant.TryGetValue(item.VariantId, out var existing))
                        existing.IncreaseQuantity(item.Quantity);
                    else
                        cartEnt.AddItem(new CartItem(item.ProductId, item.VariantId, cartEnt.Id, item.Quantity, item.PriceAtAdded));
                }
            }

            await _cartRepository.SaveChangesAsync();

            // Drop both caches so the next read re-hydrates from the merged DB state.
            await InvalidateUserCartCache(userId);
            await _cacheStore.RemoveDataAsync(CartKey(guestId.Value));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Cache key for a cart, keyed by its owner id (guest or user).</summary>
        private static string CartKey(Guid ownerId) => ownerId.ToString();

        /// <summary>The canonical "nothing to show" cart.</summary>
        private static CartDto EmptyCart() => new CartDto
        {
            Id = Guid.Empty,
            CartItems = new List<CartItemDto>(),
            TotalPrice = 0
        };

        /// <summary>Single place that decides how a cart's total is computed.</summary>
        private static CartDto BuildCartDto(Guid id, List<CartItemDto> items) => new CartDto
        {
            Id = id,
            CartItems = items,
            TotalPrice = items.Sum(ci => ci.lineTotal)
        };

        /// <summary>Batched live catalog lookup for a set of variant ids.</summary>
        private async Task<List<CartItemDto>> GetLiveItemsAsync(IEnumerable<Guid> variantIds) =>
            (await _productQueries.GetCartItemDetailsByVariantIdsAsync(variantIds.ToList())).ToList();

        /// <summary>Builds the frozen cache snapshot for a line. PriceAtAdded is captured
        /// from the live price once, here, and never re-read.</summary>
        private static CachedCartItem BuildCachedItem(CartItemDto live, int quantity) => new CachedCartItem
        {
            Id = Guid.NewGuid(),
            VariantId = live.VariantId,
            ProductId = live.ProductId,
            Brand = live.Brand,
            ProductName = live.ProductName,
            PriceAtAdded = live.CurrentPrice,
            Color = live.Color,
            Size = live.Size,
            MainImgUrl = live.MainImgUrl,
            Quantity = quantity
        };

        /// <summary>Fills the response DTO's cache-side fields for a brand-new line. By
        /// definition PriceAtAdded == CurrentPrice, so PriceChanged is false.</summary>
        private static void ApplyNewLineToDto(CartItemDto dto, CachedCartItem cached)
        {
            dto.Id = cached.Id;
            dto.PriceAtAdded = cached.PriceAtAdded;
            dto.Quantity = cached.Quantity;
        }

        /// <summary>
        /// Persists the cart to Redis and builds the response DTO. A write failure is a real
        /// failure (no fallback store for a cache-only write): it's logged with context and
        /// re-thrown rather than masked as a fake success.
        /// </summary>
        private async Task<CartDto> SetCart(CachedCart cart, List<CartItemDto> items, Guid ownerId, TimeSpan ttl)
        {
            try
            {
                await _cacheStore.SetDataAsync(CartKey(ownerId), cart, ttl);
                return BuildCartDto(cart.Id, items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save cart to Redis for {OwnerId}", ownerId);
                throw;
            }
        }

        /// <summary>Drops a user's cached cart. Cache-aside over Postgres: the next read
        /// re-hydrates from the DB (the source of truth). A missing key is a no-op.</summary>
        private async Task InvalidateUserCartCache(Guid userId) =>
            await _cacheStore.RemoveDataAsync(CartKey(userId));

        /// <summary>
        /// Overlays the frozen cache facts (PriceAtAdded, Quantity) from <paramref name="cachedItems"/>
        /// onto the matching live catalog rows, keyed by VariantId, and computes PriceChanged /
        /// lineTotal / ExceedsStock. Cached lines with no live match (deactivated since they were
        /// added) are collected into <paramref name="toRemove"/> and left out of the result.
        /// </summary>
        private List<CartItemDto> MergeCachedItemsIntoLive(List<CachedCartItem> cachedItems, List<CartItemDto> liveItems, List<CachedCartItem> toRemove)
        {
            var liveByVariant = liveItems.ToDictionary(d => d.VariantId);

            foreach (var item in cachedItems)
            {
                // No live row = item went unavailable after it was cached → flag for removal.
                if (liveByVariant.TryGetValue(item.VariantId, out var live))
                {
                    live.Id = item.Id;
                    live.PriceAtAdded = item.PriceAtAdded;
                    live.Quantity = item.Quantity;
                }
                else
                {
                    toRemove.Add(item);
                    _logger.LogInformation("Cart item failed to be retrieved with Id {VariantId}", item.VariantId);
                }
            }

            return liveByVariant.Values.ToList();
        }

        /// <summary>
        /// Same overlay as <see cref="MergeCachedItemsIntoLive"/> but sourced from persisted
        /// <see cref="CartItem"/> entities. Lines with no live match are simply dropped
        /// (not tracked for removal — the DB row stays; only the response omits it).
        /// </summary>
        private List<CartItemDto> MergeEntityItemsIntoLive(List<CartItem> entityItems, List<CartItemDto> liveItems)
        {
            var liveByVariant = liveItems.ToDictionary(d => d.VariantId);

            foreach (var item in entityItems)
            {
                if (liveByVariant.TryGetValue(item.ProductVariantId, out var live))
                {
                    live.Id = item.Id;
                    live.PriceAtAdded = item.PriceAtAdded;
                    live.Quantity = item.Quantity;
                }
            }

            return liveByVariant.Values.ToList();
        }

        /// <summary>Builds a CachedCart from response DTOs (used to warm the user cache).</summary>
        private static CachedCart CreateCacheCart(List<CartItemDto> cartItemsDtos, Guid userId)
        {
            var cart = new CachedCart
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };

            foreach (var item in cartItemsDtos)
            {
                cart.CartItems.Add(new CachedCartItem
                {
                    Id = item.Id,
                    Brand = item.Brand,
                    ProductName = item.ProductName,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    Color = item.Color,
                    Size = item.Size,
                    MainImgUrl = item.MainImgUrl,
                    PriceAtAdded = item.PriceAtAdded,
                    Quantity = item.Quantity
                });
            }

            return cart;
        }

        /// <summary>
        /// Removes from the cache model every item whose VariantId appears in the remove
        /// list. Matched by VariantId (meaning), not reference. Mutates in place.
        /// </summary>
        private static void RemoveItemsFromCachedCart(List<CachedCartItem> removeItemList, List<CachedCartItem> cartItems)
        {
            cartItems.RemoveAll(ci => removeItemList.Any(r => r.VariantId == ci.VariantId));
        }
    }
}