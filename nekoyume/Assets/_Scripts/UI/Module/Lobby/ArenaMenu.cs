﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class ArenaMenu : MainMenu
    {
        [SerializeField] private GameObject _ticketCountGO;

        [SerializeField] private TextMeshProUGUI _ticketCount;

        [SerializeField] private TextMeshProUGUI _ticketResetTime;

        [SerializeField] private TextMeshProUGUI _seasonText;

        [SerializeField] private GameObject _seasonGameObject;

        [SerializeField] private GameObject _championshipGameObject;

        [SerializeField] private GameObject grandFinaleGameObject;

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            var agent = Game.Game.instance.Agent;
            UpdateArenaSeasonTitle(agent.BlockIndex);
            agent.BlockIndexSubject
                .Subscribe(UpdateArenaSeasonTitle)
                .AddTo(_disposables);
            RxProps.ArenaTicketsProgress
                .ObserveOnMainThread()
                .Subscribe(UpdateTicket)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateTicket(RxProps.TicketProgress ticketProgress)
        {
            _ticketCountGO.SetActive(ticketProgress.currentTickets > 0);
            _ticketCount.text = ticketProgress.currentTickets
                .ToString(CultureInfo.InvariantCulture);
            _ticketResetTime.text = ticketProgress.remainTimespanToReset;
        }

        private void UpdateArenaSeasonTitle(long blockIndex)
        {
            if (TableSheets.Instance.GrandFinaleScheduleSheet?.GetRowByBlockIndex(blockIndex)
                is not null)
            {
                grandFinaleGameObject.SetActive(true);
                _seasonGameObject.SetActive(false);
                _championshipGameObject.SetActive(false);
                return;
            }

            ArenaSheet.RoundData currentRoundData;
            try
            {
                currentRoundData =
                    TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            }
            catch (Exception)
            {
                _seasonGameObject.SetActive(false);
                _championshipGameObject.SetActive(false);
                grandFinaleGameObject.SetActive(false);
                return;
            }

            switch (currentRoundData.ArenaType)
            {
                case ArenaType.OffSeason:
                    _seasonGameObject.SetActive(false);
                    _championshipGameObject.SetActive(false);
                    grandFinaleGameObject.SetActive(false);
                    break;
                case ArenaType.Season:
                    _seasonText.text = TableSheets.Instance.ArenaSheet
                        .GetSeasonNumber(
                            blockIndex,
                            currentRoundData.Round)
                        .ToString();
                    _seasonGameObject.SetActive(true);
                    _championshipGameObject.SetActive(false);
                    grandFinaleGameObject.SetActive(false);
                    break;
                case ArenaType.Championship:
                    _seasonGameObject.SetActive(false);
                    _championshipGameObject.SetActive(true);
                    grandFinaleGameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}