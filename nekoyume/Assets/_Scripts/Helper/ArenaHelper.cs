using System;
using System.Diagnostics.SymbolStore;
using System.Threading.Tasks;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume
{
    public static class ArenaHelper
    {
        public static bool TryGetThisWeekAddress(out Address weeklyArenaAddress)
        {
            return TryGetThisWeekAddress(Game.Game.instance.Agent.BlockIndex, out weeklyArenaAddress);
        }

        public static bool TryGetThisWeekAddress(long blockIndex, out Address weeklyArenaAddress)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = (int) blockIndex / gameConfigState.WeeklyArenaInterval;
            if (index < 0)
            {
                return false;
            }

            weeklyArenaAddress = WeeklyArenaState.DeriveAddress(index);
            return true;
        }

        public static bool TryGetThisWeekState(out WeeklyArenaState weeklyArenaState)
        {
            return TryGetThisWeekState(Game.Game.instance.Agent.BlockIndex, out weeklyArenaState);
        }

        public static bool TryGetThisWeekState(long blockIndex, out WeeklyArenaState weeklyArenaState)
        {
            weeklyArenaState = null;
            if (blockIndex != Game.Game.instance.Agent.BlockIndex)
            {
                Debug.LogError(
                    $"[{nameof(ArenaHelper)}.{nameof(TryGetThisWeekState)}] `{nameof(blockIndex)}`({blockIndex}) not equals with `Game.Game.instance.Agent.BlockIndex`({Game.Game.instance.Agent.BlockIndex})");
                return false;
            }

            if (!TryGetThisWeekAddress(blockIndex, out var address))
                return false;

            weeklyArenaState = new WeeklyArenaState(Game.Game.instance.Agent.GetState(address));
            return true;
        }

        public static async Task<WeeklyArenaState> GetThisWeekStateAsync(long blockIndex)
        {
            if (blockIndex != Game.Game.instance.Agent.BlockIndex)
            {
                Debug.LogError(
                    $"[{nameof(ArenaHelper)}.{nameof(TryGetThisWeekState)}] `{nameof(blockIndex)}`({blockIndex}) not equals with `Game.Game.instance.Agent.BlockIndex`({Game.Game.instance.Agent.BlockIndex})");
                return null;
            }

            if (!TryGetThisWeekAddress(blockIndex, out var address))
                return null;

            var state = await Game.Game.instance.Agent.GetStateAsync(address);
            return state is null ? null : new WeeklyArenaState(state);
        }

        public static Address GetPrevWeekAddress()
        {
            return GetPrevWeekAddress(Game.Game.instance.Agent.BlockIndex);
        }

        public static Address GetPrevWeekAddress(long thisWeekBlockIndex)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = Math.Max((int) thisWeekBlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            return WeeklyArenaState.DeriveAddress(index);
        }

        public static bool TryGetThisWeekStateAndArenaInfo(Address avatarAddress, out WeeklyArenaState weeklyArenaState,
            out ArenaInfo arenaInfo)
        {
            return TryGetThisWeekStateAndArenaInfo(Game.Game.instance.Agent.BlockIndex, avatarAddress,
                out weeklyArenaState, out arenaInfo);
        }

        public static bool TryGetThisWeekStateAndArenaInfo(long blockIndex, Address avatarAddress,
            out WeeklyArenaState weeklyArenaState,
            out ArenaInfo arenaInfo)
        {
            arenaInfo = null;
            return TryGetThisWeekState(blockIndex, out weeklyArenaState) &&
                   weeklyArenaState.TryGetValue(avatarAddress, out arenaInfo);
        }

        public static Address GetNextWeekAddress(long blockIndex)
        {
            var gameConfigState = States.Instance.GameConfigState;
            var index = (int) blockIndex / gameConfigState.WeeklyArenaInterval;
            index++;
            return WeeklyArenaState.DeriveAddress(index);
        }
    }
}
