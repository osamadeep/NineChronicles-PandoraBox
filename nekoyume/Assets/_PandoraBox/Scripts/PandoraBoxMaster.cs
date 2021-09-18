using UnityEngine;

namespace PandoraBox
{
    public class PandoraBoxMaster : MonoBehaviour
    {
        public static PandoraBoxMaster Instance;
        public static string VersionId = "v1.0.5";
        public static string SupportAddress = "0x46528E7DEdaC16951bDccb55B20303AB0c729679";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
    }
}
