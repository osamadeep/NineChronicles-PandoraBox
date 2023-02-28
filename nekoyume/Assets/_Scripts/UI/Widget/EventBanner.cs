using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Notice;

namespace Nekoyume.UI.Module
{
    public class EventBanner : Widget
    {
        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")] [SerializeField]
        private Button closeButton;

        [SerializeField] private WNCGPrice priceWidget;

        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
        [SerializeField]
        private RectTransform content = null;

        [SerializeField] private RectTransform indexContent = null;

        [SerializeField] private GameObject Banner;

        [SerializeField] private GameObject IndexOn;

        [SerializeField] private GameObject IndexOff;

        [SerializeField] private PageView pageView;

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => { Find<EventBanner>().Close(true); });
        }
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||


        private IEnumerator Start()
        {
            yield return new WaitUntil(() => NoticeManager.instance.IsInitialized);

            var dataList = NoticeManager.instance.BannerData;
            foreach (var data in dataList)
            {
                if (data.UseDateTime &&
                    !DateTime.UtcNow.IsInTime(data.BeginDateTime, data.EndDateTime))
                {
                    continue;
                }

                var ba = Instantiate(Banner, content);
                ba.GetComponent<EventBannerItem>().Set(data);
            }

            for (var i = 0; i < content.childCount; i++)
            {
                Instantiate(i == 0 ? IndexOn : IndexOff, indexContent);
            }

            var indexImages = new List<Image>();
            for (var i = 0; i < indexContent.childCount; i++)
            {
                indexImages.Add(indexContent.GetChild(i).GetComponent<Image>());
            }

            pageView.Set(content, indexImages);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            priceWidget.UpdateWncgPrice();
            base.Show(ignoreShowAnimation);
        }
    }
}