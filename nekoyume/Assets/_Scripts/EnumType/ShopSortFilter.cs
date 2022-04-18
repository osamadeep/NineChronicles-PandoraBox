using System.Collections.Generic;

namespace Nekoyume.EnumType
{
    public enum ShopSortFilter
    {
        Class = 0,
        CP = 1,
        Price = 2,
        Time = 3,
        Level = 4,
    }

    public static class ShopSortFilterExtension
    {
        public static IEnumerable<ShopSortFilter> ShopSortFilters
        {
            get
            {
                return new[]
                {
                    ShopSortFilter.Class,
                    ShopSortFilter.CP,
                    ShopSortFilter.Price,
                    ShopSortFilter.Time,
                    ShopSortFilter.Level,
                };
            }
        }
    }
}