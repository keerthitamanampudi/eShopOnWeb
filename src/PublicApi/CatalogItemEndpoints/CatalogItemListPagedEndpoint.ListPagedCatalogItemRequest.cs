namespace Microsoft.eShopWeb.PublicApi.CatalogItemEndpoints;

public class ListPagedCatalogItemRequest : BaseRequest
{
    public int PageSize { get; init; }
    public int PageIndex { get; init; }
    public int? CatalogBrandId { get; init; }
    public int? CatalogTypeId { get; init; }
    public string? SearchTerm { get; init; }

    public ListPagedCatalogItemRequest(int? pageSize, int? pageIndex, int? catalogBrandId, int? catalogTypeId, string? searchTerm)
    {
        PageSize = pageSize ?? 0;
        PageIndex = pageIndex ?? 0;
        CatalogBrandId = catalogBrandId;
        CatalogTypeId = catalogTypeId;
        SearchTerm = searchTerm;
    }
}
