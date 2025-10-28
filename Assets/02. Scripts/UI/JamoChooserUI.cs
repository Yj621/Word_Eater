using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JamoChooserUI : MonoBehaviour
{
    [Header("Top Tabs")]
    [SerializeField] private Button btnConsonant;
    [SerializeField] private Button btnVowel;

    [Header("Grid Root (Content)")]
    [SerializeField] private Transform gridRoot; // GridLayoutGroup 달린 오브젝트

    [Header("Button Template")]
    [SerializeField] private Button jamoButtonTemplate; // 자음/모음이 자식으로 들어갈 버튼

    public Action<JamoDefsType, string> OnSelected; // 외부에 콜백 제공

    private JamoDefsType _current = JamoDefsType.Consonant;
    private readonly List<Button> _spawned = new();

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
        RebuildGrid(type == JamoDefsType.Consonant ? JamoDefs.Consonants : JamoDefs.Vowels);
    }

    private void RebuildGrid(List<string> list)
    {
        foreach (var b in _spawned) Destroy(b.gameObject);
        _spawned.Clear();

        foreach (var jamo in list)
        {
            var btn = Instantiate(jamoButtonTemplate, gridRoot);
            btn.gameObject.SetActive(true);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = jamo;

            btn.onClick.AddListener(() =>
            {
                OnSelected?.Invoke(_current, jamo);
                Close();
            });

            _spawned.Add(btn);
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
        Destroy(gameObject);
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
