using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BackgroundScroller : MonoBehaviour
    {
        float xDistance;
        private void Start()
        {
            xDistance = (transform.GetChild(0).position.x - transform.GetChild(1).position.x) * 3;
        }
        void Update()
        {
            //Debug.LogError(transform.position.x);
            if (transform.position.x <= -10)
                transform.position = new Vector2(transform.position.x + xDistance, 0);
        }
    }
}
