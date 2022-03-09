using Libplanet;
using Libplanet.Blocks;
using PandoraBox;
using System.Collections;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.UI
{
    public class VersionSystem : SystemWidget
    {
        public TextMeshProUGUI informationText;
        private int _version;
        private long _blockIndex;
        private BlockHash _hash;

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        string pandoraTextVer = "";
        string current9cScanBlock = "";
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SubscribeBlockIndex).AddTo(gameObject);
            Game.Game.instance.Agent.BlockTipHashSubject.Subscribe(SubscribeBlockHash).AddTo(gameObject);
            //pandoraTextVer = string.Format("PandoraBox v{0}.{1}.{2}",
            //    int.Parse(PandoraBoxMaster.VersionId.Substring(0, 2)),
            //    int.Parse(PandoraBoxMaster.VersionId.Substring(2, 2)),
            //    int.Parse(PandoraBoxMaster.VersionId.Substring(4, 2)));
            pandoraTextVer = string.Format("PandoraBox v{0}.{2}",
                            int.Parse(PandoraBoxMaster.VersionId.Substring(0, 2)),
                            int.Parse(PandoraBoxMaster.VersionId.Substring(2, 2)),
                            int.Parse(PandoraBoxMaster.VersionId.Substring(4, 2)));
            if (PandoraBoxMaster.VersionId.Length > 6)
                pandoraTextVer += "<color=green>A</color>";
            StartCoroutine(Get9cBlock());
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

        IEnumerator Get9cBlock()
        {
            int secToUpdate = 6;
            while (true)
            {
                string url = "https://api.9cscan.com/blocks?limit=1";
                UnityWebRequest www = UnityWebRequest.Get(url);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    current9cScanBlock = "?";
                }
                else
                {
                    if (_blockIndex != 0)
                        try
                        {
                            Scan9c scanLatestBlock = JsonUtility.FromJson<Scan9c>(www.downloadHandler.text);
                            int block = scanLatestBlock.before;
                            int differenceBlocks = (int)_blockIndex - block;
                            if (differenceBlocks > -15)
                                if (differenceBlocks > 0)
                                    current9cScanBlock = $"(<color=green>+{Mathf.Abs(differenceBlocks)}</color>)";
                                else
                                    current9cScanBlock = $"(<color=green>{differenceBlocks}</color>)";
                            else
                                current9cScanBlock = $"(<color=red>!</color>)";
                            //Debug.LogError(block + "  " +  _blockIndex);
                        }
                        catch { current9cScanBlock = "(?)"; }
                }

                informationText.text = $"{pandoraTextVer} / #{_blockIndex}{current9cScanBlock}";
                yield return new WaitForSeconds(secToUpdate);
            }
        }
    }

    [System.Serializable]
    public class Scan9c
    {
        public int before;
    }
}
