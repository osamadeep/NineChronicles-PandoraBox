using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Model.Mail;

namespace Nekoyume.PandoraBox
{
    using Nekoyume.UI;
    using Nekoyume.UI.Scroller;
    using Nekoyume.UI.Tween;
    using UniRx;

    public class PandoraRaid : MonoBehaviour
    {
        [SerializeField]
        TMP_InputField StageIDText;

        [SerializeField]
        TMP_InputField CurrentTriesManual;

        [SerializeField]
        GameObject RotateShape;

        [SerializeField]
        Button RaidButton = null;

        [SerializeField]
        TextMeshProUGUI RaidButtonText = null;

        [SerializeField]
        TextMeshProUGUI CurrentTriesText = null;

        private Game.Character.Player _player;

        bool isBusy = false;

        private void Start()
        {
            GetComponent<AnchoredPositionSingleTweener>().PlayReverse();
            CurrentTriesManual.gameObject.SetActive(true);
            StartCoroutine(ShowCurrentTries());
        }

        // Start is called before the first frame update
        void OnEnable()
        {
            GetComponent<AnchoredPositionSingleTweener>().PlayTween();
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
                        GetComponent<AnchoredPositionSingleTweener>().PlayTween();
                        isMenu = true;
                    }
                    else if (!Widget.Find<Menu>().IsActive() && isMenu)
                    {
                        //rt.anchoredPosition = new Vector3(-400, -250);
                        GetComponent<AnchoredPositionSingleTweener>().PlayReverse();
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
                States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(out var clearedStage);
                if (int.Parse(StageIDText.text) > clearedStage +1)
                {
                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: You Didnt Open " + "<color=red><b>"+ int.Parse(StageIDText.text) + "</b></color> yet!"
                        , NotificationCell.NotificationType.Information);
                    return;
                }
                int tries = ((int)(States.Instance.CurrentAvatarState.actionPoint / 5));

                if (PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
                {
                    StartCoroutine(Raid(int.Parse(CurrentTriesManual.text)));
                }
                else
                {
                    if (tries <= 0)
                    {
                        OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: You have " + "<color=red><b>0</b></color> Action Points!"
                        , NotificationCell.NotificationType.Information);
                        return;
                    }
                    StartCoroutine(Raid(int.Parse(CurrentTriesManual.text)));
                }
            }
        }

        IEnumerator Raid(int count)
        {

            isBusy = true;
            RotateShape.SetActive(true);
            StageIDText.interactable = false;
            RaidButton.GetComponent<Image>().color = Color.red;
            RaidButtonText.text = "Cancel!";
            float AllowedCooldown = 3f; //save it to settings

            //yield return new WaitForSeconds(AllowedCooldown);
            yield return new WaitForSeconds(AllowedCooldown);
            _player = Game.Game.instance.Stage.GetPlayer();
            var stage = Game.Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(i => i.Id == int.Parse(StageIDText.text));

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


            //bool RaidMethodIsSweep = Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_PVE_RaidMethodIsSweep", 0));
            bool RaidMethodIsSweep = PandoraBoxMaster.Instance.Settings.RaidMethodIsSweep;

            if (!isBusy)
                yield break;

            if (!RaidMethodIsSweep)
            {
                LocalLayerModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address, -_requiredCost);
                ActionRenderHandler.Instance.Pending = true;
                Game.Game.instance.ActionManager.HackAndSlash(_player, worldID, stage.Id, count).Subscribe();
                //Game.instance.ActionManager.HackAndSlash(_player, worldID, stage.Id, count).Subscribe();
                OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Raiding Stage <color=red>" + stage.Id
                + "</color> (<color=green>" + count + "</color>) times Completed!", NotificationCell.NotificationType.Information);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (!isBusy)
                        yield break;
                    RaidButtonText.text = $"(<color=green>{count- (i+1)}</color>)Cancel!";
                    LocalLayerModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,-_requiredCost);
                    ActionRenderHandler.Instance.Pending = true;
                    Game.Game.instance.ActionManager.HackAndSlash(_player, worldID, stage.Id, 1).Subscribe();

                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Raiding Stage <color=red>" + stage.Id
                        + "</color> <color=green>" + (i + 1) + "</color>/" + count + "...", NotificationCell.NotificationType.Information);
                    yield return new WaitForSeconds(AllowedCooldown);

                }
            }

            StartCoroutine(Cooldown());
            //OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: <color=green>" + count + "</color> Fights Sent, Please Hold ...");
        }

        public void Show(bool isReversed)
        {
            if (!isReversed)
            {
                GetComponent<AnchoredPositionSingleTweener>().PlayReverse();
            }
            else
            {
                GetComponent<AnchoredPositionSingleTweener>().PlayTween();
            }
        }

        IEnumerator Cooldown()
        {
            int i = 45;
            if (PandoraBoxMaster.CurrentPandoraPlayer.IsPremium())
                i = 15;

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
