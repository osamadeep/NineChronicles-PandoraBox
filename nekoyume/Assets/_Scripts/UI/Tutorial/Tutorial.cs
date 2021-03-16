using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Tutorial : Widget
    {
        public override WidgetType WidgetType => WidgetType.TutorialMask;

        [SerializeField] private Button button;
        [SerializeField] private List<ItemContainer> items;
        [SerializeField] private Animator animator;
        [SerializeField] private float playTime = 2;

        private Coroutine _coroutine;
        private System.Action _callback;
        private const int ItemCount = 3;
        private int _playTimeRef;
        private int _finishRef;
        private bool _isPlaying;
        private IDisposable _onClickDispose = null;

        public Button NextButton => button;

        public void Play(List<ITutorialData> datas, int presetId, System.Action callback)
        {
            if(!(_onClickDispose is null))
            {
                _onClickDispose.Dispose();
                _onClickDispose = null;
            }

            _onClickDispose = button.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(playTime))
                .Subscribe(_ => OnClick())
                .AddTo(gameObject);

            if (_isPlaying)
            {
                return;
            }

            _finishRef = 0;
            _isPlaying = true;

            animator.SetTrigger(presetId.ToString());
            RunStopwatch();
            foreach (var data in datas)
            {
                var item = items.FirstOrDefault(x => data.Type == x.Type);
                item?.Item.gameObject.SetActive(true);
                item?.Item.Play(data, () => PlayEnd());
            }
            _callback = callback;
        }

        private void RunStopwatch()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            _coroutine = StartCoroutine(Stopwatch());
        }

        private IEnumerator Stopwatch()
        {
            _playTimeRef = 1;
            yield return new WaitForSeconds(playTime);
            PlayEnd();
        }

        public void Stop(System.Action callback = null)
        {
            _onClickDispose.Dispose();
            _onClickDispose = null;
            _finishRef = 0;
            _playTimeRef = 0;
            _isPlaying = true;
            foreach (var item in items)
            {
                item.Item.Stop(() => PlayEnd(callback));
            }

        }

        private void PlayEnd(System.Action callback = null)
        {
            _finishRef++;
            if (_finishRef >= ItemCount + _playTimeRef)
            {
                _isPlaying = false;
                callback?.Invoke();
            }
        }

        private void OnClick()
        {
            if (_isPlaying)
            {
                return;
            }

            AudioController.instance.PlaySfx(AudioController.SfxCode.Click);
            _callback?.Invoke();
        }
    }

    [Serializable]
    public class ItemContainer
    {
        [SerializeField] private TutorialItemType type;
        [SerializeField] private TutorialItem item;

        public TutorialItemType Type => type;
        public TutorialItem Item => item;
    }
}
