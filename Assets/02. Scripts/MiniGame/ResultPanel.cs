using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ResultPanel : MonoBehaviour
{
    public TextMeshProUGUI ModeText;
    public TextMeshProUGUI ScoreText;

    [Header("UI")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup dim;
    [SerializeField] private Button backgroundBtn;

    [Header("Tween")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.2f;

    bool _opened;
    Action _onClosed;

    void Awake()
    {
        if (!panel) panel = transform as RectTransform;

        if (backgroundBtn)
        {
            backgroundBtn.onClick.RemoveListener(Hide);
            backgroundBtn.onClick.AddListener(Hide);
        }
    }

    public void BindOnClosed(Action onClosed)
    {
        _onClosed = onClosed;
    }

    public void Init(bool isEasyMode, int clearCount)
    {
        if (ModeText) ModeText.text = $"{(isEasyMode ? "이지" : "하드")} 모드";
        if (ScoreText) ScoreText.text = $"{clearCount} 개의 미니게임 클리어!";
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _opened = true;

        DOTween.Kill(panel);
        DOTween.Kill(dim);

        if (dim)
        {
            dim.alpha = 0f;
            dim.blocksRaycasts = true;
            dim.interactable = true;
            dim.DOFade(1f, showDuration)
   .SetEase(Ease.OutCubic)
   .SetUpdate(true);
        }

        if (panel)
        {
            panel.localScale = Vector3.zero;
            panel.DOScale(Vector3.one, showDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void Hide()
    {
        if (!_opened) return;
        _opened = false;

        DOTween.Kill(panel);
        DOTween.Kill(dim);

        if (dim)
        {
            dim.blocksRaycasts = false;
            dim.interactable = false;
            dim.DOFade(0f, hideDuration)
    .SetEase(Ease.InCubic)
    .SetUpdate(true);
        }

        if (panel)
        {
            panel.DOScale(Vector3.zero, hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    _onClosed?.Invoke();
                });
        }
        else
        {
            gameObject.SetActive(false);
            _onClosed?.Invoke();
        }
    }

    void ForceHide()
    {
        _opened = false;
        if (dim)
        {
            dim.alpha = 0f;
            dim.blocksRaycasts = false;
            dim.interactable = false;
        }
        if (panel) panel.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}
