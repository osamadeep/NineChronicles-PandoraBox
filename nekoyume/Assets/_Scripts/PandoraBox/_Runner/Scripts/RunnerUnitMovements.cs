using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    //[RequireComponent(typeof(Rigidbody2D))]
    public class RunnerUnitMovements : MonoBehaviour
    {
        public Vector2 MoveAxis;
        public float MoveSpeed;
        public float TimeScale = 1;
        public float EndPosition = -3000;
        //Rigidbody2D rb;

        private void Start()
        {
            //rb = GetComponent<Rigidbody2D>();
        }

        // Update is called once per frame
        private void Update()
        {
            transform.Translate(MoveAxis * MoveSpeed * TimeScale * Time.deltaTime);
            //rb.MovePosition(rb.position + (MoveAxis * MoveSpeed * TimeScale * Time.deltaTime));
            //rb.velocity = MoveAxis * MoveSpeed * TimeScale * Time.deltaTime * 100;
            if (transform.position.x <= -20)
            {
                //if (transform.CompareTag("Missile"))
                //{
                //    transform.parent.gameObject.SetActive(false);
                //}
                //else
                gameObject.SetActive(false);
                //Debug.LogError(name + " Dead!");
            }
        }
    }
}
