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

    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;          // 확인 패널 전체
    [SerializeField] private TextMeshProUGUI confirmText;      // "ㄱ 을 선택하시겠습니까?" 같은 문구
    [SerializeField] private Button btnConfirmYes;             // 예 버튼
    [SerializeField] private Button btnConfirmNo;              // 아니오 버튼

    public Action<JamoDefsType, string> OnSelected; // 외부에 콜백 제공

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

    public void InitConfirmPanel(GameObject panel)
    {
        confirmPanel = panel;

        if (confirmPanel == null)
        {
            Debug.LogError("[JamoChooserUI] InitConfirmPanel에 null이 넘어왔습니다.");
            return;
        }

        // 자식들 찾아서 캐싱
        confirmText = confirmPanel.transform.Find("Confirm_Text")
                                            .GetComponent<TextMeshProUGUI>();
        btnConfirmYes = confirmPanel.transform.Find("Button_Group/Yes_Button")
                                              .GetComponent<Button>();
        btnConfirmNo = confirmPanel.transform.Find("Button_Group/No_Button")
                                              .GetComponent<Button>();

        // 시작할 때는 항상 꺼두기
        confirmPanel.SetActive(false);

        // 혹시 이전 리스너가 남아 있을 수 있으니 정리
        btnConfirmYes.onClick.RemoveAllListeners();
        btnConfirmNo.onClick.RemoveAllListeners();

        // 여기서는 공통 동작만 넣고, 실제 Yes/No 동작은 ShowConfirm에서 다시 설정
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

        confirmPanel.SetActive(true);
        Debug.Log($"[JamoChooserUI] confirmPanel activeSelf={confirmPanel.activeSelf}, inHierarchy={confirmPanel.activeInHierarchy}");


        if (confirmText != null)
        {
            confirmText.text = $"'{jamo}' 를 선택하시겠습니까?";
        }

        // 기존 리스너가 중첩되지 않게 먼저 제거
        if (btnConfirmYes != null)
        {
            btnConfirmYes.onClick.RemoveAllListeners();
            btnConfirmYes.onClick.AddListener(() =>
            {
                // 여기서 실제로 획득됨 (외부에서 OnSelected에 JamoInventory.Add 연결해두면 됨)
                OnSelected?.Invoke(_current, _pendingJamo);
                confirmPanel.SetActive(false);
                Close();
            });
        }

        if (btnConfirmNo != null)
        {
            btnConfirmNo.onClick.RemoveAllListeners();
            btnConfirmNo.onClick.AddListener(() =>
            {
                // 아니오 → 패널만 닫고 선택창은 그대로 유지
                confirmPanel.SetActive(false);
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
