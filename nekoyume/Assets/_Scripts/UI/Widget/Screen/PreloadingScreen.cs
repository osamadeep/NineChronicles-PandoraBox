using Libplanet;
using Nekoyume.Game.Factory;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.Video;
using Nekoyume.PandoraBox;

namespace Nekoyume.UI
{
    public class PreloadingScreen : LoadingScreen
    {
        [SerializeField]
        private VideoPlayer videoPlayer;

        [SerializeField]
        private VideoClip showClip;

        [SerializeField]
        private VideoClip loopClip;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
            videoPlayer.clip = showClip;
            videoPlayer.Prepare();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            if (!string.IsNullOrEmpty(Message))
            {
                indicator.Show(Message);
            }

            videoPlayer.Play();
            videoPlayer.loopPointReached += OnShowVideoEnded;
        }

        public override async void Close(bool ignoreCloseAnimation = false)
        {
            videoPlayer.Stop();
            if (PandoraBoxMaster.Instance.Settings.IsStory)
            {
                Find<Synopsis>().Show();
            }
            else
            {
                PlayerFactory.Create();

                if (Util.TryGetStoredAvatarSlotIndex(out var slotIndex) &&
                    States.Instance.AvatarStates.ContainsKey(slotIndex))
                {
                    var avatarState = States.Instance.AvatarStates[slotIndex];
                    if (avatarState?.inventory == null ||
                        avatarState.questList == null ||
                        avatarState.worldInformation == null)
                    {
                        EnterLogin();
                    }
                    else
                    {
                        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                        await States.Instance.SelectAvatarAsync(slotIndex);
                        PandoraBoxMaster.SetCurrentPandoraPlayer(PandoraBoxMaster.GetPandoraPlayer(States.Instance.CurrentAvatarState.agentAddress.ToString()));
                        if (PandoraBoxMaster.CurrentPandoraPlayer.IsBanned)
                        {
                            videoPlayer.Play();
                            PandoraBoxMaster.Instance.ShowError(101, "This address is Banned, please visit us for more information!");
                            return;
                        }
                        else
                        {
                            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                            //await States.Instance.SelectAvatarAsync(slotIndex);
                            Game.Event.OnRoomEnter.Invoke(false);
                        }
                    }
                }
                else
                {
                    EnterLogin();
                }
            }

            base.Close(ignoreCloseAnimation);
            indicator.Close();
        }

        private static void EnterLogin()
        {
            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            PandoraBoxMaster.SetCurrentPandoraPlayer(PandoraBoxMaster.GetPandoraPlayer(States.Instance.CurrentAvatarState.agentAddress.ToString()));
            if (PandoraBoxMaster.CurrentPandoraPlayer.IsBanned)
                PandoraBoxMaster.Instance.ShowError(101, "This address is Banned, please visit us for more information!");
            else
            {
                //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                Find<Login>().Show();
                Game.Event.OnNestEnter.Invoke();
            }
        }

        private void OnShowVideoEnded(VideoPlayer player)
        {
            player.loopPointReached -= OnShowVideoEnded;
            videoPlayer.clip = loopClip;
            player.isLooping = true;
            videoPlayer.Play();
        }
    }
}
