using System.Collections.Generic;
using System;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class JamoChooserUI : MonoBehaviour
{
    [Header("Top Tabs")]
    [SerializeField] private Button btnConsonant;
    [SerializeField] private Button btnVowel;
    // [SerializeField] private Sprite pressBtnSprite;
    // [SerializeField] private Sprite normalBtnSprite;

    [Header("Grid Root (Content)")]
    [SerializeField] private Transform gridRoot; // GridLayoutGroup 달린 오브젝트

    [Header("Button Template")]
    [SerializeField] private Button jamoButtonTemplate; // 자음/모음이 자식으로 들어갈 버튼

    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;          // 확인 패널 전체
    [SerializeField] private TextMeshProUGUI confirmText;      // "ㄱ 을 선택하시겠습니까?" 같은 문구
    [SerializeField] private Button btnConfirmYes;             // 예 버튼
    [SerializeField] private Button btnConfirmNo;              // 아니오 버튼

    public Action<JamoDefsType, string> OnSelected; // 외부에 콜백 제공
    public Action<bool> OnRequestClose; // 외부에 닫기 요청 전달 (true: 창 닫기, false: 배경만 닫기)

    private JamoDefsType _current = JamoDefsType.Consonant;
    private readonly List<Button> _spawned = new();
    private string _pendingJamo;   // 현재 선택 대기 중인 자모


    private void Awake()
    {
        btnConsonant.onClick.AddListener(() => Switch(JamoDefsType.Consonant));
        btnVowel.onClick.AddListener(() => Switch(JamoDefsType.Vowel));

        jamoButtonTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 기본은 자음
        Switch(JamoDefsType.Consonant);
    }

    private void Switch(JamoDefsType type)
    {
        _current = type;
        if (btnConsonant != null) 
            btnConsonant.interactable = (type != JamoDefsType.Consonant);

        if (btnVowel != null) 
            btnVowel.interactable = (type != JamoDefsType.Vowel);
        RebuildGrid(type == JamoDefsType.Consonant ? JamoDefs.Consonants : JamoDefs.Vowels);
    }

    private void RebuildGrid(List<string> list)
    {
        foreach (var b in _spawned) Destroy(b.gameObject);
        _spawned.Clear();

        foreach (var jamo in list)
        {
            var current = jamo; // 캡쳐용 로컬 변수

            var btn = Instantiate(jamoButtonTemplate, gridRoot);
            btn.gameObject.SetActive(true);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = current;

            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[JamoChooserUI] 버튼 클릭: {current}");
                ShowConfirm(current);
            });

            _spawned.Add(btn);

        }
    }

    private void ShowConfirm(string jamo)
    {
        Debug.Log($"[JamoChooserUI] ShowConfirm 호출됨: {jamo}");

        _pendingJamo = jamo;

        if (confirmPanel == null)
        {
            Debug.LogWarning("[JamoChooserUI] confirmPanel == null, 바로 선택 처리");

            // 확인 패널 없으면 기존처럼 바로 선택 처리
            OnSelected?.Invoke(_current, jamo);
            Close();
            return;
        }
        // 열기 연출: 스케일 0 -> 1
        confirmPanel.transform.localScale = Vector3.zero;
        confirmPanel.SetActive(true);
        confirmPanel.transform.SetAsLastSibling();

        if (confirmText != null)
            confirmText.text = $"'{jamo}' 를 선택하시겠습니까?";

        confirmPanel.transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack);

        // 기존 리스너가 중첩되지 않게 먼저 제거
        if (btnConfirmYes != null)
        {
            btnConfirmYes.onClick.RemoveAllListeners();
            btnConfirmYes.onClick.AddListener(() =>
            {
                // 실제로 획득됨
                OnSelected?.Invoke(_current, _pendingJamo);

                // 닫는 연출: 스케일 축소 후 비활성화, 그 다음 외부에 창 닫기 요청 전달
                confirmPanel.transform.DOScale(Vector3.zero, 0.15f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        confirmPanel.SetActive(false);
                        OnRequestClose?.Invoke(true);
                    });
            });
        }

        if (btnConfirmNo != null)
        {
            btnConfirmNo.onClick.RemoveAllListeners();
            btnConfirmNo.onClick.AddListener(() =>
            {
                // 아니오 → 패널 닫는 연출
                confirmPanel.transform.DOScale(Vector3.zero, 0.12f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        confirmPanel.SetActive(false);
                        // 아니오 선택: 배경(예: closePanel)만 닫도록 요청
                        OnRequestClose?.Invoke(false);
                    });
            });
        }
    }


    public void OpenAtScreenPosition(Vector2 screenPos)
    {
        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition = ScreenToCanvasAnchoredPosition(rt, screenPos);
        }
        gameObject.SetActive(true);
    }

    public void Close()
    {
        // 씬에 배치된 경우 재사용을 위해 비활성화 처리
        gameObject.SetActive(false);
    }

    // 화면 좌표를 같은 Canvas의 앵커 좌표로 변환
    private Vector2 ScreenToCanvasAnchoredPosition(RectTransform ui, Vector2 screenPos)
    {
        var canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPoint);
        return localPoint;
    }
}
