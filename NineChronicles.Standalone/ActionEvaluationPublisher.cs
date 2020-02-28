﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Libplanet.Blockchain;
using MagicOnion.Client;
using Microsoft.Extensions.Hosting;
using Nekoyume.Action;
using Nekoyume.Shared.Hubs;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class ActionEvaluationPublisher : BackgroundService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly BlockChain<NineChroniclesActionType> _blockChain;
        
        public ActionEvaluationPublisher(
            BlockChain<NineChroniclesActionType> blockChain,
            string host,
            int port
        )
        {
            _blockChain = blockChain;
            _host = host;
            _port = port;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1000, stoppingToken);
            var client = StreamingHubClient.Connect<IActionEvaluationHub, IActionEvaluationHubReceiver>(
                new Channel(_host, _port, ChannelCredentials.Insecure),
                null
            );
            await client.JoinAsync();

            _blockChain.TipChanged += async (o, ev) =>
            {
                await client.UpdateTipAsync(ev.Index);
            };
            var renderer = new ActionRenderer(ActionBase.RenderSubject, ActionBase.UnrenderSubject);
            renderer.EveryRender<ActionBase>().Subscribe(
                async ev =>
                {
                    var formatter = new BinaryFormatter();
                    using var s = new MemoryStream();
                    formatter.Serialize(s, ev);
                    await client.BroadcastAsync(s.ToArray());
                },
                stoppingToken
            );
        }
    }
}
