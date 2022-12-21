using System;
using System.Collections.Generic;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class WorldMapStage : MonoBehaviour
    {
        public enum State
        {
            Normal,
            Disabled,
            Hidden
        }

        public class ViewModel : IDisposable
        {
            public readonly StageType stageType;
            public readonly int stageId;
            public readonly bool hasBoss;
            public readonly ReactiveProperty<State> State = new();
            public readonly ReactiveProperty<bool> Selected = new();
            public readonly ReactiveProperty<bool> HasNotification = new(false);

            public ViewModel(StageType stageType, State state)
                : this(stageType, -1, false, state)
            {
            }

            public ViewModel(
                StageType stageType,
                int stageId,
                bool hasBoss,
                State state)
            {
                this.stageType = stageType;
                this.stageId = stageId;
                this.hasBoss = hasBoss;
                State.Value = state;
            }

            public void Dispose()
            {
                State.Dispose();
                Selected.Dispose();
            }
        }

        //|||||||||||||| PANDORA START CODE |||||||||||||||||||
        [Header("PANDORA CUSTOM FIELDS")]
        [SerializeField] private GameObject futureImage;
        [Space(50)]
        //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||

        public float bossScale = 1f;

        [SerializeField]
        private Image normalImage;

        [SerializeField]
        private Image disabledImage;

        [SerializeField]
        private Image selectedImage;

        [SerializeField]
        private Image bossImage;

        [SerializeField]
        private Button button;

        [SerializeField]
        private TextMeshProUGUI buttonText;

        [SerializeField]
        private GameObject hasNotificationImage;

        private Vector3 _normalImageScale;

        private Vector3 _disabledImageScale;

        private Vector3 _selectedImageScale;

        private readonly List<IDisposable> _disposablesForModel = new();

        private Tweener _tweener;

        public readonly Subject<WorldMapStage> onClick = new();

        public ViewModel SharedViewModel { get; private set; }

        private void Awake()
        {
            _normalImageScale = normalImage.transform.localScale;
            _disabledImageScale = disabledImage.transform.localScale;
            _selectedImageScale = selectedImage.transform.localScale;

            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    onClick.OnNext(this);
                }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            SubscribeSelect(SharedViewModel?.Selected.Value ?? false);
        }

        private void OnDisable()
        {
            _tweener?.Kill();
            _tweener = null;
        }

        public void Show(ViewModel viewModel)
        {
            if (viewModel is null)
            {
                Hide();

                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            SharedViewModel = viewModel;
            SharedViewModel.State.Subscribe(SubscribeState).AddTo(_disposablesForModel);
            SharedViewModel.Selected.Subscribe(SubscribeSelect).AddTo(_disposablesForModel);
            SharedViewModel.HasNotification.SubscribeTo(hasNotificationImage).AddTo(_disposablesForModel);
            Set(SharedViewModel.hasBoss);

            buttonText.text = StageInformation.GetStageIdString(
                SharedViewModel.stageType,
                SharedViewModel.stageId);
        }

        public void Hide()
        {
            SharedViewModel.State.Value = State.Hidden;
        }

        private void SubscribeState(State value)
        {
            if (SharedViewModel?.Selected.Value ?? false)
            {
                return;
            }

            //|||||||||||||| PANDORA START CODE |||||||||||||||||||
            futureImage.SetActive(false);
            //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
            switch (value)
            {
                case State.Normal:
                    gameObject.SetActive(true);
                    normalImage.enabled = true;
                    disabledImage.enabled = false;
                    selectedImage.enabled = false;
                    buttonText.color = ColorHelper.HexToColorRGB("FFF9DD");
                    break;
                case State.Disabled:
                    gameObject.SetActive(true);
                    normalImage.enabled = false;
                    disabledImage.enabled = true;
                    selectedImage.enabled = false;
                    //|||||||||||||| PANDORA START CODE |||||||||||||||||||
                    futureImage.SetActive(true);
                    //|||||||||||||| PANDORA  END  CODE |||||||||||||||||||
                    selectedImage.enabled = false;
                    buttonText.color = ColorHelper.HexToColorRGB("666666");
                    break;
                case State.Hidden:
                    gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            if (L10nManager.TryGetFontMaterial(FontMaterialType.ButtonNormal, out var fontMaterial))
            {
                buttonText.fontSharedMaterial = fontMaterial;
            }

            normalImage.SetNativeSize();
        }

        private void SubscribeSelect(bool value)
        {
            _tweener?.Kill();
            _tweener = null;
            transform.localScale = Vector3.one;

            if (!value)
            {
                SubscribeState(SharedViewModel?.State.Value ?? State.Normal);
                return;
            }

            gameObject.SetActive(true);
            normalImage.enabled = false;
            disabledImage.enabled = false;
            selectedImage.enabled = true;
            buttonText.color = ColorHelper.HexToColorRGB("FFF9DD");

            if (L10nManager.TryGetFontMaterial(FontMaterialType.ButtonYellow, out var fontMaterial))
            {
                buttonText.fontSharedMaterial = fontMaterial;
            }

            _tweener = transform
                .DOScale(1.2f, 1f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void Set(bool isBoss)
        {
            var icon = EventManager.GetEventInfo().StageIcon;
            var offset = EventManager.GetEventInfo().StageIconOffset;
            normalImage.sprite = icon;
            normalImage.SetNativeSize();
            normalImage.rectTransform.anchoredPosition = offset;

            disabledImage.sprite = icon;
            disabledImage.SetNativeSize();
            disabledImage.rectTransform.anchoredPosition = offset;
            selectedImage.sprite = icon;
            selectedImage.SetNativeSize();
            selectedImage.rectTransform.anchoredPosition = offset;
            bossImage.enabled = isBoss;
            ResetScale();
            if (isBoss)
            {
                normalImage.transform.localScale *= bossScale;
                disabledImage.transform.localScale *= bossScale;
                selectedImage.transform.localScale *= bossScale;
            }
        }

        private void ResetScale()
        {
            normalImage.transform.localScale = _normalImageScale;
            disabledImage.transform.localScale = _disabledImageScale;
            selectedImage.transform.localScale = _selectedImageScale;
        }
    }
}
