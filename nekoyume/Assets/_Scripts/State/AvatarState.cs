using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Game.Quest;
using Nekoyume.Model;

namespace Nekoyume.State
{
    /// <summary>
    /// Agent가 포함하는 각 Avatar의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AvatarState : State, ICloneable
    {
        public string name;
        public int characterId;
        public int level;
        public long exp;
        public Game.Item.Inventory inventory;
        public int worldStage;
        public DateTimeOffset updatedAt;
        public DateTimeOffset? clearedAt;
        public Address agentAddress;
        public QuestList questList;
        public MailBox mailBox;
        public long BlockIndex;
        public long nextDailyRewardIndex;
        public int actionPoint;
        public CollectionMap stageMap;
        public CollectionMap monsterMap;
        public CollectionMap itemMap;
        public CollectionMap eventMap;

        public AvatarState(Address address, Address agentAddress, long blockIndex, long rewardIndex, string name = null) : base(address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));                
            }
            
            this.name = name ?? "";
            characterId = GameConfig.DefaultAvatarCharacterId;
            level = 1;
            exp = 0;
            inventory = new Game.Item.Inventory();
            worldStage = 1;
            updatedAt = DateTimeOffset.UtcNow;
            this.agentAddress = agentAddress;
            questList = new QuestList();
            mailBox = new MailBox();
            BlockIndex = blockIndex;
            actionPoint = GameConfig.ActionPoint;
            nextDailyRewardIndex = rewardIndex;
            stageMap = new CollectionMap();
            monsterMap = new CollectionMap();
            itemMap = new CollectionMap();
            const QuestEventType createEvent = QuestEventType.Create;
            const QuestEventType levelEvent = QuestEventType.Level;
            eventMap = new CollectionMap
            {
                new KeyValuePair<int, int>((int) createEvent, 1),
                new KeyValuePair<int, int>((int) levelEvent, level),
            };
            UpdateGeneralQuest(new []{createEvent, levelEvent});
            UpdateCompletedQuest();
        }
        
        public AvatarState(AvatarState avatarState) : base(avatarState.address)
        {
            if (avatarState == null)
            {
                throw new ArgumentNullException(nameof(avatarState));
            }
            
            name = avatarState.name;
            characterId = avatarState.characterId;
            level = avatarState.level;
            exp = avatarState.exp;
            inventory = avatarState.inventory;
            worldStage = avatarState.worldStage;
            updatedAt = avatarState.updatedAt;
            clearedAt = avatarState.clearedAt;
            agentAddress = avatarState.agentAddress;
            questList = avatarState.questList;
            mailBox = avatarState.mailBox;
            BlockIndex = avatarState.BlockIndex;
            nextDailyRewardIndex = avatarState.nextDailyRewardIndex;
            actionPoint = avatarState.actionPoint;
            stageMap = avatarState.stageMap;
            monsterMap = avatarState.monsterMap;
            itemMap = avatarState.itemMap;
            eventMap = avatarState.eventMap;
        }

        public AvatarState(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
            name = ((Text) serialized["name"]).Value;
            characterId = (int) ((Integer) serialized["characterId"]).Value;
            level = (int) ((Integer) serialized["level"]).Value;
            exp = (long) ((Integer) serialized["exp"]).Value;
            inventory = new Game.Item.Inventory((Bencodex.Types.List) serialized["inventory"]);
            worldStage = (int) ((Integer) serialized["worldStage"]).Value;
            updatedAt = serialized["updatedAt"].ToDateTimeOffset();
            clearedAt = serialized["clearedAt"].ToNullableDateTimeOffset();
            agentAddress = new Address(((Binary) serialized["agentAddress"]).Value);
            questList = new QuestList((Bencodex.Types.List) serialized["questList"]);
            mailBox = new MailBox((Bencodex.Types.List) serialized["mailBox"]);
            BlockIndex = (long) ((Integer) serialized["blockIndex"]).Value;
            nextDailyRewardIndex = (long) ((Integer) serialized["nextDailyRewardIndex"]).Value;
            actionPoint = (int) ((Integer) serialized["actionPoint"]).Value;
            stageMap = new CollectionMap((Bencodex.Types.Dictionary) serialized["stageMap"]);
            serialized.TryGetValue((Text) "monsterMap", out var value2);
            monsterMap = value2 is null ? new CollectionMap() : new CollectionMap((Bencodex.Types.Dictionary) value2);
            itemMap = new CollectionMap((Bencodex.Types.Dictionary) serialized["itemMap"]);
            eventMap = new CollectionMap((Bencodex.Types.Dictionary) serialized["eventMap"]);
        }

        public void Update(Simulator simulator)
        {
            var player = simulator.Player;
            characterId = player.RowData.Id;
            level = player.Level;
            exp = player.Exp.Current;
            inventory = player.Inventory;
            worldStage = player.worldStage;
            foreach (var pair in player.monsterMap)
            {
                monsterMap.Add(pair);
            }
            foreach (var pair in player.eventMap)
            {
                eventMap.Add(pair);
            }
            if (simulator.Result == BattleLog.Result.Win)
            {
                stageMap.Add(new KeyValuePair<int, int>(simulator.WorldStage, 1));
            }
            foreach (var pair in simulator.ItemMap)
            {
                itemMap.Add(pair);
            }

            UpdateStageQuest(simulator.rewards);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Update(Game.Mail.Mail mail)
        {
            mailBox.Add(mail);
        }

        public void UpdateGeneralQuest(IEnumerable<QuestEventType> types)
        {
            eventMap = questList.UpdateGeneralQuest(types, eventMap);
        }

        private void UpdateCompletedQuest()
        {
            eventMap = questList.UpdateCompletedQuest(eventMap);
        }

        private void UpdateStageQuest(IEnumerable<ItemBase> items)
        {
            questList.UpdateStageQuest(stageMap);
            questList.UpdateMonsterQuest(monsterMap);
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateGeneralQuest(new []{QuestEventType.Level, QuestEventType.Die});
            UpdateCompletedQuest();
        }

        public void UpdateCombinationQuest(ItemUsable itemUsable)
        {
            questList.UpdateCombinationQuest(itemUsable);
            questList.UpdateItemTypeCollectQuest(new []{itemUsable});
            var type = itemUsable is Equipment ? QuestEventType.Equipment : QuestEventType.Consumable;
            eventMap.Add(new KeyValuePair<int, int>((int) type, 1));
            UpdateGeneralQuest(new[] {type});
            UpdateCompletedQuest();
        }
        public void UpdateItemEnhancementQuest(Equipment equipment)
        {
            questList.UpdateItemEnhancementQuest(equipment);
            var type = QuestEventType.Enhancement;
            eventMap.Add(new KeyValuePair<int, int>((int) type, 1));
            UpdateGeneralQuest(new[] {type});
            UpdateCompletedQuest();
        }

        public void UpdateQuestFromAddItem(ItemUsable itemUsable, bool canceled)
        {
            var pair = inventory.AddItem(itemUsable);
            if (!itemMap.ContainsKey(itemUsable.Data.Id))
            {
                itemMap.Add(pair);
            }

            if (!canceled)
            {
                questList.UpdateItemGradeQuest(itemUsable);
                questList.UpdateItemTypeCollectQuest(new []{itemUsable});
            }
            UpdateCompletedQuest();

        }

        public void UpdateFromQuestReward(Quest quest, IRandom random)
        {
            var items = new List<ItemBase>();
            foreach (var pair in quest.Reward.ItemMap)
            {
                var row = Game.Game.instance.TableSheets.ItemSheet.Values.First(itemRow => itemRow.Id == pair.Key);
                var item = ItemFactory.Create(row, random.GenerateRandomGuid());
                var map = inventory.AddItem(item, pair.Value);
                itemMap.Add(map);
                items.Add(item);
                if (item is ItemUsable itemUsable)
                {
                    questList.UpdateItemGradeQuest(itemUsable);
                }

            }
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateCompletedQuest();

        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "name"] = (Text) name,
                [(Text) "characterId"] = (Integer) characterId,
                [(Text) "level"] = (Integer) level,
                [(Text) "exp"] = (Integer) exp,
                [(Text) "inventory"] = inventory.Serialize(),
                [(Text) "worldStage"] = (Integer) worldStage,
                [(Text) "updatedAt"] = updatedAt.Serialize(),
                [(Text) "clearedAt"] = clearedAt.Serialize(),
                [(Text) "agentAddress"] = agentAddress.Serialize(),
                [(Text) "questList"] = questList.Serialize(),
                [(Text) "mailBox"] = mailBox.Serialize(),
                [(Text) "blockIndex"] = (Integer) BlockIndex,
                [(Text) "nextDailyRewardIndex"] = (Integer) nextDailyRewardIndex,
                [(Text) "actionPoint"] = (Integer) actionPoint,
                [(Text) "stageMap"] = stageMap.Serialize(),
                [(Text) "monsterMap"] = monsterMap.Serialize(),
                [(Text) "itemMap"] = itemMap.Serialize(),
                [(Text) "eventMap"] = eventMap.Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

        public void UpdateQuestFromQuestReward(Dictionary<int, int> rewardItemMap, IRandom random)
        {
        }
    }
}
