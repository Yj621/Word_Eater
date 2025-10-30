using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class KeyBoardManager : MonoBehaviour
{
    [Header("버튼")]
    public Button[] SingleWordButtons; // 단일 글자 키
    public Button[] DoubleWordButtons; // 쌍자/쌍모음 키

    [Header("소환 프리팹")]
    public GameObject[] SingleWords;
    public GameObject[] DSWords;
    public GameObject[] DDWords;

    [Header("표시 라벨(TMP)")]
    public TextMeshProUGUI[] DoubleText;

    [Header("입력 상태")]
    public bool isShiftPressed = false;
    public LongPressKey[] longPressKeys; // 입력 기록용 (없어도 됨)
    public int DefaultCount = 2;
    
    [Header("UI/World 스폰 설정")]
    public Canvas targetCanvas;       // UI 드래그용 Canvas (없으면 버튼의 Canvas를 자동 탐색)
    public RectTransform uiSpawnRoot; // UI 프리팹을 붙일 최상위(없으면 캔버스의 root RectTransform)
    public Camera uiCamera;           // Screen Space - Overlay면 null 가능
    public float worldDepth = 10f;    // 월드 드래그 시 카메라로부터의 Z
    public Vector3 spawnOffset;       // 초기 스폰 오프셋
    bool dragging;
    int activePointerId;
    bool dragIsUI;
    RectTransform dragUIRect; // UI 프리팹일 때
    Transform dragWorldTr; // 월드 프리팹일 때

    [Header("드롭/삭제 영역(UI)")]
    public RectTransform allowedArea;   // 허용 구역(이 안에서만 살아남음)
    public RectTransform trashArea;     // 쓰레기통(여기 놓으면 삭제)

    public TextMeshProUGUI resultText; // 결과 표시용 라벨

    public void PressSingle(int index) => PressSingle(index, null);
    public void PressDouble(int index) => PressDouble(index, null);
    public int GetCount(int index) => (KeyCount.isReady ? KeyCount.Get(index) : 0);

    bool InRange(int i) => (longPressKeys != null && i >= 0 && i < longPressKeys.Length);
    void Awake()
    {
        KeyCount.Init(longPressKeys.Length, DefaultCount);
        if (longPressKeys != null)
        {
            for (int i = 0; i < longPressKeys.Length; i++)
            {
                if (longPressKeys[i]) longPressKeys[i].manager = this;
                UpdateKeyText(i);
            }
        }
    }
    // 롱프레스 + 드래그 시작 (PointerEventData 포함)
    public void PressSingle(int index, PointerEventData ev)
    {
        if (!IsValidIndex(index, SingleWordButtons, SingleWords)) return;
        if (!TryConsumeAndRefresh(index, 1)) return;
        BeginDragSpawn(SingleWordButtons[index], SingleWords[index], ev);
    }

    public void PressDouble(int index, PointerEventData ev)
    {
        if (!IsValidIndex(index, DoubleWordButtons, DSWords)) return;

        var btn = DoubleWordButtons[index];
        var prefab = (!isShiftPressed)
            ? (index < DSWords.Length ? DSWords[index] : null)
            : (index < DDWords.Length ? DDWords[index] : null);

        if (prefab == null) return;
        if (!TryConsumeAndRefresh(index, 2)) return;
        BeginDragSpawn(btn, prefab, ev);
    }

    public void PressShift()
    {
        isShiftPressed = !isShiftPressed;
        UpdateDoubleLabels();
    }

    public void onClickSubmit()
    {
        if (!TryBuildWord(out var word)) return;
        if (resultText) resultText.text = word;
        Debug.Log($"[Submit] {word}");
    }

    bool CanBuildWord()
    {
        return TryBuildWord(out _, validateOnly: true);
    }

    public bool TryBuildWord(out string word, bool validateOnly = false)
    {
        word = null;
        if (!uiSpawnRoot || !allowedArea) return false;

        // 1) 유효 영역 안의 모든 Jamo 수집
        var magnets = uiSpawnRoot.GetComponentsInChildren<JamoMagnet>(includeInactive: false);
        var inArea = new List<JamoMagnet>();
        foreach (var m in magnets)
        {
            if (!m) continue;
            if (IsInside(allowedArea, m.GetComponent<RectTransform>())) inArea.Add(m);
        }

        // 2) 베이스(초성) 블록 수집
        var bases = new List<JamoMagnet>();
        foreach (var m in inArea)
        {
            if (IsBase(m)) bases.Add(m);
        }
        if (bases.Count == 0) return false;

        // 3) 고아(싱글) 조각이 존재하면 실패 (베이스의 자식이 아닌 Jamo)
        foreach (var m in inArea)
        {
            if (IsBase(m)) continue;
            if (!IsUnderAnyBase(m, bases)) return false;
        }

        // 4) 베이스 유효성 + 좌표 추출
        var ordered = new List<(JamoMagnet b, float x)>();
        foreach (var b in bases)
        {
            // 각 음절은 반드시 모음이 하나 필요
            var V = GetMedialGlyph(b);
            if (string.IsNullOrEmpty(V)) return false;

            // 받침은 없어도 됨
            // 좌표: validArea 기준 로컬 x
            var brt = b.GetComponent<RectTransform>();
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(uiCamera, brt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(allowedArea, sp, uiCamera, out var local);
            ordered.Add((b, local.x));
        }

        // 5) x 기준 정렬
        ordered.Sort((a, b) => a.x.CompareTo(b.x));

        // 6) 합성하여 문자열 생성
        var chars = new List<char>();
        foreach (var (b, _) in ordered)
        {
            var L = (b.glyph ?? "").Trim();                           // 초성(호환자모)
            var V = (GetMedialGlyph(b) ?? "").Trim();                 // 중성(호환자모 또는 복합)
            var T = b.attachedFinal ? (b.attachedFinal.glyph ?? "").Trim() : null; // 종성(없을 수 있음)

            try
            {
                char syllable = HangulCompose.ComposeCompat(L, V, T);
                chars.Add(syllable);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Submit] compose fail L='{L}' V='{V}' T='{T}': {e.Message}");
                return false;
            }
        }

        word = new string(chars.ToArray());
        return true;
    }

    bool IsInside(RectTransform area, RectTransform rt)
    {
        var sp = RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position);
        return RectTransformUtility.RectangleContainsScreenPoint(area, sp, uiCamera);
    }

    bool IsBase(JamoMagnet m)
    {
        // 소켓이 있는 초성 오브젝트를 "베이스"로 간주
        return m.role == JamoRole.Choseong &&
               (m.rightAnchor || m.bottomAnchor || m.bottomFinalAnchor);
    }

    bool IsUnderAnyBase(JamoMagnet child, List<JamoMagnet> bases)
    {
        var t = child.transform;
        while (t != null)
        {
            foreach (var b in bases) if (t == b.transform) return true;
            t = t.parent;
        }
        return false;
    }

    string GetMedialGlyph(JamoMagnet baseCho)
    {
        // 최종 모음(합성)이 있으면 그 글립
        if (baseCho.attachedVowel && !string.IsNullOrEmpty(baseCho.attachedVowel.glyph))
            return baseCho.attachedVowel.glyph;

        // 아니면 단일 옆/아래 모음 중 하나
        if (baseCho.attachedVowelSide && string.IsNullOrEmpty(baseCho.attachedVowelSide.glyph) == false
            && baseCho.attachedVowelBelow == null)
            return baseCho.attachedVowelSide.glyph;

        if (baseCho.attachedVowelBelow && string.IsNullOrEmpty(baseCho.attachedVowelBelow.glyph) == false
            && baseCho.attachedVowelSide == null)
            return baseCho.attachedVowelBelow.glyph;

        // 둘 다 있으면 합성이 되었어야 한다(룰 미스/지연) → 실패 처리
        return null;
    }

    void OnEnable() => UpdateDoubleLabels();

    void Update()
    {
        if (!dragging) return;

        if (!TryGetPointerScreenPos(activePointerId, out var screenPos))
        {
            EndDrag();
            return;
        }

        if (dragIsUI && dragUIRect)
        {
            var root = ResolveUISpawnRoot();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, uiCamera, out var local))
            {
                dragUIRect.anchoredPosition = local;
            }
        }
        else if (dragWorldTr)
        {
            var cam = Camera.main;
            var sp = new Vector3(screenPos.x, screenPos.y, worldDepth);
            var worldPos = cam ? cam.ScreenToWorldPoint(sp) : dragWorldTr.position;
            dragWorldTr.position = worldPos;
        }

        if (IsPointerReleased(activePointerId))
        {
            EndDrag(); // 그냥 현재 위치에 고정
        }
        //Debug.Log(dragging);
    }

    void UpdateDoubleLabels()
    {
        if (DoubleText == null) return;

        for (int i = 0; i < DoubleText.Length; i++)
        {
            if (DoubleText[i] == null) continue;

            if (!isShiftPressed)
            {
                if (i < DSWords.Length && DSWords[i] != null)
                    DoubleText[i].text = DSWords[i].name;
            }
            else
            {
                if (i < DDWords.Length && DDWords[i] != null)
                    DoubleText[i].text = DDWords[i].name;
            }
        }
    }

    bool IsValidIndex(int index, Button[] buttons, GameObject[] prefabs)
    {
        if (buttons == null || prefabs == null) return false;
        if (index < 0 || index >= buttons.Length) return false;
        if (index >= prefabs.Length) return false;
        if (buttons[index] == null || prefabs[index] == null) return false;
        return true;
    }

    
    void BeginDragSpawn(Button button, GameObject prefab, PointerEventData ev)
    {
        var buttonRT = button.GetComponent<RectTransform>();

        // UI 프리팹 판정
        bool isUIPrefab = prefab.GetComponent<RectTransform>() != null
                       && prefab.GetComponent<CanvasRenderer>() != null;

        // 스크린 좌표 계산 (버튼 위치 기준)
        Vector2 buttonScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, buttonRT.position);
        Vector2 startScreen = ev != null ? ev.position : buttonScreen;

        if (isUIPrefab)
        {
            var root = ResolveUISpawnRoot();

            // 스크린 -> 로컬
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, startScreen, uiCamera, out var local);

            var go = Instantiate(prefab, root);
            var rt = go.GetComponent<RectTransform>();
            // 드래그 전용 기본 세팅
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = local + (Vector2)spawnOffset;
            rt.localScale = Vector3.one;
            var drag = go.GetComponent<DraggableWordUI>();
            drag.Init(root, allowedArea, trashArea, uiCamera);
            // 드래그 상태 진입
            dragIsUI = true;
            dragUIRect = rt;
            dragWorldTr = null;
        }
        else
        {
            var cam = Camera.main;
            var sp = new Vector3(startScreen.x, startScreen.y, worldDepth);
            var worldPos = cam ? cam.ScreenToWorldPoint(sp) : buttonRT.position;

            var go = Instantiate(prefab, worldPos + spawnOffset, Quaternion.identity);

            dragIsUI = false;
            dragUIRect = null;
            dragWorldTr = go.transform;
        }

        dragging = true;
        activePointerId = ev != null ? ev.pointerId : -1; // 마우스 기본 -1
    }

    RectTransform ResolveUISpawnRoot()
    {
        if (uiSpawnRoot) return uiSpawnRoot;
        var canvas = targetCanvas;
        if (!canvas)
        {
            // 버튼의 캔버스 자동 탐색
            canvas = FindAnyObjectByType<Canvas>();
        }
        return canvas ? canvas.transform as RectTransform : null;
    }

    void EndDrag()
    {
        dragging = false;
        activePointerId = int.MinValue;
        // 여기서 스냅/검증/충돌체크 등을 추가할 수 있음
        dragUIRect = null;
        dragWorldTr = null;
    }

    public void AddRandomKeys(int amount)
    {
        if (!KeyCount.isReady || amount <= 0) return;
        KeyCount.AddRandom(amount);

        if (longPressKeys != null)
            for (int i = 0; i < longPressKeys.Length; i++) UpdateKeyText(i);
    }

    public void AddKeyAt(int index, int add)
    {
        if (!InRange(index) || add == 0 || !KeyCount.isReady) return;
        KeyCount.AddAt(index, add);
        UpdateKeyText(index);
    }


    bool TryConsumeAndRefresh(int index, int amount = 1)
    {
        if (!InRange(index)) return false;
        if (!KeyCount.TryConsume(index, amount))
        {
            NotEnoughFeedback(index);
            return false;
        }
        UpdateKeyText(index);
        return true;
    }

    void NotEnoughFeedback(int index)
    {
        // TODO: 진동, SFX, 텍스트 빨간 깜빡임 등
        // var txt = longPressKeys[index]?.KeyCount; ...
    }


    void UpdateKeyText(int index)
    {
        if (!InRange(index)) return;
        var k = longPressKeys[index];
        if (k != null && k.KeyCount != null)
        {
            k.KeyCount.text = GetCount(index).ToString();
        }
    }
    bool TryGetPointerScreenPos(int pointerId, out Vector2 pos)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // 마우스면 pointerId가 -1이든 0이든 그냥 커서 좌표 반환
        pos = Input.mousePosition;
        return true;
#else
    for (int i = 0; i < Input.touchCount; i++)
    {
        var t = Input.GetTouch(i);
        if (t.fingerId == pointerId) { pos = t.position; return true; }
    }
    pos = default;
    return false;
#endif
    }


    bool IsPointerReleased(int pointerId)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButtonUp(0);     
#else
    for (int i = 0; i < Input.touchCount; i++)
    {
        var t = Input.GetTouch(i);
        if (t.fingerId == pointerId &&
            (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
            return true;
    }
    return false;                           
#endif
    }

}
