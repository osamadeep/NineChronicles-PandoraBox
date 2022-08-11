using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public class RunnerUnitMovements : MonoBehaviour
    {
        public Vector3 MoveAxis;
        public float MoveSpeed;
        RectTransform Recttransform;
        public float TimeScale = 1;

        // Start is called before the first frame update
        void Start()
        {
            Recttransform = GetComponent<RectTransform>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Recttransform.anchoredPosition.x <= -1500)
                gameObject.SetActive(false);
            transform.Translate(MoveAxis * MoveSpeed * TimeScale * Time.deltaTime);
        }
    }
}
