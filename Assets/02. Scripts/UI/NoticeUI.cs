using DG.Tweening;
using TMPro;
using UnityEngine;

public class NoticeUI : MonoBehaviour
{
    [Header("Notice 관련")]
    public TextMeshProUGUI messageText;
    public CanvasGroup cg;
    public RectTransform panel;

    [Header("Dotween 관련")]
    public float showSeconds = 2f; // 표시 시간
    public float inDuration = 0.5f; // 들어올때
    public float outDuration = 0.4f;
    public Vector2 startOffset = new Vector2(0, 200f);

    Tween currentTween;

    void Awake()
    {
        cg.alpha = 0f;
        panel.anchoredPosition += startOffset; // 시작 위치 offset
    }

    public void Show(string msg, float? seconds = null)
    {
        if (seconds.HasValue) showSeconds = seconds.Value;

        messageText.text = msg;

        // 기존 트윈 정리
        currentTween?.Kill();

        // 시작 상태 초기화
        cg.alpha = 0f;
        panel.anchoredPosition = startOffset;

        gameObject.SetActive(true);

        // In 애니메이션 (슬라이드+페이드)
        currentTween = DOTween.Sequence()
            .Append(cg.DOFade(1f, inDuration))
            .Join(panel.DOAnchorPos(Vector2.zero, inDuration).SetEase(Ease.OutBack))
            .AppendInterval(showSeconds)
            // Out 애니메이션 (슬라이드+페이드 아웃)
            .Append(cg.DOFade(0f, outDuration))
            .Join(panel.DOAnchorPos(startOffset, outDuration).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                gameObject.SetActive(false); // 자동 숨김
            });
    }
}