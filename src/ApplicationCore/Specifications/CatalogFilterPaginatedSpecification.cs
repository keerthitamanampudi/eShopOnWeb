using Ardalis.Specification;
using Microsoft.eShopWeb.ApplicationCore.Entities;

namespace Microsoft.eShopWeb.ApplicationCore.Specifications;

public class CatalogFilterPaginatedSpecification : Specification<CatalogItem>
{
    public CatalogFilterPaginatedSpecification(int skip, int take, int? brandId, int? typeId, string? searchTerm)
        : base()
    {
        if (take == 0)
        {
            take = int.MaxValue;
        }

        var normalizedSearchTerm = searchTerm?.Trim().ToLower();

        Query
            .Where(i => (!brandId.HasValue || i.CatalogBrandId == brandId) &&
            (!typeId.HasValue || i.CatalogTypeId == typeId) &&
            (string.IsNullOrEmpty(normalizedSearchTerm) ||
                i.Name.ToLower().Contains(normalizedSearchTerm) ||
                i.Description.ToLower().Contains(normalizedSearchTerm)))
            .Skip(skip).Take(take);
    }
}
