using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BackgroundSpriteScroller : MonoBehaviour
    {
        public float Speed = 0.5f;
        public float TimeScale;
        Image image;

        private void Start()
        {
            image = GetComponent<Image>();
            TimeScale = 1;
        }

        void Update()
        {
            Vector2 offset = new Vector2(Time.deltaTime * Speed * TimeScale, 0);
            image.material.mainTextureOffset += offset;
        }
    }
}
