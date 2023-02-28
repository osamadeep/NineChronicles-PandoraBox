using Libplanet;
using Libplanet.Blocks;
using Nekoyume.BlockChain;
using Nekoyume.PandoraBox;
using System.Collections;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.UI
{
    public class VersionSystem : SystemWidget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private TextMeshProUGUI nodeText;

        [SerializeField] private TextMeshProUGUI queueCountTxt;
        string pandoraTextVer = "";
        string current9cScanBlock = "";
        public long NodeBlockIndex;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        public TextMeshProUGUI informationText;

        private int _version;
        private long _blockIndex;
        private BlockHash _hash;

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SubscribeBlockIndex).AddTo(gameObject);
            Game.Game.instance.Agent.BlockTipHashSubject.Subscribe(SubscribeBlockHash).AddTo(gameObject);
            //pandoraTextVer = string.Format("PandoraBox v{0}.{1}.{2}",
            //    int.Parse(PandoraBoxMaster.VersionId.Substring(0, 2)),
            //    int.Parse(PandoraBoxMaster.VersionId.Substring(2, 2)),
            //    int.Parse(PandoraBoxMaster.VersionId.Substring(4, 2)));
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            pandoraTextVer = string.Format("PandoraBox v{0}.{2}",
                int.Parse(PandoraMaster.VersionId.Substring(0, 2)),
                int.Parse(PandoraMaster.VersionId.Substring(2, 2)),
                int.Parse(PandoraMaster.VersionId.Substring(4, 2)));
            if (PandoraMaster.VersionId.Length > 6)
                pandoraTextVer += "<color=green>A</color>";
            StartCoroutine(Get9cBlock());
            StartCoroutine(ShowQueueCount());
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
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
            // informationText.text = $"{pandoraTextVer} / #{_blockIndex}({current9cScanBlock})";
        }


        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        IEnumerator ShowQueueCount()
        {
            while (true)
            {
                try
                {
                    queueCountTxt.text = Game.Game.instance.ActionManager.GetQueueCount().ToString();
                }
                catch
                {
                    queueCountTxt.text = "0";
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator Get9cBlock()
        {
            int secToUpdate = 10;

            while (true)
            {
                string url = "https://api.9cscan.com/blocks?limit=1";
                UnityWebRequest www = UnityWebRequest.Get(url);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    current9cScanBlock = "<color=red>X</color>";
                }
                else
                {
                    if (_blockIndex != 0)
                        try
                        {
                            Scan9c scanLatestBlock = JsonUtility.FromJson<Scan9c>(www.downloadHandler.text);
                            int block = scanLatestBlock.before;
                            int differenceBlocks = (int)_blockIndex - block;
                            NodeBlockIndex = block;
                            if (differenceBlocks > -15)
                                if (differenceBlocks > 0)
                                    current9cScanBlock = $"(<color=green>+{Mathf.Abs(differenceBlocks)}</color>)";
                                else
                                    current9cScanBlock = $"(<color=green>{differenceBlocks}</color>)";
                            else
                                current9cScanBlock = $"(<color=red>{differenceBlocks}</color>)";
                            //Debug.LogError(block + "  " +  _blockIndex);

                            try
                            {
                                nodeText.text = "<color=green>" + Game.Game.instance._options.RpcServerHost +
                                                "</color>";
                            }
                            catch
                            {
                            }
                        }
                        catch
                        {
                            current9cScanBlock = "<color=red>X</color>";
                        }
                }

                informationText.text = $"{pandoraTextVer} / #{_blockIndex}{current9cScanBlock}";
                www.Dispose();
                yield return new WaitForSeconds(secToUpdate);
            }
        }
    }

    [System.Serializable]
    public class Scan9c
    {
        public int before;
    }

    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
}