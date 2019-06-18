using System;
using Nekoyume.Game.Skill;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Necklace : Equipment
    {
        public Necklace(Data.Table.Item data, SkillBase skillBase = null) : base(data, skillBase)
        {
        }

    }
}
