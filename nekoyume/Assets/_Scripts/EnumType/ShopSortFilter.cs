using System.Collections.Generic;

namespace Nekoyume.EnumType
{
    public enum ShopSortFilter
    {
        CP = 0,
        Price = 1,
        Class = 2,
        Crystal = 3,
        Time = 4,
        Level = 5,
        PandoraScore = 6,
    }

    public static class ShopSortFilterExtension
    {
        public static IEnumerable<ShopSortFilter> ShopSortFilters
        {
            get
            {
                return new[]
                {
                    ShopSortFilter.CP,
                    ShopSortFilter.Price,
                    ShopSortFilter.Class,
                    ShopSortFilter.Crystal,
                    ShopSortFilter.Time,
                    ShopSortFilter.Level,
                    ShopSortFilter.PandoraScore,
                };
            }
        }
    }
}
