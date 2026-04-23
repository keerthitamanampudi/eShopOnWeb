using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.eShopWeb.Web.ViewModels;

namespace Microsoft.eShopWeb.Web.Pages.Basket;

public class IndexModel : PageModel
{
    private readonly IBasketService _basketService;
    private readonly IBasketViewModelService _basketViewModelService;
    private readonly IRepository<CatalogItem> _itemRepository;

    public IndexModel(IBasketService basketService,
        IBasketViewModelService basketViewModelService,
        IRepository<CatalogItem> itemRepository)
    {
        _basketService = basketService;
        _basketViewModelService = basketViewModelService;
        _itemRepository = itemRepository;
    }

    public BasketViewModel BasketModel { get; set; } = new BasketViewModel();

    public async Task OnGet()
    {
        BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(GetOrSetBasketCookieAndUserName());
    }

    public async Task<IActionResult> OnPost(CatalogItemViewModel productDetails)
    {
        if (productDetails?.Id == null)
        {
            return RedirectToPage("/Index");
        }

        var item = await _itemRepository.GetByIdAsync(productDetails.Id);
        if (item == null)
        {
            return RedirectToPage("/Index");
        }

        // Server-side protection: do not allow adding if out of stock
        if (item.AvailableStock <= 0)
        {
            TempData["StockMessage"] = "Item is out of stock.";
            return RedirectToPage();
        }

        var username = GetOrSetBasketCookieAndUserName();

        // check existing quantity in basket to avoid exceeding available stock
        var basketView = await _basketViewModelService.GetOrCreateBasketForUser(username);
        var existing = basketView.Items.FirstOrDefault(x => x.CatalogItemId == productDetails.Id);
        var existingQty = existing?.Quantity ?? 0;

        if (existingQty >= item.AvailableStock)
        {
            TempData["StockMessage"] = "Cannot add more than available stock.";
            return RedirectToPage();
        }

        // If adding one would exceed stock, do not add and inform user
        if (existingQty + 1 > item.AvailableStock)
        {
            TempData["StockMessage"] = $"Only {item.AvailableStock - existingQty} items left.";
            return RedirectToPage();
        }

        var basket = await _basketService.AddItemToBasket(username,
            productDetails.Id, item.Price);

        BasketModel = await _basketViewModelService.Map(basket);

        return RedirectToPage();
    }

    public async Task OnPostUpdate(IEnumerable<BasketItemViewModel> items)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var username = GetOrSetBasketCookieAndUserName();
        var basketView = await _basketViewModelService.GetOrCreateBasketForUser(username);

        // Map incoming basket item IDs to catalog item IDs
        var basketItemMap = basketView.Items.ToDictionary(b => b.Id, b => b.CatalogItemId);

        var updateModel = new Dictionary<string, int>();

        foreach (var incoming in items)
        {
            // incoming.Id is basket item id
            if (!basketItemMap.TryGetValue(incoming.Id, out var catalogId))
            {
                continue;
            }

            var catalogItem = await _itemRepository.GetByIdAsync(catalogId);
            var available = catalogItem?.AvailableStock ?? 0;
            var desiredQty = incoming.Quantity;

            // clamp to available stock
            if (desiredQty > available)
            {
                desiredQty = available;
            }
            if (desiredQty < 0)
            {
                desiredQty = 0;
            }

            updateModel[incoming.Id.ToString()] = desiredQty;
        }

        var basket = await _basketService.SetQuantities(basketView.Id, updateModel);
        BasketModel = await _basketViewModelService.Map(basket);
    }

    private string GetOrSetBasketCookieAndUserName()
    {
        Guard.Against.Null(Request.HttpContext.User.Identity, nameof(Request.HttpContext.User.Identity));
        string? userName = null;

        if (Request.HttpContext.User.Identity.IsAuthenticated)
        {
            Guard.Against.Null(Request.HttpContext.User.Identity.Name, nameof(Request.HttpContext.User.Identity.Name));
            return Request.HttpContext.User.Identity.Name!;
        }

        if (Request.Cookies.ContainsKey(Constants.BASKET_COOKIENAME))
        {
            userName = Request.Cookies[Constants.BASKET_COOKIENAME];

            if (!Request.HttpContext.User.Identity.IsAuthenticated)
            {
                if (!Guid.TryParse(userName, out var _))
                {
                    userName = null;
                }
            }
        }
        if (userName != null) return userName;

        userName = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions { IsEssential = true };
        cookieOptions.Expires = DateTime.Today.AddYears(10);
        Response.Cookies.Append(Constants.BASKET_COOKIENAME, userName, cookieOptions);

        return userName;
    }
}
