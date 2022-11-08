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

        // Update is called once per frame
        void Update()
        {
            WarningSprite.position = new Vector3(WarningSprite.position.x, runnerPlayer.position.y);
        }
    }
}
