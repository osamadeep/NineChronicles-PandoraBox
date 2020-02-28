using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ConsumableItemRecipeSheet : Sheet<int, ConsumableItemRecipeSheet.Row> 
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public List<int> MaterialItemIds { get; private set; }
            public int ResultConsumableItemId { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0], CultureInfo.InvariantCulture);
                MaterialItemIds = new List<int>();
                for (var i = 1; i < 5; i++)
                {
                    if (string.IsNullOrEmpty(fields[i]))
                        break;
                    
                    MaterialItemIds.Add(ParseInt(fields[i]));
                }
                MaterialItemIds.Sort((left, right) => left - right);
                
                ResultConsumableItemId = ParseInt(fields[5]);
            }

            public bool IsMatch(IEnumerable<int> materialItemIds)
            {
                var itemIds = materialItemIds as int[] ?? materialItemIds.ToArray();
                return MaterialItemIds.Count == itemIds.Length &&
                    MaterialItemIds.All(itemIds.Contains);
            }
        }

        public ConsumableItemRecipeSheet() : base(nameof(ConsumableItemRecipeSheet))
        {
        }

        public bool TryGetValue(IEnumerable<int> materialItemIds, out Row row, bool throwException = false)
        {
            foreach (var value in Values.Where(value => value.IsMatch(materialItemIds)))
            {
                row = value;
                return true;
            }

            row = null;
            return false;
        }

        public bool TryGetValue(IEnumerable<MaterialItemSheet.Row> materialItemRows, out Row row, bool throwException = false)
        {
            return TryGetValue(materialItemRows.Select(materialItemRow => materialItemRow.Id), out row, throwException);
        }
    }
}
