using Nekoyume.Game.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.PandoraBox
{
    public class RunnerMissile : MonoBehaviour
    {
        public Transform WarningSprite;
        public GameObject Missile;
        public Transform runnerPlayer;
        public GameObject BlowVFX;

        // Update is called once per frame
        void Update()
        {
            WarningSprite.position = new Vector3(WarningSprite.position.x, runnerPlayer.position.y);
        }

        public void EliminateMissile()
        {
            Missile.SetActive(false);
            BlowVFX.SetActive(false);
            BlowVFX.SetActive(true);
            AudioController.instance.PlaySfx(AudioController.SfxCode.Critical01);
        }
    }
}
