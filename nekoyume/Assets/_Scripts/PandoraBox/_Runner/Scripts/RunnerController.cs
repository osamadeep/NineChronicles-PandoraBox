using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{

    public class RunnerController : MonoBehaviour
    {
        [SerializeField] GameObject SpeedVFX;
        public float Jump;
        Rigidbody2D rb;
        int jumpCount;
        public float TimeScale;
        public Runner.RunnerState runner;
        bool IsSheld = false;
        //[SerializeField] Player _player;
        //private Player _player;

        private void Awake()
        {

        }


        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();

            //var go = PlayerFactory.Create();
            //_player = go.GetComponent<Player>();
            //_player.Equip(10251001, 10151000);
            //_player.StartRun();
        }

        // Update is called once per frame
        void Update()
        {
            rb.gravityScale = TimeScale;
            //Debug.LogError(transform.position.y);
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) &&  runner == Runner.RunnerState.Play)
            {
                AudioController.instance.PlaySfx(AudioController.SfxCode.Jump);
                //anim.pla PlayAnimationForState(nameof(CharacterAnimation.Type.Attack), 0);
                //jumpCount++;
                rb.velocity = Vector2.zero;
                rb.angularDrag = 0;
                rb.velocity = new Vector2(0, Mathf.Sqrt(-2.0f * Physics2D.gravity.y * Jump * TimeScale));
            }

            //if (transform.position.y <= -1f)
            //    jumpCount = 0;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Enemy") && runner == Runner.RunnerState.Play && !IsSheld)
            {

                Widget.Find<Runner>().PlayerGotHit();
                if (collision.name == "EnemyRocket")
                    collision.gameObject.SetActive(false);
                else
                    collision.transform.parent.gameObject.SetActive(false);
                //ActionCamera.instance.Shake();
            }
            else if (collision.CompareTag("Coin") && runner == Runner.RunnerState.Play)
            {
                Widget.Find<Runner>().CollectCoins(collision.GetComponent<RectTransform>());
            }
        }

        public void EnableSpeed(bool isEnable)
        {
            IsSheld = isEnable;
            SpeedVFX.SetActive(isEnable);
        }

        public void Attack()
        {
            //Animator.Play(nameof(CharacterAnimation.Type.Attack), BaseLayerIndex, 0f);
        }
    }
}
