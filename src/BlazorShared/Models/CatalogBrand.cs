using System.Globalization;
using BlazorShared.Attributes;

namespace BlazorShared.Models;

[Endpoint(Name = "catalog-brands")]
 
public class CatalogBrand : LookupData
{
        public CatalogBrand()
        {
        }
    
        public CatalogBrand(string name)
        {
            Name = name;
        }
    
        public override string ToString()
        {
            return Name;
    }
}
