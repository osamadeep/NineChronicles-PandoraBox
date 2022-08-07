using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
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
            float levelSpeed = 1;
            try
            {
                levelSpeed = Mathf.Clamp(Widget.Find<Runner>().LevelSpeed, 1, 5);
            }
            catch { }

            Vector2 offset = new Vector2(Time.time * Speed * levelSpeed, 0);
            image.material.mainTextureOffset = offset;
        }
    }
}
