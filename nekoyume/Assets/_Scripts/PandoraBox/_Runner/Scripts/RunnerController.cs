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
        public float AccelerateJet=0.3f;
        Rigidbody2D rb;
        int jumpCount;
        public float TimeScale;
        public PandoraRunner.RunnerState runner = PandoraRunner.RunnerState.Start;
        public bool IsShield = false;
        bool IsFlying = false;
        public AudioSource WalkingSound;

        public SkeletonAnimation RunnerSkeletonAnimation;

        [SerializeField]
        ParticleSystem jetpackFire;


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
            //    rb.gravityScale = TimeScale;
            //    if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && runner == PandoraRunner.RunnerState.Play)
            //    {
            //        jetpackFire.SetActive(true);
            //        AudioController.instance.PlaySfx(AudioController.SfxCode.Jet);
            //    }

            //    if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space)) && runner == PandoraRunner.RunnerState.Play)
            //    {
            //        jetpackFire.SetActive(false);
            //        GetComponent<AudioSource>().enabled = false;
            //    }



            //    if ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) &&  runner == PandoraRunner.RunnerState.Play)
            //    {
            //        rb.velocity = new Vector2(0, Mathf.Sqrt(-2.0f * Physics2D.gravity.y * Jump * TimeScale));
            //        JetPackIsOn(true);
            //        GetComponent<AudioSource>().enabled = true;
            //    }

            //    //check if its on ground
            //    if (rb.velocity.y )
            IsFlying = ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) && runner == PandoraRunner.RunnerState.Play);


            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && runner == PandoraRunner.RunnerState.Play)
            {
                GetComponent<AudioSource>().enabled = false;
                AudioController.instance.PlaySfx(AudioController.SfxCode.Jet);
                RunnerSkeletonAnimation.loop = true;
                RunnerSkeletonAnimation.AnimationName = "Casting";
                RunnerSkeletonAnimation.Skeleton.SetAttachment("shadow", null);
            }
            else if ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) && runner == PandoraRunner.RunnerState.Play)
            {

            }
            else if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space)) && runner == PandoraRunner.RunnerState.Play)
            {

            }
        }

        private void FixedUpdate()
        {
            if (IsFlying)
            {
                rb.AddForce( Vector2.up * AccelerateJet * TimeScale);


                var jf = jetpackFire.emission;
                jf.enabled = true;
            }
            else
            {
                if (rb.velocity.y < 0)
                    rb.AddForce(Vector2.up * Mathf.Abs(rb.velocity.y) * TimeScale);

                var jf = jetpackFire.emission;
                jf.enabled = false;
            }

            //if (runner == PandoraRunner.RunnerState.Play && transform.position.y < )
        }


        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (( collision.CompareTag("Enemy")) && runner == PandoraRunner.RunnerState.Play && !IsShield)
            {
                Game.Game.instance.Runner.PlayerGotHit();
                collision.transform.parent.gameObject.SetActive(false);
                AudioController.instance.PlaySfx(AudioController.SfxCode.Critical01);
            }
            else if (collision.CompareTag("Missile") && runner == PandoraRunner.RunnerState.Play && !IsShield)
            {
                Game.Game.instance.Runner.PlayerGotHit();
                collision.GetComponentInParent<RunnerMissile>().EliminateMissile();
            }
            else if (collision.CompareTag("Missile") && runner == PandoraRunner.RunnerState.Play)
            {
                //add quest counter here
                collision.GetComponentInParent<RunnerMissile>().EliminateMissile();
            }
            else if (collision.CompareTag("Coin") && runner == PandoraRunner.RunnerState.Play)
            {
                Game.Game.instance.Runner.CollectCoins(collision.transform);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.transform.CompareTag("GroundCollider") && runner == PandoraRunner.RunnerState.Play && !IsFlying)
            {
                PlayerOnGround();
            }
        }

        public void EnableSpeed(bool isEnable)
        {
            IsShield = isEnable;
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

        public void PlayerOnGround()
        {
            ActionCamera.instance.Shake();
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
            RunnerSkeletonAnimation.loop = true;
            RunnerSkeletonAnimation.AnimationName = "Run";
            //Animator.Play(nameof(CharacterAnimation.Type.Run), 0, 0f);
            RunnerSkeletonAnimation.Skeleton.SetAttachment("shadow", attachment.Name);
            GetComponent<AudioSource>().enabled = true;
        }
    }
}
