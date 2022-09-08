using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;

namespace Nekoyume.PandoraBox
{
    public class RainbowRGBParticle : MonoBehaviour
    {
        public float speed;
        float h, s, v;
        ParticleSystem.MainModule currentPS;

        private void Start()
        {
            currentPS = GetComponent<ParticleSystem>().main;
        }
        // Update is called once per frame
        void FixedUpdate()
        {
            Color.RGBToHSV(PandoraMaster.StickManOutlineColor, out h, out s, out v);
            h += speed / 10000;
            if (h > 1)
            {
                h = 0;
            }
            s = 1;
            v = 1;

            PandoraMaster.StickManOutlineColor = Color.HSVToRGB(h, s, v);
            currentPS.startColor = PandoraMaster.StickManOutlineColor;
        }
    }
}
