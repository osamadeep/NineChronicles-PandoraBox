using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.PandoraBox;
using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.PandoraBox
{

    public class RunnerController : MonoBehaviour
    {
        [SerializeField] GameObject SpeedVFX;
        public float Jump;
        Rigidbody2D rb;
        int jumpCount;
        public float TimeScale;
        public PandoraRunner.RunnerState runner = PandoraRunner.RunnerState.Start;
        bool IsSheld = false;
        public AudioSource WalkingSound;

        public SkeletonAnimation RunnerSkeletonAnimation;

        public Animator Animator;

        [SerializeField]
        GameObject jetpackFire;


        private void Awake()
        {

        }

        Attachment attachment;
        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            WalkingSound = GetComponent<AudioSource>();

            // Find the slot by name.
            var slot = RunnerSkeletonAnimation.skeleton.FindSlot("shadow");
            // Get the attachment by name from the skeleton's skin or default skin.
            attachment = RunnerSkeletonAnimation.skeleton.GetAttachment(slot.Skeleton.FindSlotIndex("shadow"), "shadow");
            // Sets the slot's attachment.
            slot.Attachment = attachment;

            RunnerSkeletonAnimation.Skeleton.SetAttachment("weapon", null);
        }

        // Update is called once per frame
        void Update()
        {
            rb.gravityScale = TimeScale;
            //Debug.LogError(transform.position.y);
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) &&  runner == PandoraRunner.RunnerState.Play)
            {
                //AudioController.instance.PlaySfx(AudioController.SfxCode.Jump);
                //jumpCount++;
                rb.velocity = Vector2.zero;
                rb.angularDrag = 0;
                rb.velocity = new Vector2(0, Mathf.Sqrt(-2.0f * Physics2D.gravity.y * Jump * TimeScale));

                JetPackIsOn(true);
            }

            //Debug.LogError(transform.position.y);
            //if (transform.position.y <= -1f)
            //{

            //}
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (( collision.CompareTag("Enemy") || collision.CompareTag("Missile")) && runner == PandoraRunner.RunnerState.Play && !IsSheld)
            {

                //Widget.Find<Runner>().PlayerGotHit();
                PandoraRunner.instance.PlayerGotHit();
                collision.transform.parent.gameObject.SetActive(false);
                //ActionCamera.instance.Shake();
            }
            else if (collision.CompareTag("Coin") && runner == PandoraRunner.RunnerState.Play)
            {
                //Widget.Find<Runner>().CollectCoins(collision.transform);
                PandoraRunner.instance.CollectCoins(collision.transform);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.transform.CompareTag("GroundCollider") && runner == PandoraRunner.RunnerState.Play)
            {
                JetPackIsOn(false);
            }
        }

        public void EnableSpeed(bool isEnable)
        {
            IsSheld = isEnable;
            SpeedVFX.SetActive(isEnable);
        }

        public void LoseAnimation()
        {
            WalkingSound.enabled = false;
            Animator.Play(nameof(CharacterAnimation.Type.TurnOver_02), 0, 0f);
            //Animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
        }

        public IEnumerator RecoverAnimation()
        {
            Animator.Play(nameof(CharacterAnimation.Type.Win_03), 0, 0f);
            yield return new WaitForSeconds(2);

            WalkingSound.enabled = true;
            Animator.Play(nameof(CharacterAnimation.Type.Run), 0, 0f);
            Animator.SetBool(nameof(CharacterAnimation.Type.Run), true);
        }

        public void Attack()
        {
            //Animator.Play(nameof(CharacterAnimation.Type.Attack), BaseLayerIndex, 0f);
        }


        void JetPackIsOn(bool isOn)
        {
            if (isOn)
            {
                Animator.Play(nameof(CharacterAnimation.Type.Casting), 0, 0f);
                RunnerSkeletonAnimation.Skeleton.SetAttachment("shadow", null);
                GetComponent<AudioSource>().enabled = false;
            }
            else
            {
                Animator.Play(nameof(CharacterAnimation.Type.Run), 0, 0f);
                RunnerSkeletonAnimation.Skeleton.SetAttachment("shadow", attachment.Name);
                GetComponent<AudioSource>().enabled = true;
            }

            jetpackFire.SetActive(isOn);
            Animator.SetBool(nameof(CharacterAnimation.Type.Run), !isOn);
        }
    }
}
