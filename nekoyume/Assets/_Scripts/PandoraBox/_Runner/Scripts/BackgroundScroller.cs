using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BackgroundScroller : MonoBehaviour
    {
        public int SceneIndex = 0;
        public Sprite[] Sprites;
        public float EndAlpha;
        public float TransactionTime;
        public float XEnd = -16;
        float xDistance;
        private void Start()
        {
            xDistance = (transform.position.x - transform.GetChild(0).position.x) * 3;
            StartCoroutine(Change());
        }
        void Update()
        {
            //Debug.LogError(transform.position.x);
            if (transform.position.x <= XEnd)
                transform.position -= new Vector3( xDistance, 0,0);

        }

        IEnumerator Change()
        {
            while (true)
            {
                yield return new WaitForSeconds(100);
                StartCoroutine(ChangeScene());
            }
        }

        IEnumerator ChangeScene()
        {
            SceneIndex = ++SceneIndex % 3;




            List<SpriteRenderer> sprites = new List<SpriteRenderer>();
            for (int i = 1; i < transform.childCount; i++)
            {
                sprites.Add(transform.GetChild(i).GetComponent<SpriteRenderer>());
            }

            float elapsedTime = 0;;
            while (elapsedTime < TransactionTime)
            {
                foreach (SpriteRenderer item in sprites)
                    item.color = new Color(1, 1, 1, Mathf.Lerp(1f, EndAlpha, (elapsedTime / TransactionTime)));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            foreach (SpriteRenderer item in sprites)
                item.sprite = Sprites[SceneIndex];

            elapsedTime = 0;
            while (elapsedTime < TransactionTime)
            {
                foreach (SpriteRenderer item in sprites)
                    item.color = new Color(1, 1, 1, Mathf.Lerp(EndAlpha, 1f, (elapsedTime / TransactionTime)));
                elapsedTime += Time.deltaTime;
                yield return null;
            }


        }
    }
}
