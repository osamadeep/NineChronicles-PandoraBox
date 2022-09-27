using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.Arena;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Event;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State
{
    using UniRx;

    public static partial class RxProps
    {
        private static IAgent _agent;
        private static States _states;
        private static TableSheets _tableSheets;

        private static readonly List<IDisposable> _disposables = new();

        private static Address? _currentAvatarAddr;

        public static void Start(IAgent agent, States states, TableSheets tableSheets)
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (states is null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (tableSheets is null)
            {
                throw new ArgumentNullException(nameof(tableSheets));
            }

            Debug.Log($"{nameof(RxProps)} start");
            _agent = agent;
            _states = states;
            _tableSheets = tableSheets;

            _disposables.DisposeAllAndClear();
            _agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(OnBlockIndex)
                .AddTo(_disposables);

            ReactiveAvatarState.Address
                .Subscribe(OnAvatarChanged)
                .AddTo(_disposables);

            StartArena();
            StartEvent();
        }

        public static void SetCurrentAvatarAddress(Address avatarAddress)
        {
            _currentAvatarAddr = avatarAddress;
        }

        public static void Stop()
        {
            Debug.Log($"{nameof(RxProps)} stop");
            _disposables.DisposeAllAndClear();
        }

        public static async UniTask<(
            AvatarState selectedAvatarState,
            (ArenaInformation current, ArenaInformation next) arenaInfoTuple,
            EventDungeonInfo eventDungeonInfo)> SelectAvatarAsync(int avatarIndexToSelect) =>
            await UniTask.WhenAll(
                States.Instance.SelectAvatarAsync(avatarIndexToSelect),
                ArenaInfoTuple.UpdateAsync(),
                EventDungeonInfo.UpdateAsync());

        private static void OnBlockIndex(long blockIndex)
        {
            OnBlockIndexArena(blockIndex);
            OnBlockIndexEvent(blockIndex);
        }

        private static void OnAvatarChanged(Address avatarAddr)
        {
            if (_currentAvatarAddr.HasValue &&
                _currentAvatarAddr.Value.Equals(avatarAddr))
            {
                return;
            }

            _currentAvatarAddr = avatarAddr;
            OnAvatarChangedArena();
            OnAvatarChangedEvent();
        }
    }
}
