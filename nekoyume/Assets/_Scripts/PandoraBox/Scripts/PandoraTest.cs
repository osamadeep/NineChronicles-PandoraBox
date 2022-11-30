using Nekoyume.Game;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public class PandoraTest : MonoBehaviour
    {
        public SkeletonAnimation SkeletonAnimationEnemy;
        private SkeletonAnimation skeletonAnimation;
        public Sprite sprite;
        public GameObject bullet;
        public RunnerUnitMovements[] BG;

        void Awake()
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        private void Start()
        {
            //skeletonAnimation.Skeleton.SetAttachment("weapon", null);
            ////if (PandoraBoxMaster.CurrentPandoraPlayer.SwordSkin == 1)
            ////    _player.SpineController.UpdateWeapon(10151001, sprite1, PandoraBoxMaster.Instance.CosmicSword);
            //var _clonedSkin = skeletonAnimation.skeleton.Data.DefaultSkin.GetClone();
            //var _weaponSlotIndex = skeletonAnimation.skeleton.FindSlotIndex("weapon");
            //Shader _shader;
            //_shader = Shader.Find("Spine/Skeleton");
            //var _material = new Material(_shader);
            //var _atlasPage = _material.ToSpineAtlasPage();
            //var attachment = sprite.ToRegionAttachment(_atlasPage);
            //_clonedSkin.SetAttachment(_weaponSlotIndex, "weapon", attachment);//_weaponSlotIndex, WeaponSlot, newWeapon);
            //skeletonAnimation.skeleton.SetSkin( _clonedSkin);
        }

        private void Update()
        {
            SetMove(false);
            if (Input.GetKey(KeyCode.E))
                skeletonAnimation.AnimationName = "Casting";
            else if (Input.GetKey(KeyCode.Q))
                skeletonAnimation.AnimationName = "Touch";
            else if (Input.GetKey(KeyCode.R))
            {
                skeletonAnimation.AnimationName = "Hit";
                StartCoroutine(Bullet());
            }
            else if (Input.GetKey(KeyCode.W))
                skeletonAnimation.AnimationName = "Idle";
            else if (Input.GetKey(KeyCode.I))
            {
                SetMove(true);
                //enemy spawn
                SkeletonAnimationEnemy.gameObject.SetActive(true);
                SkeletonAnimationEnemy.transform.position = new Vector3(7, -1.4f);
                SkeletonAnimationEnemy.GetComponent<RunnerUnitMovements>().MoveAxis = new Vector2(1,0);
                SkeletonAnimationEnemy.loop = true;
                SkeletonAnimationEnemy.AnimationName = "Run";
            }
            else
            {
                skeletonAnimation.AnimationName = "Run";
                SetMove(true);
            }
        }

        bool isBullet = false;
        IEnumerator Bullet()
        {
            if (isBullet)
                yield break;

            isBullet = true;
            //yield return new WaitForSeconds(0.5f);
            bullet.SetActive(true);
            //ActionCamera.instance.Shake();
            yield return new WaitForSeconds(0.2f);

            SkeletonAnimationEnemy.loop = false;
            SkeletonAnimationEnemy.AnimationName = "Die";
            SkeletonAnimationEnemy.GetComponent<RunnerUnitMovements>().MoveAxis = Vector3.zero;
            yield return new WaitForSeconds(2f);
            bullet.SetActive(false);
            isBullet = false;
        }

        void SetMove(bool isMove)
        {
            foreach (var item in BG)
            {
                item.MoveAxis = isMove?new Vector2(1,0): new Vector2(0, 0);
            }
            SkeletonAnimationEnemy.GetComponent<RunnerUnitMovements>().MoveSpeed = isMove ? -2.4f : -1.2f;
        }
    }
}
