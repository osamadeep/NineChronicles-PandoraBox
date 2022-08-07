using Nekoyume.Game.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Runner : Widget
    {
        public float LevelSpeed = 1f;
        protected override void Awake()
        {
            base.Awake();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Runner1);

            StartCoroutine(SpeedMusic());
        }


        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
        }

        IEnumerator SpeedMusic()
        {
            Debug.LogError(GetComponent<AudioSource>().clip.length);
            yield return new WaitForSeconds(GetComponent<AudioSource>().clip.length);
            Debug.LogError("PLAYED!");
            AudioController.instance.PlayMusic(AudioController.MusicCode.Runner2);
        }
    }
}
