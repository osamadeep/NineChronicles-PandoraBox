using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using BTAI;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types;
using Libplanet.Blockchain;
using Log = Serilog.Log;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneSubscription : ObjectGraphType
    {
        // TODO: 추가적인 값을 줘야 합니다.
        class TipChanged : ObjectGraphType<TipChanged>
        {
            public long Index { get; set; }

            public TipChanged()
            {
                Field<LongGraphType>(nameof(Index));
            }
        }

        private ISubject<TipChanged> _subject = new ReplaySubject<TipChanged>();

        private StandaloneContext StandaloneContext { get; }

        public StandaloneSubscription(StandaloneContext standaloneContext)
        {
            StandaloneContext = standaloneContext;
            AddField(new EventStreamFieldType {
                Name = "tipChanged",
                Type = typeof(TipChanged),
                Resolver = new FuncFieldResolver<TipChanged>(Resolve),
                Subscriber = new EventStreamResolver<TipChanged>(Subscribe),
            });
        }

        public void RegisterTipChangedSubscription()
        {
            if (StandaloneContext.BlockChain is null)
            {
                throw new ArgumentNullException(
                    nameof(StandaloneContext.BlockChain),
                    $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)}" +
                    $" should be set before calling `{nameof(RegisterTipChangedSubscription)}()`.");
            }

            StandaloneContext.BlockChain.TipChanged += (sender, args) =>
            {
                _subject.OnNext(new TipChanged
                {
                    Index = args.Index,
                });
            };
        }

        private TipChanged Resolve(IResolveFieldContext context)
        {
            return context.Source as TipChanged;
        }

        private IObservable<TipChanged> Subscribe(IResolveEventStreamContext context)
        {
            return _subject.AsObservable();
        }
    }
}
