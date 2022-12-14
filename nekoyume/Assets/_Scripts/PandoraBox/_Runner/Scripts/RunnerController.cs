using Grpc.Core;
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
        [SerializeField] LayerMask groundLayer;
        [SerializeField] Transform groundChecker;
        [SerializeField] GameObject SpeedVFX;
        public float AccelerateJet=0.3f;
        Rigidbody2D rb;
        int jumpCount;
        public float TimeScale;
        public PandoraRunner.RunnerState runner = PandoraRunner.RunnerState.NewRound;
        
        
        public AudioSource WalkingSound;

        public SkeletonAnimation RunnerSkeletonAnimation;

        public bool IsInvincible;
        public bool IsGround;
        public bool IsJetting;

        [SerializeField]
        ParticleSystem jetpackFire;


        private void Awake()
        {
            transform.position = new Vector2(-5000, -5000);
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

        void Update()
        {
            if (runner != PandoraRunner.RunnerState.Playing)
                return;

            IsJetting = ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.W)));
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.W)))
                AudioController.instance.PlaySfx(AudioController.SfxCode.Jet);
        }

        private void FixedUpdate()
        {
            if (runner != PandoraRunner.RunnerState.Playing)
                return;

            CheckIfOnGround();
            if (IsJetting)
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
        }


        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (runner != PandoraRunner.RunnerState.Playing)
                return;

            if (( collision.CompareTag("Enemy")) && !IsInvincible)
            {
                if (!IsInvincible)
                {
                    Game.Game.instance.Runner.PlayerGotHit();
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Critical01);
                }
                else
                {
                    //add quest counter here
                }
                collision.transform.parent.gameObject.SetActive(false);
            }
            else if (collision.CompareTag("Missile"))
            {
                if (!IsInvincible)
                    Game.Game.instance.Runner.PlayerGotHit();
                else
                {
                    //add quest counter here
                }
                collision.GetComponentInParent<RunnerMissile>().EliminateMissile();
            }
            else if (collision.CompareTag("Coin"))
            {
                Game.Game.instance.Runner.CollectCoins(collision.transform);
            }
        }

        public void EnableSpeed(bool isEnable)
        {
            IsInvincible = isEnable;
            SpeedVFX.SetActive(isEnable);
        }


        public IEnumerator RecoverAnimation()
        {
            ChangeState("Win");
            yield return new WaitForSeconds(2.33f);
            ChangeState("Run");
        }

        public void CheckIfOnGround()
        {
            var colliding = Physics2D.OverlapCircle(groundChecker.transform.position, 0.02f, groundLayer);
            IsGround = colliding == null ? false : true;
            
            GetComponent<AudioSource>().enabled = IsGround;
            if (IsGround)
                ChangeState("Run");
            else
                ChangeState("Flying");
        }

        public void ChangeState(string currentPose)
        {

            RunnerSkeletonAnimation.loop = true;
            RunnerSkeletonAnimation.Skeleton.SetAttachment("shadow", attachment.Name);
            switch (currentPose)
            {
                case "Run":
                    WalkingSound.enabled = true;
                    RunnerSkeletonAnimation.AnimationName = "Run";
                    break;
                case "Flying":
                    WalkingSound.enabled = false;
                    RunnerSkeletonAnimation.AnimationName = "Casting";
                    RunnerSkeletonAnimation.Skeleton.SetAttachment("shadow", null);
                    break;
                case "Idle":
                    WalkingSound.enabled = false;
                    RunnerSkeletonAnimation.AnimationName = "Idle";
                    break;
                case "Win":
                    WalkingSound.enabled = false;
                    RunnerSkeletonAnimation.loop = false;
                    RunnerSkeletonAnimation.AnimationName = "Win";
                    break;
                case "Lose":
                    WalkingSound.enabled = false;
                    var jf = jetpackFire.emission;
                    jf.enabled = false;
                    RunnerSkeletonAnimation.loop = false;
                    RunnerSkeletonAnimation.AnimationName = "TurnOver_02";
                    break;
            }
        }
    }
}
