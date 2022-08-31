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
    using Nekoyume.Model.Item;
    using Nekoyume.UI;
    using Nekoyume.UI.Module;
    using Nekoyume.UI.Scroller;
    using Nekoyume.UI.Tween;
    using System.Collections.Generic;
    using UniRx;

    public class Raid : MonoBehaviour
    {
        public static Raid Instance;

        [SerializeField] TMP_InputField StageIDText;

        [SerializeField] TMP_InputField CurrentTriesManual;

        [SerializeField] GameObject RotateShape;

        [SerializeField] Button RaidButton = null;

        [SerializeField] TextMeshProUGUI RaidButtonText = null;

        [SerializeField] TextMeshProUGUI CurrentTriesText = null;

        private Game.Character.Player _player;

        bool isBusy = false;
        int totalCount;
        public int CurrentStageID;

        void Awake()
        {
            RaidButton.OnClickAsObservable().Subscribe(_ => StartRaid()).AddTo(gameObject);
            if (Instance == null)
            {
                Instance = this;
            }
        }

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
            try
            {
                StageIDText.text = PlayerPrefs.GetInt("_PandoraBox_PVE_LastRaidStage_" + States.Instance.CurrentAvatarState.address, 202).ToString();
            }
            catch { }
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
            bool isMenu = false;
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                try
                {
                    CurrentTriesText.text = ((int)(States.Instance.CurrentAvatarState.actionPoint / 5)).ToString();
                    if (Widget.Find<Menu>().IsActive() && !isMenu)
                    {
                        //rt.anchoredPosition = new Vector3(10, -250);
                        StageIDText.text = PlayerPrefs.GetInt("_PandoraBox_PVE_LastRaidStage_" + States.Instance.CurrentAvatarState.address, 202).ToString();
                        GetComponent<AnchoredPositionSingleTweener>().PlayTween();
                        isMenu = true;
                    }
                    else if (!Widget.Find<Menu>().IsActive() && isMenu)
                    {
                        //rt.anchoredPosition = new Vector3(-400, -250);
                        StageIDText.text = PlayerPrefs.GetInt("_PandoraBox_PVE_LastRaidStage_" + States.Instance.CurrentAvatarState.address, 202).ToString();
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
#if !UNITY_EDITOR
            if (isBusy)
            {
                isBusy = false;
                StartCoroutine(Cooldown());
            }
            else if (PandoraUtil.IsBusy())
            {
                //show error msg
            }
            else
#endif
            {
                CurrentStageID = int.Parse(StageIDText.text);
                totalCount = int.Parse(CurrentTriesManual.text);
                States.Instance.CurrentAvatarState.worldInformation.TryGetLastClearedStageId(out var clearedStage);
                if (int.Parse(StageIDText.text) > clearedStage + 1)
                {
                    OneLineSystem.Push(MailType.System,
                        $"<color=green>Pandora Box</color>: You Didnt Open {int.Parse(StageIDText.text)} yet!"
                        , NotificationCell.NotificationType.Alert);
                    return;
                }

                int tries = ((int)(States.Instance.CurrentAvatarState.actionPoint / 5));

                if (PandoraMaster.CurrentPandoraPlayer.IsPremium())
                {
                    StartCoroutine(StartRaid(totalCount));
                }
                else
                {
                    if (tries <= 0)
                    {
                        OneLineSystem.Push(MailType.System,
                            "<color=green>Pandora Box</color>: You have <b>0</b> Action Points!"
                            , NotificationCell.NotificationType.Alert);
                        return;
                    }

                    StartCoroutine(StartRaid(totalCount));
                }
            }
        }

        IEnumerator StartRaid(int count)
        {
            isBusy = true;
            PandoraMaster.CurrentAction = PandoraUtil.ActionType.HackAndSlash;
            RotateShape.SetActive(true);
            StageIDText.interactable = false;
            RaidButton.GetComponent<Image>().color = Color.red;
            RaidButtonText.text = "Cancel!";
            float AllowedCooldown = 3f; //save it to settings

            //yield return new WaitForSeconds(AllowedCooldown);
            yield return new WaitForSeconds(AllowedCooldown);

            if (PandoraMaster.CurrentPandoraPlayer.IsPremium())
                AllowedCooldown = 0.5f;
            _player = Game.Game.instance.Stage.GetPlayer();
            var stage = Game.Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(i =>
                i.Id == int.Parse(StageIDText.text));

            int worldID = 0;
            if (stage.Id < 51)
                worldID = 1;
            else if (stage.Id > 50 && stage.Id <= 100)
                worldID = 2;
            else if (stage.Id > 100 && stage.Id <= 150)
                worldID = 3;
            else if (stage.Id > 150 && stage.Id <= 200)
                worldID = 4;
            else if (stage.Id > 200 && stage.Id <= 250)
                worldID = 5;
            else if (stage.Id > 250 && stage.Id <= 300)
                worldID = 6;

            int _requiredCost = stage.CostAP;


            //bool RaidMethodIsSweep = Convert.ToBoolean(PlayerPrefs.GetInt("_PandoraBox_PVE_RaidMethodIsSweep", 0));
            bool RaidMethodIsProgress = PandoraMaster.Instance.Settings.RaidMethodIsProgress;

            if (!isBusy)
            {
                PandoraMaster.CurrentAction = PandoraUtil.ActionType.Idle;
                yield break;
            }

            //save last stage id
            PlayerPrefs.SetInt("_PandoraBox_PVE_LastRaidStage_" + States.Instance.CurrentAvatarState.address, stage.Id);

            if (!RaidMethodIsProgress) //farming materials
            {
                ActionRenderHandler.Instance.Pending = true;

                var equipments = _player.Equipments;
                var costumes = _player.Costumes;

                List<System.Guid> costumesN;
                List<System.Guid> equipmentsN;

                costumesN = costumes.Select(c => c.ItemId).ToList();
                equipmentsN = equipments.Select(e => e.ItemId).ToList();

                Game.Game.instance.ActionManager.HackAndSlashSweep(
                costumesN,
                equipmentsN,
                0,
                count *5,
                worldID,
                stage.Id);

#if UNITY_EDITOR
                Debug.LogError("Raid Done! " + States.Instance.CurrentAvatarState.name);
#endif

                OneLineSystem.Push(MailType.System,
                    "<color=green>Pandora Box</color>: Sending Farming Raids for Stage <color=red>" + stage.Id
                    + "</color> (<color=green>" + count + "</color>) times ...",
                    NotificationCell.NotificationType.Information);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (!isBusy)
                        yield break;
                    RaidButtonText.text = $"(<color=green>{count - (i + 1)}</color>)Cancel!";
                    ActionRenderHandler.Instance.Pending = true;
                    Game.Game.instance.ActionManager.HackAndSlash(_player, worldID, stage.Id).Subscribe();

                    OneLineSystem.Push(MailType.System,
                        "<color=green>Pandora Box</color>: Sending Progress Raids Stage <color=red>" + stage.Id
                        + "</color> <color=green>" + (i + 1) + "</color>/" + count + "...",
                        NotificationCell.NotificationType.Information);
                    yield return new WaitForSeconds(AllowedCooldown);
                }
            }

            StartCoroutine(Cooldown());
        }

        //public void Show(bool isReversed)
        //{
        //    StageIDText.text = PlayerPrefs.GetInt("_PandoraBox_PVE_LastRaidStage_" + States.Instance.CurrentAvatarState.address, 202).ToString();
        //    if (!isReversed)
        //    {
        //        GetComponent<AnchoredPositionSingleTweener>().PlayReverse();
        //    }
        //    else
        //    {
        //        GetComponent<AnchoredPositionSingleTweener>().PlayTween();
        //    }
        //}

        IEnumerator Cooldown()
        {
            int i = 15;
            if (PandoraMaster.CurrentPandoraPlayer.IsPremium())
                i = 5;

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
