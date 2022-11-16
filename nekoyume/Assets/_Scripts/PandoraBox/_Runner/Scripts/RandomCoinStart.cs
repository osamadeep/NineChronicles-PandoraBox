using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public class RandomCoinStart : MonoBehaviour
    {
        // Start is called before the first frame update
        void OnEnable()
        {
            GetComponentInChildren<Animator>().Play(0, -1, Random.value);
        }
    }
}
