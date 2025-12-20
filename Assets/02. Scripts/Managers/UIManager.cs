using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordEater.Core;

public class UIManager : MonoBehaviour
{
    [Header("Keyborad UI")]
    [SerializeField] private RectTransform _keyboardRect; // 키보드 UI의 RectTransform
    private float _animationDuration = 0.5f; // 애니메이션 지속 시간

    private bool _isKeyboardOpen = false; // 키보드 상태 추적 변수
    PhoneSwiper phoneSwiper;
    public KeyBoardManager KeyBoardManager;
    public GameObject PageIcon;
    // 원하는 위치 고정 (anchoredPosition 기준)

    [Header("오답 연출")]
    [SerializeField] private Image _damageOverlay;          // 전체 화면 빨간 Image
    [SerializeField] private RectTransform _shakeTarget;    // 흔들 대상 (예: 전체 UI 루트)

    [Header("상단 알림")]
    [SerializeField] private RectTransform _alarmPanel;       // 알림창 패널 (상단에 위치)
    [SerializeField] private TextMeshProUGUI _alarmTitleText;  
    [SerializeField] private TextMeshProUGUI _alarmText;      // 알림창 텍스트
    [SerializeField] private Image _alarmIconImage;
    [SerializeField] private float _alarmShowPosY = -150f;    // 화면 안으로 들어왔을 때 Y좌표 (예: -150)
    [SerializeField] private float _alarmHidePosY = 150f;     // 화면 밖으로 나갔을 때 Y좌표 (예: 150)

    [Header("UI 연결")]
    [SerializeField] private GameObject _batteryChargePanel;    // 팝업 전체 부모 (Panel)
    [SerializeField] private Transform _t_BatteryCharge;  // 실제 튀어오를 팝업 창 (배경 제외)
    [SerializeField] private TextMeshProUGUI _messageText; // 메시지 텍스트
    [SerializeField] private Button _batteryChargeButton;      // 확인(닫기) 버튼

    [Header("아이템 사용 확인 팝업")]
    [SerializeField] private GameObject _confirmPanel;       // 팝업 전체 패널
    [SerializeField] private TextMeshProUGUI _titleText;     // 제목 텍스트
    [SerializeField] private TextMeshProUGUI _explanText;    // 내용 텍스트
    [SerializeField] private Button _btnYes;                 // '네' 버튼
    [SerializeField] private Button _btnNo;                  // '아니오' 버튼
    [SerializeField] private Image _itemImg;
    private Action _onConfirmCallback; // 확인 버튼 눌렀을 때 실행할 추가 로직(옵션)

    private Vector3 _shakeOriginalPos;

    private Vector2 _showPosition = new Vector2(0, 0);
    private Vector2 _hidePosition = new Vector2(0, -450);

    public static UIManager Instance;


    private void Awake()
    {
        Instance = this;
        // 씬 시작 시 팝업 숨기기
        if (_batteryChargePanel != null)
            _batteryChargePanel.SetActive(false);

        // 버튼 리스너 연결
        if (_batteryChargeButton != null)
            _batteryChargeButton.onClick.AddListener(OnConfirmClicked);
    }
    void Start()
    {
        phoneSwiper = GetComponent<PhoneSwiper>();

        // 흔들 기준 위치 저장
        if (_shakeTarget != null)
            _shakeOriginalPos = _shakeTarget.localPosition;

        // 오버레이 알파 0으로 초기화
        if (_damageOverlay != null)
        {
            var c = _damageOverlay.color;
            c.a = 0f;
            _damageOverlay.color = c;
        }
    }

    /// <summary>
    /// 배터리 충전 팝업 띄우기
    /// </summary>
    /// <param name="message">보여줄 메시지</param>
    /// <param name="onClose">닫힌 뒤 실행할 로직 (없으면 null)</param>
    public void Show(string message, Action onClose = null)
    {
        if (_messageText != null) _messageText.text = message;
        _onConfirmCallback = onClose;

        if (_batteryChargePanel != null)
        {
            _batteryChargePanel.SetActive(true);

            // 애니메이션 대상 설정 (popupContainer가 없으면 _batteryChargePanel 자체를 애니메이션)
            Transform target = _t_BatteryCharge != null ? _t_BatteryCharge : _batteryChargePanel.transform;

            // 크기를 0으로 초기화
            target.localScale = Vector3.zero;

            // 0 -> 1로 커지면서 튀어오르는 연출 (OutBack)
            target.DOKill();
            target.DOScale(1f, 0.4f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true); // TimeScale이 0이어도 동작하게 함
        }
    }

    /// <summary>
    /// 상단 알림창을 띄우는 메서드
    /// Sprite icon을 전달하면 아이콘을 표시하고, 전달하지 않으면 숨김
    /// </summary>
    public void ShowEmergencyAlarm(string title, string message, float duration, Action onComplete = null, Sprite icon = null)
    {
        if (_alarmPanel == null)
        {
            onComplete?.Invoke();
            return;
        }

        //제목 설정
        if (_alarmTitleText != null) _alarmTitleText.text = title;

        // 텍스트 설정
        if (_alarmText != null) _alarmText.text = message;

        //  아이콘 설정
        if (_alarmIconImage != null)
        {
            if (icon != null)
            {
                _alarmIconImage.sprite = icon;
                _alarmIconImage.gameObject.SetActive(true); // 아이콘이 있으면 켜기
            }
            else
            {
                // 아이콘이 없으면(null) 끄기 (WordEater 사망 시 등)
                _alarmIconImage.gameObject.SetActive(false);
            }
        }

        // 연출 로직 (기존과 동일)
        _alarmPanel.anchoredPosition = new Vector2(_alarmPanel.anchoredPosition.x, _alarmHidePosY);
        _alarmPanel.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // 내려오기
        seq.Append(_alarmPanel.DOAnchorPosY(_alarmShowPosY, 0.5f).SetEase(Ease.OutBack));

        // 대기
        seq.AppendInterval(duration);

        // 올라가기
        seq.Append(_alarmPanel.DOAnchorPosY(_alarmHidePosY, 0.3f).SetEase(Ease.InBack));

        // 완료 콜백
        seq.OnComplete(() =>
        {
            _alarmPanel.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 오프라인 배터리 충전 팝업 닫기 버튼 로직
    /// </summary>
    private void OnConfirmClicked()
    {
        Transform target = _t_BatteryCharge != null ? _t_BatteryCharge : _batteryChargePanel.transform;

        // 닫을 때는 작아지면서 사라짐 (InBack)
        target.DOKill();
        target.DOScale(0f, 0.25f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // 애니메이션 끝난 후 비활성화
                if (_batteryChargePanel != null) _batteryChargePanel.SetActive(false);

                _onConfirmCallback?.Invoke();
                _onConfirmCallback = null;
            });
    }

    // 아이템 사용 팝업
    public void ShowConfirmPopup(string title, string message, Action onYes, Action onNo, Sprite itemIcon = null)
    {
        if (_confirmPanel == null)
        {
            Debug.LogError("Confirm Panel이 UIManager에 할당되지 않았습니다!");
            // UI가 없으면 일단 '아니오' 처리 (게임 진행 막힘 방지)
            onNo?.Invoke();
            return;
        }

        _confirmPanel.SetActive(true);

        // 텍스트 세팅
        if (_titleText != null) _titleText.text = title;
        if (_explanText != null) _explanText.text = message;

        // 기존 연결된 이벤트 제거 (RemoveAllListeners) 후 새 이벤트 연결
        if (_btnYes != null)
        {
            _btnYes.onClick.RemoveAllListeners();
            _btnYes.onClick.AddListener(() =>
            {
                _confirmPanel.SetActive(false); // 창 닫기
                onYes?.Invoke();               // '네' 로직 실행
            });
        }

        if (_btnNo != null)
        {
            _btnNo.onClick.RemoveAllListeners();
            _btnNo.onClick.AddListener(() =>
            {
                _confirmPanel.SetActive(false); // 창 닫기
                onNo?.Invoke();                // '아니오' 로직 실행
            });
        }

        if (_itemImg != null)
        {
            if (itemIcon != null)
            {
                _itemImg.sprite = itemIcon;     // 스프라이트 교체
                _itemImg.gameObject.SetActive(true); // 이미지 켜기
            }
            else
            {
                _itemImg.gameObject.SetActive(false); // 이미지가 없으면 끄기
            }
        }

        // 등장 연출 (선택사항)
        _confirmPanel.transform.localScale = Vector3.zero;
        _confirmPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void PlayMistakeFx()
    {
        // 빨간 플래시
        if (_damageOverlay != null)
        {
            _damageOverlay.DOKill();
            var c = _damageOverlay.color;
            c.a = 0f;
            _damageOverlay.color = c;

            _damageOverlay
                .DOFade(0.5f, 0.08f)   // 빠르게 반투명 빨강
                .OnComplete(() =>
                {
                    _damageOverlay.DOFade(0f, 0.25f); // 부드럽게 사라짐
                });
        }

        // 화면 흔들기
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
    private void OnEnable()
    {
        GameEvents.OnMistakeHit += PlayMistakeFx;
    }

    private void OnDisable()
    {
        GameEvents.OnMistakeHit -= PlayMistakeFx;
    }


    /// <summary>
    /// 키보드 상태를 토글하는 함수
    /// </summary>
    public void ToggleKeyboard()
    {
        if (_isKeyboardOpen)
        {
            phoneSwiper.isUsingTab = false;
            CloseKeyboard();
        }
        else
        {
            phoneSwiper.isUsingTab = true;
            OpenKeyboard();
        }
    }

    /// <summary>
    /// 키보드를 펴는 함수
    /// </summary>
    public void OpenKeyboard()
    {
        PageIcon.SetActive(false);
        if (_isKeyboardOpen) return;
        _isKeyboardOpen = true;

        _keyboardRect
            .DOAnchorPos(_showPosition, _animationDuration)
            .SetEase(Ease.OutCirc);
    }

    /// <summary>
    /// 키보드를 접는 함수
    /// </summary>
    public void CloseKeyboard()
    {
        KeyBoardManager.ClosePanelAndRestore();
        PageIcon.SetActive(true);
        if (!_isKeyboardOpen) return;
        _isKeyboardOpen = false;

        _keyboardRect
            .DOAnchorPos(_hidePosition, _animationDuration)
            .SetEase(Ease.InCirc)
            .SetUpdate(true); // 타임 스케일 무시
    }

    public void Test_PopUp()
    {
        NoticeManager.Instance.ShowTimed("3초뒤 닫힘", 3f);
    }
    public void Test_PopUp2()
    {
        NoticeManager.Instance.ShowSticky("X버튼을 눌러야 닫힘");
    }
    public void Test_PopUp3()
    {
        var handle = NoticeManager.Instance.ShowManual("사용자가 임의로 닫으면 안됨");
        // 작업 진행 후 닫는 요청 보내기 (현재는 바로 닫아서 실행이 안 되는것 처럼 보임)
        handle.Dismiss();
    }
}
