using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ADPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button noThanksButton;

    [Header("Text Refs")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI watchAdLabel;
    [SerializeField] private TextMeshProUGUI noThanksLabel;

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

        var cg = canvasGroup;
        if (cg != null)
        {
            cg.ignoreParentGroups = true;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;
        }

        SetCanvas(true);
    }

    public void Hide()
    {
        _visible = false;
        SetCanvas(false);
    }

    private void HideImmediate()
    {
        _visible = false;
        SetCanvas(false, immediate: true);
    }

    private void SetCanvas(bool show, bool immediate = false)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable = show;
        gameObject.SetActive(show);
    }

    private void OnClickWatchAd()
    {
        if (!_visible) return;

        watchAdButton.interactable = false;
        noThanksButton.interactable = false;

        AdsManager.Instance.ShowRewarded(
            onRewardEarned: () =>
            {
                _onAccept?.Invoke();  // 부활 / 배터리 충전 / 기타 보상
                Hide();
                ResetButtons();
            },
            onUnavailable: () =>
            {
                watchAdButton.interactable = true;
                noThanksButton.interactable = true;
                Debug.LogWarning("[Ads] 광고가 준비되지 않았습니다. 잠시 후 다시 시도해주세요.");
            }
        );
    }

    private void OnClickNoThanks()
    {
        _onDecline?.Invoke();
        Hide();
        ResetButtons();
    }

    private void ResetButtons()
    {
        watchAdButton.interactable = true;
        noThanksButton.interactable = true;
    }
}
