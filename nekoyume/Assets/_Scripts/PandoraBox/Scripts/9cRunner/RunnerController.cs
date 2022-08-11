using Nekoyume.Game;
using Nekoyume.Game.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{

    public class RunnerController : MonoBehaviour
    {
        public float Jump;
        Rigidbody2D rb;
        int jumpCount;
        public float TimeScale;
        public Runner.RunnerState runner;


        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        // Update is called once per frame
        void Update()
        {
            rb.gravityScale = TimeScale;

            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && jumpCount < 1 && runner == Runner.RunnerState.Play)
            {
                AudioController.instance.PlaySfx(AudioController.SfxCode.Jump);
                jumpCount++;
                rb.velocity = Vector2.zero;
                rb.angularDrag = 0;
                rb.velocity = new Vector2(0, Mathf.Sqrt(-2.0f * Physics2D.gravity.y * Jump * TimeScale));
            }

            if (transform.position.y <= -1f)
                jumpCount = 0;
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Enemy") && runner == Runner.RunnerState.Play)
            {
                //Debug.LogError("Hit Enemy");
                Widget.Find<Runner>().PlayerGotHit();
                collision.gameObject.SetActive(false);
                ActionCamera.instance.Shake();
            }
            else if (collision.CompareTag("Coin") && runner == Runner.RunnerState.Play)
            {
                Widget.Find<Runner>().CollectCoins(collision.transform);
            }
        }
    }
}
