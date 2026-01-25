using System.Collections.Generic;
using System;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using WordEater.Systems;

public class JamoChooserUI : MonoBehaviour
{
    [Header("Top Tabs")]
    [SerializeField] private Button btnConsonant;
    [SerializeField] private Button btnVowel;
    // [SerializeField] private Sprite pressBtnSprite;
    // [SerializeField] private Sprite normalBtnSprite;

    [Header("Grid Root (Content)")]
    [SerializeField] private Transform gridRoot; // GridLayoutGroup 달린 오브젝트

    [Header("Background")]
    [SerializeField] private Button btnBackground; // 뒷배경(검정 투명) 버튼 연결

    [Header("Button Template")]
    [SerializeField] private Button jamoButtonTemplate; // 자음/모음이 자식으로 들어갈 버튼

    [Header("Main Content Panel (Jamo Panel)")]
    [SerializeField] private Transform jamoMainPanel; // [추가] Jamo_Panel (뒤로 보낼 대상)

    [Header("Confirm Panel")]
    [SerializeField] private GameObject jamoConfirmPanel;          // 확인 패널 전체
    [SerializeField] private TextMeshProUGUI confirmText;      // "ㄱ 을 선택하시겠습니까?" 같은 문구
    [SerializeField] private Button btnConfirmYes;             // 예 버튼
    [SerializeField] private Button btnConfirmNo;              // 아니오 버튼

    public Action<JamoDefsType, string> OnSelected; // 외부에 콜백 제공
    public Action<bool> OnRequestClose; // 외부에 닫기 요청 전달 (true: 창 닫기, false: 배경만 닫기)
    public Func<string, bool> OnCheckCanReceive; // [추가] 받을 수 있는지 미리 체크 (true면 가능)

    // 아이템 소모 여부 (ClickIconChoiceJamo 등에서 설정)
    public bool consumeAfterPick = false;

    private JamoDefsType _current = JamoDefsType.Consonant;
    private readonly List<Button> _spawned = new();
    private string _pendingJamo;   // 현재 선택 대기 중인 자모


    private void Awake()
    {
        btnConsonant.onClick.AddListener(() => Switch(JamoDefsType.Consonant));
        btnVowel.onClick.AddListener(() => Switch(JamoDefsType.Vowel));

        // 배경 버튼 클릭 시 -> 창 닫기 요청 (true 전달)
        if (btnBackground != null)
        {
            btnBackground.onClick.AddListener(() =>
            {
                SoundManager.Instance.SFXStart(SoundManager.SFXType.button2);
                // false가 아니라 true를 보내야 부모(ClickIconChoiceJamo)가 OnCloseChooser()를 실행합니다.
                OnRequestClose?.Invoke(true);
            });
        }

        jamoButtonTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {// 기본은 자음이지만, 처음 켜질 때는 소리를 내지 않기 위해 Switch를 거치지 않거나
     // 소리가 포함되지 않은 초기화 로직을 수행하는 것이 좋습니다.

        _current = JamoDefsType.Consonant;
        if (btnConsonant != null) btnConsonant.interactable = false;
        if (btnVowel != null) btnVowel.interactable = true;

        RebuildGrid(JamoDefs.Consonants); // Switch 대신 직접 Rebuild 호출
    }

    private void Switch(JamoDefsType type)
    {
        SoundManager.Instance.SFXStart(SoundManager.SFXType.button1);
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
                // Debug.Log($"[JamoChooserUI] 버튼 클릭: {current}");
                ShowConfirm(current);
            });

            _spawned.Add(btn);

        }
    }

    private void ShowConfirm(string jamo)
    {
        // Debug.Log($"[JamoChooserUI] ShowConfirm 호출됨: {jamo}");

        _pendingJamo = jamo;

        if (jamoConfirmPanel == null)
        {
            // Debug.LogWarning("[JamoChooserUI] confirmPanel == null, 바로 선택 처리");

            // 확인 패널 없으면 기존처럼 바로 선택 처리
            OnSelected?.Invoke(_current, jamo);
            Close();
            return;
        }
        // 열기 연출: 스케일 0 -> 1
        jamoConfirmPanel.transform.localScale = Vector3.zero;
        jamoConfirmPanel.SetActive(true);

        // [롤백] Canvas 소팅 방식 제거 (사용자 요청)
        // [수정] 단순히 확인 패널을 맨 앞으로 가져옴 (가장 기본적인 방식)
        jamoConfirmPanel.transform.SetAsLastSibling();

        if (confirmText != null)
            confirmText.text = $"'{jamo}' 를 선택하시겠습니까?";

        jamoConfirmPanel.transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack);

        // 기존 리스너가 중첩되지 않게 먼저 제거
        if (btnConfirmYes != null)
        {
            btnConfirmYes.onClick.RemoveAllListeners();
            btnConfirmYes.onClick.AddListener(() =>
            {
                SoundManager.Instance.SFXStart(SoundManager.SFXType.button1);
                //받을 수 있는 상태인지 먼저 체크
                if (OnCheckCanReceive != null && !OnCheckCanReceive(_pendingJamo))
                {
                    // 토스트 팝업 등으로 "더 이상 가질 수 없습니다" 표시 권장
                    // Debug.LogWarning("가득 차서 받을 수 없습니다.");

                    jamoConfirmPanel.SetActive(false);
                    this.gameObject.SetActive(false);
                    OnRequestClose?.Invoke(false);

                    if (GameManager.Instance != null)
                        GameManager.Instance.CloseBlurPanelsImmediate();

                    if (UIManager.Instance != null) UIManager.Instance.Show("더 이상 가질 수 없습니다!");
                        SoundManager.Instance.SFXStart(SoundManager.SFXType.notice);
                    return;
                }

                // 아이템 소모 시도
                if (consumeAfterPick && ItemManager.Instance != null)
                {
                    // 자음/모음 선택권 소모
                    if (!ItemManager.Instance.TryUseItem(ItemType.JamoSelectionTicket))
                    {
                        // Debug.LogWarning("아이템이 부족하여 사용할 수 없습니다.");
                        GameManager.Instance.CloseBlurPanelsImmediate();

                        jamoConfirmPanel.SetActive(false);
                        OnRequestClose?.Invoke(false);
                        return;
                    }
                }
                // 실제로 획득됨
                OnSelected?.Invoke(_current, _pendingJamo);

                // [추가] 사용 완료 알림 패널 띄우기 (사용자 요청)
                if (UIManager.Instance != null)
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.CloseBlurPanelsImmediate();

                    UIManager.Instance.Show("사용 완료!");
                }

                // 닫는 연출 후
                jamoConfirmPanel.transform.DOScale(Vector3.zero, 0.15f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        jamoConfirmPanel.SetActive(false);

                        // [핵심] 여기서 true를 보내야 JamoChooserUI 전체가 닫힙니다.
                        OnRequestClose?.Invoke(true);
                    });
            });
        }

        if (btnConfirmNo != null)
        {
            btnConfirmNo.onClick.RemoveAllListeners();
            btnConfirmNo.onClick.AddListener(() =>
            {

                SoundManager.Instance.SFXStart(SoundManager.SFXType.button2);
                // 아니오 → 패널 닫는 연출
                jamoConfirmPanel.transform.DOScale(Vector3.zero, 0.12f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        jamoConfirmPanel.SetActive(false);
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
        _OnCloseCleanup(); // [추가] 닫힐 때 복구 로직 실행
    }

    // [추가] 닫을 때 다시 켜줄 타겟 오브젝트 등록 (버튼 프리팹이 사라져도 여기서 처리)
    private GameObject _restoreTarget;

    public void RegisterRestoreTarget(GameObject target)
    {
        _restoreTarget = target;
        if (_restoreTarget != null) _restoreTarget.SetActive(false); // 등록 즉시 끄기
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

    private void _OnCloseCleanup()
    {
        // [복구] 등록된 타겟이 있다면 다시 켜주기
        if (_restoreTarget != null)
        {
            _restoreTarget.SetActive(true);
            _restoreTarget = null;
        }
    }
}
