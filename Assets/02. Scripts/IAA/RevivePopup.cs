using System;
using UnityEngine;
using UnityEngine.UI;

public class RevivePopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button noThanksButton;
    [SerializeField] private GameObject spinner; // 로딩 표시용

    private Action _onAccept;
    private Action _onDecline;
    private bool _visible;

    private void Awake()
    {
        if (watchAdButton != null) watchAdButton.onClick.AddListener(OnClickWatchAd);
        if (noThanksButton != null) noThanksButton.onClick.AddListener(OnClickNoThanks);
        HideImmediate();
    }

    public void Show(Action onAccept, Action onDecline)
    {
        _onAccept = onAccept;
        _onDecline = onDecline;
        _visible = true;

        if (spinner != null) spinner.SetActive(false);

        // 팝업은 UnscaledTime 기반으로 동작 (애니메이션/타이머 쓴다면)
        var cg = canvasGroup;
        if (cg != null)
        {
            cg.ignoreParentGroups = true;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        // 최상단 보장(없다면 추가)
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

        if (spinner != null) spinner.SetActive(true);
        watchAdButton.interactable = false;
        noThanksButton.interactable = false;

        // 광고 표시
        AdsManager.I.ShowReviveAd(
            onRevive: () =>
            {
                // 광고 보상 수령 → 부활 승인
                _onAccept?.Invoke();
                Hide();
                ResetButtons();
            },
            onFail: () =>
            {
                // 광고가 준비 안됨/실패 → 안내 후 버튼 복구
                if (spinner != null) spinner.SetActive(false);
                watchAdButton.interactable = true;
                noThanksButton.interactable = true;
                Debug.LogWarning("[Revive] 광고가 준비되지 않았습니다. 잠시 후 다시 시도해주세요.");
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
        if (spinner != null) spinner.SetActive(false);
        watchAdButton.interactable = true;
        noThanksButton.interactable = true;
    }
}
