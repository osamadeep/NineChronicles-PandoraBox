using System;
using System.Collections.Generic;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class PlayerFactory : MonoBehaviour
    {
        public static GameObject Create(AvatarState avatarState)
        {
            if (avatarState is null)
            {
                throw new ArgumentNullException(nameof(avatarState));
            }

            var tableSheets = Game.instance.TableSheets;
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            Player plr = new Player(avatarState, tableSheets.CharacterSheet, tableSheets.CharacterLevelSheet, tableSheets.EquipmentItemSetEffectSheet);
            var objectPool = Game.instance.Stage.objectPool;
            var player = objectPool.Get<Character.Player>();
            player.avatarAddress = avatarState.address.ToString();
            if (!player)
            {
                throw new NotFoundComponentException<Character.Player>();
            }

            player.Set(plr, true);
            return player.gameObject;
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

            //return Create(new Player(avatarState,
            //                                tableSheets.CharacterSheet,
            //                                tableSheets.CharacterLevelSheet,
            //                                tableSheets.EquipmentItemSetEffectSheet));
        }

        public static GameObject Create(Player model = null)
        {
            if (model is null)
            {
                var tableSheets = Game.instance.TableSheets;
                model = new Player(
                    1,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet);
            }

            var objectPool = Game.instance.Stage.objectPool;
            var player = objectPool.Get<Character.Player>();
            if (!player)
            {
                throw new NotFoundComponentException<Character.Player>();
            }

            player.Set(model, true);
            return player.gameObject;
        }

        public static GameObject Create(
            AvatarState avatarState,
            IEnumerable<Costume> costumes,
            Armor armor,
            Weapon weapon)
        {
            if (avatarState is null)
            {
                throw new ArgumentNullException(nameof(avatarState));
            }

            var tableSheets = Game.instance.TableSheets;
            var model = new Player(avatarState,
                tableSheets.CharacterSheet,
                tableSheets.CharacterLevelSheet,
                tableSheets.EquipmentItemSetEffectSheet);
            var objectPool = Game.instance.Stage.objectPool;

            var player = objectPool.Get<Character.Player>();
            if (!player)
            {
                throw new NotFoundComponentException<Character.Player>();
            }

            player.Set(model, costumes, armor, weapon, true);
            return player.gameObject;
        }
    }
}
