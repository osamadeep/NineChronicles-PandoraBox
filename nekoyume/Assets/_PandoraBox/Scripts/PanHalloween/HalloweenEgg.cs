using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using UnityEngine;

public class HalloweenEgg : MonoBehaviour
{

    //private void OnMouseDown()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

    //        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
    //        if (hit.collider != null)
    //        {
    //            if (hit.transform.tag == "Egg")
    //            {

    //            }
    //        }
    //    }
    //}

    private void Start()
    {
        PanHalloween.Instance.Eggs[int.Parse(name)] = transform;
        gameObject.SetActive(false);
    }

    public void SendEgg(int x)
    {
                    OneLineSystem.Push(MailType.System, $"<color=green>Pandora Box</color>: You found egg # <color=green>{x}</color>/8, Keep going"
                        , NotificationCell.NotificationType.Information);
                    PanHalloween.Instance.FoundOne(x);
                    //gameObject.SetActive(false);
    }
}
