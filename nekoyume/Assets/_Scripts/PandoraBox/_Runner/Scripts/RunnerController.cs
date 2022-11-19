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

        [SerializeField]
        GameObject jetpackFire;


        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            WalkingSound = GetComponent<AudioSource>();
        }

        void Start()
        {
            StartCoroutine(InitilizeRunner());
        }


        Attachment attachment;
        IEnumerator InitilizeRunner()
        {
            WalkingSound.enabled = false;
            yield return new WaitForSeconds(2);

            // Find the slot by name.
            var slot = RunnerSkeletonAnimation.skeleton.FindSlot("shadow");
            // Get the attachment by name from the skeleton's skin or default skin.
            attachment = RunnerSkeletonAnimation.skeleton.GetAttachment(slot.Skeleton.FindSlotIndex("shadow"), "shadow");
            // Sets the slot's attachment.
            slot.Attachment = attachment;

            RunnerSkeletonAnimation.Skeleton.SetAttachment("weapon", null);
            gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            rb.gravityScale = TimeScale;
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) &&  runner == PandoraRunner.RunnerState.Play)
            {
                //AudioController.instance.PlaySfx(AudioController.SfxCode.Jump);
                //jumpCount++;
                rb.velocity = Vector2.zero;
                rb.angularDrag = 0;
                rb.velocity = new Vector2(0, Mathf.Sqrt(-2.0f * Physics2D.gravity.y * Jump * TimeScale));

                JetPackIsOn(true);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (( collision.CompareTag("Enemy")) && runner == PandoraRunner.RunnerState.Play && !IsSheld)
            {
                Game.Game.instance.Runner.PlayerGotHit();
                collision.transform.parent.gameObject.SetActive(false);
            }
            else if (collision.CompareTag("Missile") && runner == PandoraRunner.RunnerState.Play && !IsSheld)
            {
                Game.Game.instance.Runner.PlayerGotHit();
                collision.gameObject.SetActive(false);
            }
            else if (collision.CompareTag("Coin") && runner == PandoraRunner.RunnerState.Play)
            {
                Game.Game.instance.Runner.CollectCoins(collision.transform);
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
            RunnerSkeletonAnimation.loop = false;
            RunnerSkeletonAnimation.AnimationName = "TurnOver_02";
        }

        public IEnumerator RecoverAnimation()
        {
            RunnerSkeletonAnimation.loop = false;
            RunnerSkeletonAnimation.AnimationName = "Win";
            yield return new WaitForSeconds(2.33f);

            WalkingSound.enabled = true;
            RunnerSkeletonAnimation.loop = true;
            RunnerSkeletonAnimation.AnimationName = "Run";
        }

        public void Attack()
        {
            //Animator.Play(nameof(CharacterAnimation.Type.Attack), BaseLayerIndex, 0f);
        }


        public void JetPackIsOn(bool isOn)
        {
            if (isOn)
            {
                AudioController.instance.PlaySfx(AudioController.SfxCode.Jet);
                RunnerSkeletonAnimation.loop = true;
                RunnerSkeletonAnimation.AnimationName = "Casting";
                //Animator.Play(nameof(CharacterAnimation.Type.Casting), 0, 0f);
                RunnerSkeletonAnimation.Skeleton.SetAttachment("shadow", null);
                GetComponent<AudioSource>().enabled = false;
            }
            else
            {
                ActionCamera.instance.Shake();
                AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
                RunnerSkeletonAnimation.loop = true;
                RunnerSkeletonAnimation.AnimationName = "Run";
                //Animator.Play(nameof(CharacterAnimation.Type.Run), 0, 0f);
                RunnerSkeletonAnimation.Skeleton.SetAttachment("shadow", attachment.Name);
                GetComponent<AudioSource>().enabled = true;
            }

            jetpackFire.SetActive(isOn);
            //Animator.SetBool(nameof(CharacterAnimation.Type.Run), !isOn);
        }
    }
}
