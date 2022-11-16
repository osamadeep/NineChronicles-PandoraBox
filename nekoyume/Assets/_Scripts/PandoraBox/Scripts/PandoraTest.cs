using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public class PandoraTest : MonoBehaviour
    {

        private SkeletonAnimation skeletonAnimation;

        void Awake()
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.Space))
                skeletonAnimation.AnimationName = "Casting";
            else
                skeletonAnimation.AnimationName = "Run";
        }

        void LateUpdate()
        {

        }
    }
}
