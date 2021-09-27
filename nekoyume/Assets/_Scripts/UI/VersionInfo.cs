using Libplanet;
using Libplanet.Blocks;
using PandoraBox;
using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class VersionInfo : SystemInfoWidget
    {
        public TextMeshProUGUI InformationText;
        private int _version;
        private long _blockIndex;
        private BlockHash _hash;

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SubscribeBlockIndex).AddTo(gameObject);
            Game.Game.instance.Agent.BlockTipHashSubject.Subscribe(SubscribeBlockHash).AddTo(gameObject);
        }

        public void SetVersion(int version)
        {
            _version = version;
            UpdateText();
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateText();
        }

        private void SubscribeBlockHash(BlockHash hash)
        {
            _hash = hash;
            UpdateText();
            
        }

        private void UpdateText()
        {
            string textVer = string.Format("v{0}.{1}.{2}",
                            int.Parse(PandoraBoxMaster.Instance.Settings.VersionId.Substring(0, 2)),
                            int.Parse(PandoraBoxMaster.Instance.Settings.VersionId.Substring(2, 2)),
                            int.Parse(PandoraBoxMaster.Instance.Settings.VersionId.Substring(4, 2)));
            const string format = "PandoraBox {0} / #{1} / Hash: {2}";
            var hash = _hash.ToString();
            var text = string.Format(
                format,
                textVer,
                _blockIndex,
                hash.Length >= 4 ? hash.Substring(0, 4) : "...");
            InformationText.text = text;
        }
    }
}
