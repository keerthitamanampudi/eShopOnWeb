using BlazorShared.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.Web.Services;
using Microsoft.eShopWeb.Web.ViewModels;

public class IndexModel : PageModel
{
    private readonly ICatalogViewModelService _catalogViewModelService;

    public IndexModel(ICatalogViewModelService catalogViewModelService)
    {
        _catalogViewModelService = catalogViewModelService;
    }

    public required CatalogIndexViewModel CatalogModel { get; set; } = new CatalogIndexViewModel();

    // 🔥 ADD THIS
    [BindProperty(SupportsGet = true)]
    public string? SearchString { get; set; }

    public async Task OnGet(CatalogIndexViewModel catalogModel, int? pageId)
    {
        CatalogModel = await _catalogViewModelService.GetCatalogItems(
            pageId ?? 0,
            Constants.ITEMS_PER_PAGE,
            catalogModel.BrandFilterApplied,
            catalogModel.TypesFilterApplied,
            SearchString   // 👈 PASS HERE
        );
    }
}
