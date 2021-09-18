using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PandoraBox
{
    public class IntroCheck : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI verText;

        // Start is called before the first frame update
        void Start()
        {
            verText.text = PandoraBoxMaster.VersionId.ToString();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
