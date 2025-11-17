using DG.Tweening;
using UnityEngine;

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

    public GameObject PageIcon;

    void Start()
    {
        phoneSwiper = GetComponent<PhoneSwiper>();
        PageIcon.SetActive(true);
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
