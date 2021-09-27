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
            string temp = PandoraBoxMaster.Instance.Settings.VersionId;
            string textVer = string.Format("v{0}.{1}.{2}",
                            int.Parse(temp.Substring(0, 2)),
                            int.Parse(temp.Substring(2, 2)),
                            int.Parse(temp.Substring(4, 2)));
            verText.text = textVer;
        }
    }
}

