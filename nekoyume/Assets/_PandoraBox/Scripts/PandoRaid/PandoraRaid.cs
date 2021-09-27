using System;
using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Tween;
using static Nekoyume.Game.Event;
using UnityEngine.UI;

namespace PandoraBox
{
    public class PandoraRaid : MonoBehaviour
    {
        [SerializeField]
        TMP_InputField StageIDText;

        [SerializeField]
        GameObject RotateShape;

        [SerializeField]
        Button RaidButton = null;

        [SerializeField]
        TextMeshProUGUI RaidButtonText = null;

        [SerializeField]
        TextMeshProUGUI CurrentTriesText = null;

        private Player _player;

        bool isBusy = false;

        private void Start()
        {
            GetComponent<AnchoredPositionXTweener>().PlayReverse();
            StartCoroutine(ShowCurrentTries());
        }

        // Start is called before the first frame update
        void OnEnable()
        {
            //GetComponent<AnchoredPositionXTweener>().PlayTween();
            //StartCoroutine(kill());
        }

        IEnumerator ShowCurrentTries()
        {
            //yield return new WaitForSeconds(2f);
            //while (true)
            //{
            //    if (Widget.Find<Menu>().IsActive())
            //        break;
            //}
            RectTransform rt = GetComponent<RectTransform>();
            bool isMenu= false;
            while (true)
            {
                
                yield return new WaitForSeconds(0.5f);
                try
                {
                    CurrentTriesText.text = ((int)(States.Instance.CurrentAvatarState.actionPoint / 5)).ToString();
                    if (Widget.Find<Menu>().IsActive() && !isMenu)
                    {
                        //rt.anchoredPosition = new Vector3(10, -250);
                        GetComponent<AnchoredPositionXTweener>().PlayTween();
                        isMenu = true;
                    }
                    else if (!Widget.Find<Menu>().IsActive() && isMenu)
                    {
                        //rt.anchoredPosition = new Vector3(-400, -250);
                        GetComponent<AnchoredPositionXTweener>().PlayReverse();
                        isMenu = false;
                    }
                }
                catch
                {

                }
            }
        }

        public void StartRaid()
        {
            if (isBusy)
            {
                isBusy = false;
                StartCoroutine(Cooldown());
            }
            else
            {
                int tries = ((int)(States.Instance.CurrentAvatarState.actionPoint / 5));
                if (tries <= 0)
                {
                    OneLinePopup.Push(MailType.System, "<color=green>Pandora Box</color>: You have "+ "<color=red><b>0</b></color> Action Points!");
                    return;
                }
            
                StartCoroutine(Raid(tries));
            }
        }

        IEnumerator Raid(int count)
        {
            isBusy = true;
            RotateShape.SetActive(true);
            StageIDText.interactable = false;
            RaidButton.GetComponent<Image>().color = Color.red;
            RaidButtonText.text = "Cancel!";

            yield return new WaitForSeconds(2f);
            _player = Game.instance.Stage.GetPlayer();
            yield return new WaitForSeconds(1f);
            var stage = Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(i => i.Id == int.Parse(StageIDText.text));

            int worldID = 0;
            if (stage.Id < 51)
                worldID = 1;
            else if (stage.Id > 50 && stage.Id < 101)
                worldID = 2;
            else if (stage.Id > 100 && stage.Id < 151)
                worldID = 3;
            else if (stage.Id > 150 && stage.Id < 201)
                worldID = 4;
            else if (stage.Id > 200 && stage.Id < 251)
                worldID = 5;

            int _requiredCost = stage.CostAP;
            for (int i = 0; i < count; i++)
            {
                if (!isBusy)
                    yield break;

                Game.instance.ActionManager
                    .HackAndSlash(_player, worldID, stage.Id)
                    .Subscribe(
                        _ =>
                        {
                            //LocalLayerModifier.ModifyAvatarActionPoint(
                            //    States.Instance.CurrentAvatarState.address, _requiredCost);
                        }, e => ActionRenderHandler.BackToMain(false, e))
                    ;
                //LocalLayerModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address, -5);

                OneLinePopup.Push(MailType.System, "<color=green>Pandora Box</color>: Raiding Stage <color=red>" + stage.Id
                    +"</color> <color=green>" + (i + 1) + "</color>/" + count + "...");
                yield return new WaitForSeconds(2f);

            }
            StartCoroutine(Cooldown());
            //OneLinePopup.Push(MailType.System, "<color=green>Pandora Box</color>: <color=green>" + count + "</color> Fights Sent, Please Hold ...");
        }

        public void Show(bool isReversed)
        {
            if (!isReversed)
            {
                //transform.localPosition = new Vector3(-305, -250);
                GetComponent<AnchoredPositionXTweener>().PlayReverse();
            }
            else
            {
                GetComponent<AnchoredPositionXTweener>().PlayTween();
            }
        }

        IEnumerator Cooldown()
        {

            int i = PlayerPrefs.GetInt("_PandoraBox_PVE_RaidCooldown", 150); ;
            RaidButton.GetComponent<Image>().color = Color.white;
            RaidButton.interactable = false;
            RotateShape.SetActive(false);
            while (i > 0)
            {
                RaidButtonText.text = "" + i--;
                yield return new WaitForSeconds(1f);
            }
            RaidButtonText.text = "RAID!";
            RaidButton.interactable = true;
            StageIDText.interactable = true;
            isBusy = false;
        }
    }
}
