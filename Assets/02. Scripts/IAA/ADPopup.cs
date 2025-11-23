using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ADPopup : MonoBehaviour
{
    [Header("할당")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button noThanksButton;
    [SerializeField] private GameObject blockPanel;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI watchAdLabel;
    [SerializeField] private TextMeshProUGUI noThanksLabel;

    [Header("애니메이션 설정")]
    [SerializeField] private float animDuration = 0.3f;    // 애니메이션 시간
    [SerializeField] private Ease showEase = Ease.OutBack; // 나타날 때 효과 (통통 튀는 느낌)
    [SerializeField] private Ease hideEase = Ease.InBack;  // 사라질 때 효과

    private Action _onAccept;
    private Action _onDecline;
    private bool _visible;

    private void Awake()
    {
        if (watchAdButton != null) watchAdButton.onClick.AddListener(OnClickWatchAd);
        if (noThanksButton != null) noThanksButton.onClick.AddListener(OnClickNoThanks);
        HideImmediate();
    }

    /// <summary>
    /// 이번 팝업의 용도(부활/배터리 등)에 맞게 문구 설정
    /// </summary>
    public void Configure(string title, string watchAdText, string noThanksText)
    {
        if (titleText != null) titleText.text = title;
        if (watchAdLabel != null) watchAdLabel.text = watchAdText;
        if (noThanksLabel != null) noThanksLabel.text = noThanksText;
    }

    public void Show(Action onAccept, Action onDecline)
    {
        _onAccept = onAccept;
        _onDecline = onDecline;
        _visible = true;

        // 초기화 및 활성화
        blockPanel.SetActive(true);
        gameObject.SetActive(true);

        // 애니메이션 충돌 방지를 위해 기존 트윈 제거
        KillTweens();

        // 시작 상태 설정 (투명하고 조금 작게 시작)
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.8f;

        // 상호작용은 애니메이션이 끝난 후 켜는 것이 안전함 (선택사항)
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        // Canvas Sorting 설정 (기존 로직 유지)
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;
        }

        // DOTween 시퀀스 실행 (페이드 인 + 스케일 업)
        Sequence seq = DOTween.Sequence();

        seq.Append(canvasGroup.DOFade(1f, animDuration)); // 알파값 0 -> 1
        seq.Join(transform.DOScale(1f, animDuration).SetEase(showEase)); // 크기 0.8 -> 1
        seq.SetUpdate(true);
        seq.OnComplete(() =>
        {
            canvasGroup.interactable = true; // 애니메이션 종료 후 버튼 클릭 가능하게
        });
    }

    public void Hide()
    {
        _visible = false;
        canvasGroup.interactable = false; // 클릭 방지

        // 애니메이션 충돌 방지
        KillTweens();

        // DOTween 시퀀스 실행 (페이드 아웃 + 스케일 다운)
        Sequence seq = DOTween.Sequence();

        seq.Append(canvasGroup.DOFade(0f, animDuration)); // 알파값 1 -> 0
        seq.Join(transform.DOScale(0.8f, animDuration).SetEase(hideEase)); // 크기 1 -> 0.8

        // 애니메이션이 '완료된 후'에 비활성화
        seq.OnComplete(() =>
        {
            blockPanel.SetActive(false);
            gameObject.SetActive(false);
        });
    }

    private void HideImmediate()
    {
        KillTweens();
        _visible = false;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        blockPanel.SetActive(false);
    }

    // 실행 중인 DOTween이 있다면 즉시 종료 (중복 실행 방지)
    private void KillTweens()
    {
        canvasGroup.DOKill();
        transform.DOKill();
    }

    private void OnClickWatchAd()
    {
        if (!_visible) return;

        watchAdButton.interactable = false;
        noThanksButton.interactable = false;

        AdsManager.Instance.ShowRewarded(
            onRewardEarned: () =>
            {
                _onAccept?.Invoke();
                Hide(); // 애니메이션과 함께 닫기
                ResetButtons();
            },
            onUnavailable: () =>
            {
                watchAdButton.interactable = true;
                noThanksButton.interactable = true;
                Debug.LogWarning("[Ads] 광고가 준비되지 않았습니다.");
            }
        );
    }

    private void OnClickNoThanks()
    {
        _onDecline?.Invoke();
        Hide(); // 애니메이션과 함께 닫기
        ResetButtons();
    }

    private void ResetButtons()
    {
        watchAdButton.interactable = true;
        noThanksButton.interactable = true;
    }
}