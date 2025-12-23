using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 개별 알림 배너를 제어하는 클래스임
/// 등장/퇴장 애니메이션과 클릭 이벤트를 처리함
/// </summary>
public class NoticeUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Notice 관련")]
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private CanvasGroup _cg;
    [SerializeField] private RectTransform _panel;

    [Header("버튼")]
    public Button closeButton;     // 닫기 버튼 (옵션임)

    [Header("Dotween 관련")]
    [SerializeField] private float _showSeconds = 2f;    // 표시 지속 시간
    [SerializeField] private float _inDuration = 0.5f;   // 등장 애니메이션 시간
    [SerializeField] private float _outDuration = 0.4f;  // 퇴장 애니메이션 시간
    [SerializeField] private Vector2 _startOffset = new Vector2(0, 200f); // 시작 위치 오프셋

    Tween currentTween;
    NoticeOptions activeOptions;
    bool isShowing;

    void Awake()
    {
        // 초기화함 (투명, 위치 오프셋 적용)
        _cg.alpha = 0f;
        _panel.anchoredPosition += _startOffset;
    }

    /// <summary>
    /// 알림 옵션에 따라 메시지를 표시하고 애니메이션 재생함
    /// </summary>
    public void Show(NoticeOptions options)
    {
        activeOptions = options;
        _messageText.text = options.Message;

        // 초기 상태 설정함
        gameObject.SetActive(true);
        currentTween?.Kill();
        _cg.alpha = 0f;
        _panel.anchoredPosition = _startOffset;
        isShowing = true;

        // 등장 애니메이션 (페이드인 + 내려오기)
        var seq = DOTween.Sequence()
            .Append(_cg.DOFade(1f, _inDuration))
            .Join(_panel.DOAnchorPos(Vector2.zero, _inDuration).SetEase(Ease.OutBack));

        // Auto 모드면 일정 시간 후 자동으로 닫음
        if (options.DismissMode == NoticeDismissMode.Auto)
        {
            seq.AppendInterval(Mathf.Max(0f, options.Duration))
               .Append(_cg.DOFade(0f, _outDuration))
               .Join(_panel.DOAnchorPos(_startOffset, _outDuration).SetEase(Ease.InBack))
               .OnComplete(CompleteClose);
        }

        currentTween = seq;
    }

    /// <summary>
    /// 알림창을 애니메이션과 함께 수동으로 닫음
    /// </summary>
    public void Dismiss()
    {
        if (!isShowing) return;
        currentTween?.Kill();

        // 퇴장 애니메이션 실행함
        currentTween = DOTween.Sequence()
            .Append(_cg.DOFade(0f, _outDuration))
            .Join(_panel.DOAnchorPos(_startOffset, _outDuration).SetEase(Ease.InBack))
            .OnComplete(CompleteClose);
    }

    // 닫기 완료 후 정리 작업 수행함
    void CompleteClose()
    {
        isShowing = false;
        gameObject.SetActive(false);
        var cb = activeOptions?.OnClosed;
        activeOptions = null;
        cb?.Invoke();
    }

    /// <summary>
    /// 클릭 시 알림창 닫기 동작 수행함 (Button 모드일 경우)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isShowing) return;

        // Button 모드일 때만 화면 클릭으로 닫힘
        if (activeOptions != null && activeOptions.DismissMode == NoticeDismissMode.Button)
        {
            Dismiss();
        }
    }

    public bool IsShowing => isShowing;
}