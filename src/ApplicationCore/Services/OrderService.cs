using System;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        try
        {
            // 🔹 Entry log
            _logger.LogInformation("Starting order creation for BasketId {BasketId}", basketId);

            var basketSpec = new BasketWithItemsSpecification(basketId);
            var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

            Guard.Against.Null(basket, nameof(basket));
            Guard.Against.EmptyBasketOnCheckout(basket.Items);

            _logger.LogInformation("Fetched basket for BuyerId {BuyerId} with {ItemCount} items",
                basket.BuyerId, basket.Items.Count);

            var catalogItemsSpecification = new CatalogItemsSpecification(
                basket.Items.Select(item => item.CatalogItemId).ToArray());

            var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

            _logger.LogInformation("Fetched {Count} catalog items from database", catalogItems.Count);

            var items = basket.Items.Select(basketItem =>
            {
                var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);

                // 🔹 Debug each item
                _logger.LogInformation("Processing ItemId {ItemId} | Requested Qty {Qty} | Available Stock {Stock}",
                    catalogItem.Id, basketItem.Quantity, catalogItem.AvailableStock);

                // 🔹 Stock validation (important)
                if (basketItem.Quantity > catalogItem.AvailableStock)
                {
                    _logger.LogWarning("Stock validation failed for ItemId {ItemId}. Requested {Qty}, Available {Stock}",
                        catalogItem.Id, basketItem.Quantity, catalogItem.AvailableStock);

                    throw new Exception($"Insufficient stock for item {catalogItem.Name}");
                }

                var itemOrdered = new CatalogItemOrdered(
                    catalogItem.Id,
                    catalogItem.Name,
                    _uriComposer.ComposePicUri(catalogItem.PictureUri));

                return new OrderItem(
                    itemOrdered,
                    basketItem.UnitPrice,
                    basketItem.Quantity);
            }).ToList();

            _logger.LogInformation("Creating order for BuyerId {BuyerId} with {ItemCount} items",
                basket.BuyerId, items.Count);

            var order = new Order(basket.BuyerId, shippingAddress, items);

            await _orderRepository.AddAsync(order);

            _logger.LogInformation("Order successfully created for BuyerId {BuyerId}", basket.BuyerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order creation failed for BasketId {BasketId}", basketId);
            throw;
        }
    }
}
