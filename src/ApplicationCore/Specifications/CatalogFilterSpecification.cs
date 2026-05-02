using Ardalis.Specification;
using Microsoft.eShopWeb.ApplicationCore.Entities;

namespace Microsoft.eShopWeb.ApplicationCore.Specifications;

public class CatalogFilterSpecification : Specification<CatalogItem>
{
    public CatalogFilterSpecification(int? brandId, int? typeId, string? searchTerm)
    {
        var normalizedSearchTerm = searchTerm?.Trim().ToLower();

        Query.Where(i => (!brandId.HasValue || i.CatalogBrandId == brandId) &&
            (!typeId.HasValue || i.CatalogTypeId == typeId) &&
            (string.IsNullOrEmpty(normalizedSearchTerm) ||
                i.Name.ToLower().Contains(normalizedSearchTerm) ||
                i.Description.ToLower().Contains(normalizedSearchTerm)));
    }
}
