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
    [SerializeField] private CanvasGroup blockPanelGroup; 
    [SerializeField] private Button backgroundButton;

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
        if (backgroundButton != null)
        {
            backgroundButton.onClick.AddListener(OnClickNoThanks);
        }
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
        if (blockPanelGroup != null)
        {
            blockPanelGroup.gameObject.SetActive(true);
            blockPanelGroup.alpha = 0f; // 투명하게 시작
            blockPanelGroup.blocksRaycasts = true; // 터치 방지 켜기
        }
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.8f;

        // 클릭 방지
        canvasGroup.interactable = false;
        if (backgroundButton != null) backgroundButton.interactable = false;
        // 애니메이션 충돌 방지를 위해 기존 트윈 제거
        KillTweens();
        Sequence seq = DOTween.Sequence();

        // [중요] 메인 팝업을 Append로 먼저 등록 (시간의 기준)
        seq.Append(canvasGroup.DOFade(1f, animDuration));

        // 나머지(스케일, 배경)는 Join으로 동시에 실행
        seq.Join(transform.DOScale(1f, animDuration).SetEase(showEase));
        if (blockPanelGroup != null)
        {
            seq.Join(blockPanelGroup.DOFade(1f, animDuration));
        }

        seq.SetUpdate(true);
        seq.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            if (backgroundButton != null) backgroundButton.interactable = true;
        });
    }

    public void Hide()
    {
        _visible = false;

        // 즉시 클릭 차단
        canvasGroup.interactable = false;
        if (backgroundButton != null) backgroundButton.interactable = false;

        KillTweens();

        Sequence seq = DOTween.Sequence();

        // [핵심 수정] 
        // 1. 팝업 본체가 사라지는 것을 기준(Append)으로 잡습니다.
        seq.Append(canvasGroup.DOFade(0f, animDuration));

        // 2. 스케일 줄어드는 것도 동시에(Join)
        seq.Join(transform.DOScale(0.8f, animDuration).SetEase(hideEase));

        // 3. 배경이 사라지는 것도 동시에(Join) -> 같은 animDuration 사용
        if (blockPanelGroup != null)
        {
            seq.Join(blockPanelGroup.DOFade(0f, animDuration));
        }

        seq.OnComplete(() =>
        {
            if (blockPanelGroup != null) blockPanelGroup.gameObject.SetActive(false);
            gameObject.SetActive(false);
        });
    }

    private void HideImmediate()
    {
        KillTweens();
        _visible = false;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        if (blockPanelGroup != null)
        {
            blockPanelGroup.alpha = 0f;
            blockPanelGroup.gameObject.SetActive(false);
        }
    }

    // 실행 중인 DOTween이 있다면 즉시 종료 (중복 실행 방지)
    private void KillTweens()
    {
        canvasGroup.DOKill();
        transform.DOKill();
        if (blockPanelGroup != null) blockPanelGroup.DOKill();
    }

    private void OnClickWatchAd()
    {
        if (!_visible) return;
        SetButtonsInteractable(false);

        AdsManager.Instance.ShowRewarded(
            onRewardEarned: () =>
            {
                _onAccept?.Invoke();
                Hide();
                SetButtonsInteractable(true);
            },
            onUnavailable: () =>
            {
                SetButtonsInteractable(true);
                Debug.LogWarning("[Ads] 광고 준비 안됨");
            }
        );
    }

    private void OnClickNoThanks()
    {
        _onDecline?.Invoke();
        Hide(); // 애니메이션과 함께 닫기
        ResetButtons();
    }
    private void SetButtonsInteractable(bool interactable)
    {
        watchAdButton.interactable = interactable;
        noThanksButton.interactable = interactable;
        if (backgroundButton != null) backgroundButton.interactable = interactable;
    }
    private void ResetButtons()
    {
        watchAdButton.interactable = true;
        noThanksButton.interactable = true;
    }
}