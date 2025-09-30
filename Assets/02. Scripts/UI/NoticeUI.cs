using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoticeUI : MonoBehaviour
{
    [Header("Notice 관련")]
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private CanvasGroup _cg;
    [SerializeField] private RectTransform _panel;

    [Header("버튼")]
    public Button closeButton;     // X 버튼(없으면 Inspector에서 null로 둬도 됨)

    [Header("Dotween 관련")]
    [SerializeField] private float _showSeconds = 2f; // 표시 시간
    [SerializeField] private float _inDuration = 0.5f; // 들어올때
    [SerializeField] private float _outDuration = 0.4f;
    [SerializeField] private Vector2 _startOffset = new Vector2(0, 200f);

    Tween currentTween;
    NoticeOptions activeOptions;
    bool isShowing;

    void Awake()
    {
        _cg.alpha = 0f;
        _panel.anchoredPosition += _startOffset;

        if (closeButton != null) closeButton.onClick.AddListener(Dismiss);
    }


    public void Show(NoticeOptions options)
    {
        activeOptions = options;
        _messageText.text = options.Message;

        // 버튼 가시성
        if (closeButton != null)
            closeButton.gameObject.SetActive(options.DismissMode == NoticeDismissMode.Button);

        // 초기 상태
        gameObject.SetActive(true);
        currentTween?.Kill();
        _cg.alpha = 0f;
        _panel.anchoredPosition = _startOffset;
        isShowing = true;

        // In
        var seq = DOTween.Sequence()
            .Append(_cg.DOFade(1f, _inDuration))
            .Join(_panel.DOAnchorPos(Vector2.zero, _inDuration).SetEase(Ease.OutBack));

        // Auto면 대기 후 Out
        if (options.DismissMode == NoticeDismissMode.Auto)
        {
            seq.AppendInterval(Mathf.Max(0f, options.Duration))
               .Append(_cg.DOFade(0f, _outDuration))
               .Join(_panel.DOAnchorPos(_startOffset, _outDuration).SetEase(Ease.InBack))
               .OnComplete(CompleteClose);
        }

        currentTween = seq;
    }

    // 외부/버튼/탭으로 닫기
    public void Dismiss()
    {
        if (!isShowing) return;
        currentTween?.Kill();
        currentTween = DOTween.Sequence()
            .Append(_cg.DOFade(0f, _outDuration))
            .Join(_panel.DOAnchorPos(_startOffset, _outDuration).SetEase(Ease.InBack))
            .OnComplete(CompleteClose);
    }

    void CompleteClose()
    {
        isShowing = false;
        gameObject.SetActive(false);
        var cb = activeOptions?.OnClosed;
        activeOptions = null;
        cb?.Invoke();
    }

    public bool IsShowing => isShowing;

}