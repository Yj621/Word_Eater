using DG.Tweening;
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

    // 원하는 위치 고정 (anchoredPosition 기준)
    private Vector2 _showPosition = new Vector2(0, 0);       // Y = 0
    private Vector2 _hidePosition = new Vector2(0, -450);    // Y = -480

    [Header("오답 연출")]
    [SerializeField] private Image _damageOverlay;          // 전체 화면 빨간 Image
    [SerializeField] private RectTransform _shakeTarget;    // 흔들 대상 (예: 전체 UI 루트)

    private Vector3 _shakeOriginalPos;

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
        PageIcon.SetActive(true);
        if (!_isKeyboardOpen) return;
        _isKeyboardOpen = false;

        _keyboardRect
            .DOAnchorPos(_hidePosition, _animationDuration)
            .SetEase(Ease.InCirc);
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
