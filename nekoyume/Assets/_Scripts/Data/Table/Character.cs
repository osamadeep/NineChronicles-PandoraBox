using System;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Character : Row
    {
        public int id = 0;
        public string characterName = "";
        public string characterInfo = "";
        public int characterResource = 0;
        public int bookIndex = 0;
        public string size = "s";
        public Elemental.ElementalType elemental;
        public bool isBoss = false;
        public int maxLevel = 0;
        public int hp = 0;
        public int damage = 0;
        public int defense = 0;
        public decimal luck;
        public int lvHp = 0;
        public int lvDamage = 0;
        public int lvDefense = 0;
        public decimal lvLuck = 0.1m;
        public string skill0 = "";
        public string skill1 = "";
        public string skill2 = "";
        public string skill3 = "";
        public float attackRange = 1.0f;
        public float runSpeed = 1.0f;

        public class Stats
        {
            public int HP;
            public int Damage;
            public int Defense;
            public decimal Luck;
        }

        public Stats GetStats(int level)
        {
            var statsHp = hp;
            var dmg = damage;
            var def = defense;
            var lck = luck;
            if (level > 1)
            {
                var multiplier = level - 1;
                statsHp += lvHp * multiplier;
                dmg += lvDamage * multiplier;
                def += lvDefense * multiplier;
                lck += lvLuck * multiplier;
            }
            return new Stats
            {
                HP = statsHp,
                Damage = dmg,
                Defense = def,
                Luck = lck,
            };
        }
    }
}
