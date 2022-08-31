using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;

namespace Nekoyume.PandoraBox
{
    public class RainbowRGB : MonoBehaviour
    {
        public float speed;
        float h, s, v;

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
            gameObject.GetComponent<MeshRenderer>().materials[0].SetColor("_OutlineColor", PandoraMaster.StickManOutlineColor);
        }
    }
}
