using UnityEngine;

namespace PandoraBox
{
    public class PandoraBoxController : MonoBehaviour
    {
        public static PandoraBoxController instance;

        public static string supportAddress = "0x46528E7DEdaC16951bDccb55B20303AB0c729679";
        public PB_Settings settingsButton;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ShowPBSettingsButton()
        {
            settingsButton.Show();
        }
    }
}
