using Nekoyume.Model.Mail;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace Nekoyume.PandoraBox
{
    public static class PandoraUtil
    {
        public enum ActionType { Idle,HackAndSlash,Ranking,Event}

        public static bool IsBusy()
        {
            switch (PandoraMaster.CurrentAction)
            {
                case ActionType.Idle:
                    return false;
                case ActionType.Ranking:
                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Arena fight in-progress! Please wait ...", NotificationCell.NotificationType.Alert);
                    return true;
                case ActionType.HackAndSlash:
                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Stage fight in-progress! Please wait ...", NotificationCell.NotificationType.Alert);
                    return true;
                case ActionType.Event:
                    OneLineSystem.Push(MailType.System, "<color=green>Pandora Box</color>: Event fight in-progress! Please wait ...", NotificationCell.NotificationType.Alert);
                    return true;
                default:
                    return false;
            }
        }

        static Color32[] gradeColors = new Color32[6]
        {       
                Color.white,
                new Color(0, 193f/256f, 18f/256f),
                new Color(80f/256f, 106f/256f, 253f/256f),
                new Color(243f/256f, 68f/256f, 201f/256f),
                new Color(246f/256f, 153f/256f, 36f/256f),
                Color.red
        };

        public static PandoraItem GetPandoraItem(string itemName)
        {
            //0 01 4 0002
            PandoraItem item = new PandoraItem();
            item.IsBlockchain = System.Convert.ToBoolean(int.Parse(itemName.Substring(0, 1)));
            item.Type = int.Parse(itemName.Substring(1,2)); //decide what kind of items its, for arena banner it should be 01
            item.Grade = (Grade)int.Parse(itemName.Substring(3, 1)); // banner grade and color
            item.ID = itemName.Substring(4, 4); // item ID is different on the NFT ItemID
            //Debug.LogError(arenaBanner.IsBlockchain + " " + arenaBanner.Type + " " + arenaBanner.Grade + " " + arenaBanner.ID);

            item.Color = gradeColors[(int)item.Grade];
            return item;
        }

        public static string ToLongNumberNotation(BigInteger num)
        {
            var absoluteValue = BigInteger.Abs(num);
            var exponent = BigInteger.Log10(absoluteValue);
            if (absoluteValue >= BigInteger.One)
            {
                switch ((long)System.Math.Floor(exponent))
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        return num.ToString();
                    case 4:
                    case 5:
                    case 6:
                        return BigInteger.Divide(num, (BigInteger)1e3) + "K";
                    case 7:
                    case 8:
                    case 9:
                        return BigInteger.Divide(num, (BigInteger)1e6) + "M";
                    default:
                        return BigInteger.Divide(num, (BigInteger)1e9) + "B";
                }
            }

            return num.ToString();
        }
    }

    public class PandoraItem
    {
        public bool IsBlockchain;
        public int Type;
        public Grade Grade;
        public string ID;
        public Color Color;
    }

    public enum Grade
    {
        COMMON = 0,
        UNCOMMON = 1,
        RARE = 2,
        EPIC = 3,
        LEGENDARY = 4,
        MYTHIC = 5
    }
}
