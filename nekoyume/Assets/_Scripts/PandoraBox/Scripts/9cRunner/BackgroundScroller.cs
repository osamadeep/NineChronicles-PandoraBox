using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.PandoraBox
{
    public class BackgroundScroller : MonoBehaviour
    {
        public float Speed = 0.5f;
        Image image;

        private void Start()
        {
            image = GetComponent<Image>();
        }

        void Update()
        {
            Vector2 offset = new Vector2(Time.time * Speed, 0);
            image.material.mainTextureOffset = offset;
        }
    }
}
