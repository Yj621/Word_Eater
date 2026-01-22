using System;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordEater.Core;
using WordEater.Systems;

/// <summary>
/// 게임 내 모든 UI(팝업, 알림, 키보드 패널 등)를 총괄하는 매니저임
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Keyborad UI")]
    [SerializeField] private RectTransform _keyboardRect; // 키보드 UI 패널
    private float _animationDuration = 0.5f;              // 애니메이션 시간

    private bool _isKeyboardOpen = false;                 // 키보드 열림 상태 플래그
    public GameObject InteractPanel;
    PhoneSwiper phoneSwiper;
    public KeyBoardManager KeyBoardManager;
    public GameObject PageIcon;                           // 페이지 아이콘 (키보드 열리면 숨김)

    [Header("오답 연출")]
    [SerializeField] private Image _damageOverlay;        // 화면 붉어짐 효과 이미지
    [SerializeField] private RectTransform _shakeTarget;  // 흔들림 효과 대상

    [Header("상단 알림")]
    [SerializeField] private RectTransform _alarmPanel;       // 알림창 패널
    [SerializeField] private TextMeshProUGUI _alarmTitleText; // 알림 제목
    [SerializeField] private TextMeshProUGUI _alarmText;      // 알림 내용
    [SerializeField] private Image _alarmIconImage;           // 알림 아이콘
    [SerializeField] private float _alarmShowPosY = -150f;    // 알림 표시 Y좌표
    [SerializeField] private float _alarmHidePosY = 150f;     // 알림 숨김 Y좌표
    private Sprite _defaultAlarmSprite;                       // 기본 아이콘 저장용

    [Header("UI 연결")]
    [SerializeField] private GameObject _offLineRewardPanel;   // 배터리 팝업 부모 패널
    [SerializeField] private Transform _t_OffLineReward;       // 팝업 본체 (애니메이션용)
    [SerializeField] private TextMeshProUGUI _messageText;     // 메시지 표시 텍스트
    [SerializeField] private Button _offLineRewardButton;      // 확인 버튼

    [Header("아이템 사용 확인 팝업")]
    [SerializeField] private GameObject _itemBuyConfirmPanel;         // 확인 팝업 패널
    [SerializeField] private TextMeshProUGUI _itemTitleText;       // 팝업 제목
    [SerializeField] private TextMeshProUGUI _itemExplanText;      // 팝업 설명
    [SerializeField] private Button _btnYes;                   // 예 버튼
    [SerializeField] private Button _btnNo;                    // 아니오 버튼
    [SerializeField] private Image _itemImg;                   // 아이콘 이미지
    private Action _onConfirmCallback;                         // 팝업 닫힘 콜백

    private Vector3 _shakeOriginalPos;                         // 흔들림 원위치 저장용

    private Vector2 _showPosition = new Vector2(0, 0);         // 키보드 보임 위치
    private Vector2 _hidePosition = new Vector2(0, -450);      // 키보드 숨김 위치

    [SerializeField] private SlideManager slidemanager;

    public static UIManager Instance;

    private void Awake()
    {
        Instance = this;
        phoneSwiper = GetComponent<PhoneSwiper>();
        // 씬 시작 시 배터리 팝업 숨김 처리함
        InteractPanel.SetActive(false);

        if (_offLineRewardPanel != null)
            _offLineRewardPanel.SetActive(false);

        // 배터리 팝업 버튼 리스너 연결함
        if (_offLineRewardButton != null)
            _offLineRewardButton.onClick.AddListener(OnConfirmClicked);

        // 기본 알림 아이콘 저장함
        if (_alarmIconImage != null)
        {
            _defaultAlarmSprite = _alarmIconImage.sprite;
        }
    }

    void Start()
    {

        // 흔들림 효과 기준 위치 저장함
        if (_shakeTarget != null)
            _shakeOriginalPos = _shakeTarget.localPosition;

        // 데미지 오버레이 투명하게 초기화함
        if (_damageOverlay != null)
        {
            var c = _damageOverlay.color;
            c.a = 0f;
            _damageOverlay.color = c;
        }
    }

    /// <summary>
    /// 배터리 충전 팝업을 애니메이션과 함께 띄움
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    /// <param name="onClose">닫힐 때 실행할 콜백</param>
    public void Show(string message, Action onClose = null)
    {
        if (_messageText != null) _messageText.text = message;
        _onConfirmCallback = onClose;

        if (_offLineRewardPanel != null)
        {
            _offLineRewardPanel.SetActive(true);

            Transform target = _t_OffLineReward != null ? _t_OffLineReward : _offLineRewardPanel.transform;

            // 크기 0에서 시작해 튀어오르는 연출 적용함
            target.localScale = Vector3.zero;
            target.DOKill();
            target.DOScale(1f, 0.4f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true); // 일시정지 상태에서도 동작하게 함
            phoneSwiper.isUsingTab = true;
        }
    }

    public void ReviveTicket()
    {
        ItemManager.Instance.AddItem(ItemType.ReviveTicket, 1);
    }


    /// <summary>
    /// 상단 긴급 알림창을 표시함 아이콘 지정 가능함
    /// </summary>
    public void ShowEmergencyAlarm(string title, string message, float duration, Action onComplete = null, Sprite icon = null)
    {
        if (_alarmPanel == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 제목 및 내용 설정함
        if (_alarmTitleText != null) _alarmTitleText.text = title;
        if (_alarmText != null) _alarmText.text = message;

        // 아이콘 교체 로직임
        if (_alarmIconImage != null)
        {
            if (icon != null)
            {
                _alarmIconImage.sprite = icon;
            }
            else
            {
                _alarmIconImage.sprite = _defaultAlarmSprite;
            }
            _alarmIconImage.gameObject.SetActive(true);
        }

        // 알림창 등장 및 퇴장 애니메이션 시퀀스 실행함
        _alarmPanel.anchoredPosition = new Vector2(_alarmPanel.anchoredPosition.x, _alarmHidePosY);
        _alarmPanel.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // 내려옴
        seq.Append(_alarmPanel.DOAnchorPosY(_alarmShowPosY, 0.5f).SetEase(Ease.OutBack));

        // 대기함
        seq.AppendInterval(duration);

        // 올라감
        seq.Append(_alarmPanel.DOAnchorPosY(_alarmHidePosY, 0.3f).SetEase(Ease.InBack));

        // 완료 후 비활성화함
        seq.OnComplete(() =>
        {
            _alarmPanel.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 배터리 팝업 닫기 버튼 클릭 시 애니메이션 처리함
    /// </summary>
    private void OnConfirmClicked()
    {
        Transform target = _t_OffLineReward != null ? _t_OffLineReward : _offLineRewardPanel.transform;

        // 작아지면서 사라지는 연출 실행함
        target.DOKill();
        target.DOScale(0f, 0.25f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (_offLineRewardPanel != null) _offLineRewardPanel.SetActive(false);

                _onConfirmCallback?.Invoke();
                _onConfirmCallback = null;
                phoneSwiper.isUsingTab = false;
            });
    }

    /// <summary>
    /// 아이템 사용 여부를 묻는 확인 팝업을 띄움
    /// </summary>
    public void ShowConfirmPopup(string title, string message, Action onYes, Action onNo, Sprite itemIcon = null)
    {
        if (_itemBuyConfirmPanel == null)
        {
            Debug.LogError("Confirm Panel 미할당됨");
            onNo?.Invoke();
            return;
        }

        _itemBuyConfirmPanel.SetActive(true);

        if (_itemTitleText != null) _itemTitleText.text = title;
        if (_itemExplanText != null) _itemExplanText.text = message;

        void ClosePanelWithAnimation(Action onCompleteAction)
        {
            // 버튼 중복 클릭 방지를 위해 잠시 인터랙션 끄기 (선택 사항)
            if (_btnYes != null) _btnYes.interactable = false;
            if (_btnNo != null) _btnNo.interactable = false;

            // 크기를 0으로 줄이는 애니메이션 (0.2초)
            _itemBuyConfirmPanel.transform.DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .SetUpdate(true) // 타임스케일 0일 때도 작동하도록
                .OnComplete(() =>
                {
                    _itemBuyConfirmPanel.SetActive(false);

                    // 버튼 인터랙션 복구
                    if (_btnYes != null) _btnYes.interactable = true;
                    if (_btnNo != null) _btnNo.interactable = true;

                    // 애니메이션이 다 끝난 뒤에 실제 기능(Action) 실행
                    onCompleteAction?.Invoke();
                });
        }

        // 예 버튼 이벤트 연결
        if (_btnYes != null)
        {
            _btnYes.onClick.RemoveAllListeners();
            _btnYes.onClick.AddListener(() =>
            {
                // '예'를 눌렀을 때도 부드럽게 닫히게 하려면 이 함수 사용
                ClosePanelWithAnimation(onYes);
            });
        }

        // 아니오 버튼 이벤트 연결 (여기가 질문하신 부분!)
        if (_btnNo != null)
        {
            _btnNo.onClick.RemoveAllListeners();
            _btnNo.onClick.AddListener(() =>
            {
                //그냥 끄지 않고 애니메이션 함수 호출
                ClosePanelWithAnimation(onNo);
            });
        }

        // 아이템 아이콘 표시 여부 처리함
        if (_itemImg != null)
        {
            if (itemIcon != null)
            {
                _itemImg.sprite = itemIcon;
                _itemImg.gameObject.SetActive(true);
            }
            else
            {
                _itemImg.gameObject.SetActive(false);
            }
        }

        // 팝업 등장 애니메이션 실행함
        _itemBuyConfirmPanel.transform.localScale = Vector3.zero;
        _itemBuyConfirmPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    /// <summary>
    /// 오답 시 화면 붉어짐 및 흔들림 효과를 재생함
    /// </summary>
    private void PlayMistakeFx()
    {
        // 붉은색 플래시 효과 줌
        if (_damageOverlay != null)
        {
            _damageOverlay.DOKill();
            var c = _damageOverlay.color;
            c.a = 0f;
            _damageOverlay.color = c;

            _damageOverlay
                .DOFade(0.5f, 0.08f)
                .OnComplete(() =>
                {
                    _damageOverlay.DOFade(0f, 0.25f);
                });
        }

        // 화면 흔들기 효과 줌
        if (_shakeTarget != null)
        {
            _shakeTarget.DOKill();
            _shakeTarget.localPosition = _shakeOriginalPos;

            _shakeTarget
                .DOShakeAnchorPos(
                    duration: 0.25f,
                    strength: 35f,
                    vibrato: 20,
                    randomness: 90f
                )
                .OnComplete(() =>
                {
                    _shakeTarget.localPosition = _shakeOriginalPos;
                });
        }
    }

    // 이벤트 구독 및 해제함
    private void OnEnable()
    {
        GameEvents.OnMistakeHit += PlayMistakeFx;
    }

    private void OnDisable()
    {
        GameEvents.OnMistakeHit -= PlayMistakeFx;
    }


    /// <summary>
    /// 키보드 패널을 열거나 닫음
    /// </summary>
    public void ToggleKeyboard()
    {
        if (_isKeyboardOpen)
        {
            phoneSwiper.isUsingTab = false;
            InteractPanel.SetActive(false);
            CloseKeyboard();
        }
        else
        {
            phoneSwiper.isUsingTab = true;
            InteractPanel.SetActive(true);
            OpenKeyboard();
        }
    }

    /// <summary>
    /// 키보드 패널을 애니메이션과 함께 엶
    /// </summary>
    public void OpenKeyboard()
    {
        slidemanager.isOK = false;
        SoundManager.Instance.SFXStart(SoundManager.SFXType.keyboardOpen);

        PageIcon.SetActive(false);
        if (_isKeyboardOpen) return;
        _isKeyboardOpen = true;

        _keyboardRect
            .DOAnchorPos(_showPosition, _animationDuration)
            .SetEase(Ease.OutCirc);
    }

    /// <summary>
    /// 키보드 패널을 애니메이션과 함께 닫음
    /// </summary>
    public void CloseKeyboard()
    {
        slidemanager.isOK = true;

        SoundManager.Instance.SFXStart(SoundManager.SFXType.keyboardClose);

        // [수정] 키보드 닫을 때 스와이프 잠금 해제
        if (phoneSwiper != null) phoneSwiper.isUsingTab = false;

        KeyBoardManager.ClosePanelAndRestore();
        PageIcon.SetActive(true);
        if (!_isKeyboardOpen) return;
        _isKeyboardOpen = false;

        _keyboardRect
            .DOAnchorPos(_hidePosition, _animationDuration)
            .SetEase(Ease.InCirc)
            .SetUpdate(true);
    }


    public void OnClickResetButton()
    {
        // 데이터 삭제
        FileManager.Instance.ClearAllData();

        // 씬 재시작 (이때 GameManager.Start가 다시 실행되면서 패널을 켤지 말지 결정함)
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}