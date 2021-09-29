using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI
{
    public static class SkillSheetExtension
    {
        private static readonly Dictionary<int, List<BuffSheet.Row>> SkillBuffs =
            new Dictionary<int, List<BuffSheet.Row>>();

        public static string GetLocalizedName(this SkillSheet.Row row)
        {
            if (row is null)
            {
                throw new System.ArgumentNullException(nameof(row));
            }

            return L10nManager.Localize($"SKILL_NAME_{row.Id}");
        }
    }
}
