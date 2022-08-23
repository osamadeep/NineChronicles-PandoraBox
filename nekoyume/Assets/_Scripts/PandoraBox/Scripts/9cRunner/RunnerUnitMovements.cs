using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public class RunnerUnitMovements : MonoBehaviour
    {
        public Vector2 MoveAxis;
        public float MoveSpeed;
        public float TimeScale = 1;
        public float EndPosition = -3000;

        // Update is called once per frame
        void Update()
        {
            if (transform.position.x <= EndPosition)
                gameObject.SetActive(false);
            transform.Translate(MoveAxis * MoveSpeed * TimeScale * Time.deltaTime);
        }
    }
}
