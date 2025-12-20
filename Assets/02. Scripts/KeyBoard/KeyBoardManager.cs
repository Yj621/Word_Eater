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
    public int maxCount = 5;
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
    public SyllableBlock syllablePrefab;
    [Header("드롭/삭제 영역(UI)")]
    public RectTransform allowedArea;   // 허용 구역(이 안에서만 살아남음)
    public RectTransform trashArea;     // 쓰레기통(여기 놓으면 삭제)
  
    public TextMeshProUGUI resultText; // 결과 표시용 라벨
    Dictionary<int, int> _sessionSpent = new();

    public void PressSingle(int index) => PressSingle(index, null);
    public void PressDouble(int index) => PressDouble(index, null);
    public int GetCount(int index) => (KeyCount.isReady ? KeyCount.Get(index) : 0);

    bool InRange(int i) => (longPressKeys != null && i >= 0 && i < longPressKeys.Length);

    List<SyllableBlock> GetBlocksInAllowedArea()
    {
        var result = new List<SyllableBlock>();
        if (!uiSpawnRoot || !allowedArea) return result;

        var blocks = uiSpawnRoot.GetComponentsInChildren<SyllableBlock>(includeInactive: false);
        foreach (var b in blocks)
        {
            if (!b) continue;
            var rt = b.GetComponent<RectTransform>();
            if (!rt) continue;

            var sp = RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position);
            if (RectTransformUtility.RectangleContainsScreenPoint(allowedArea, sp, uiCamera))
                result.Add(b);
        }

        return result;
    }

    bool TryBuildFromBlocks(List<SyllableBlock> blocks, out string word)
    {
        word = null;
        if (blocks == null || blocks.Count == 0) return false;

        // 1) 고아 JamoMagnet 검사 (원하면 켜두기)
        // allowedArea 안에 있는데, 어떤 SyllableBlock의 자식도 아닌 자모가 있으면 실패
        var magnets = uiSpawnRoot.GetComponentsInChildren<JamoMagnet>(includeInactive: false);
        foreach (var m in magnets)
        {
            if (!m) continue;

            var rt = m.GetComponent<RectTransform>();
            if (!rt) continue;

            var sp = RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position);
            if (!RectTransformUtility.RectangleContainsScreenPoint(allowedArea, sp, uiCamera))
                continue;

            // SyllableBlock의 자식인지 확인
            var parentBlock = m.GetComponentInParent<SyllableBlock>();
            if (parentBlock == null)
            {
                // allowedArea 안에 떠다니는 자모가 있다 → 아직 완성 안 된 단어
                Debug.Log("[TryBuildWord] orphan jamo found → invalid word");
                return false;
            }
        }
        var ordered = new List<(SyllableBlock b, float x)>();
        foreach (var b in blocks)
        {
            // 최소 요건: 초성과 중성이 있어야 음절로 인정
            if (string.IsNullOrEmpty(b.choseong) || string.IsNullOrEmpty(b.jungseong))
            {
                Debug.Log($"[TryBuildWord] invalid block(need L+V): L='{b.choseong}' V='{b.jungseong}'");
                return false;
            }

            var brt = b.GetComponent<RectTransform>();
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(uiCamera, brt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(allowedArea, sp, uiCamera, out var local);
            ordered.Add((b, local.x));
        }

        ordered.Sort((a, b) => a.x.CompareTo(b.x));

        // 3) HangulCompose로 실제 글자 합성
        var chars = new List<char>();
        foreach (var (b, _) in ordered)
        {
            var L = (b.choseong ?? "").Trim();
            var V = (b.jungseong ?? "").Trim();
            var T = string.IsNullOrEmpty(b.jongseong) ? null : b.jongseong.Trim();

            try
            {
                char syllable = HangulCompose.ComposeCompat(L, V, T);
                chars.Add(syllable);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Submit] compose fail from block L='{L}' V='{V}' T='{T}': {e.Message}");
                return false;
            }
        }

        word = new string(chars.ToArray());
        return true;
    }

    void Awake()
    {
        KeyCount.OnChanged -= OnKeyCountChanged;
        KeyCount.OnChanged += OnKeyCountChanged;

        KeyCount.Init(longPressKeys.Length, DefaultCount, maxCount);

        if (longPressKeys != null)
        {
            for (int i = 0; i < longPressKeys.Length; i++)
                if (longPressKeys[i]) longPressKeys[i].manager = this;
        }
        SyllableBlock.Prefab = syllablePrefab;
    }

    private void Start()
    {
        _sessionSpent.Clear();
    }

    void OnEnable()
    {
        _sessionSpent.Clear();
        UpdateDoubleLabels();


    }


    void OnDestroy()
    {
        KeyCount.OnChanged -= OnKeyCountChanged;
    }

    void OnKeyCountChanged(int index, int newCount)
    {
        RefreshKeyUI(index);
    }
    void RefreshKeyUI(int index)
    {
        if (!InRange(index)) return;
        var k = longPressKeys[index];
        if (k == null) return;
        k.RefreshVisuals(KeyCount.Get(index), KeyCount.MaxCount);
    }

    public void PressSingle(int index, PointerEventData ev)
    {
        if (!IsValidIndex(index, SingleWordButtons, SingleWords)) return;
        if (!TryConsumeAndRefresh(index, 1)) return;
        BeginDragSpawn(SingleWordButtons[index], SingleWords[index], ev, index, 1);
    }

    public void PressDouble(int index, PointerEventData ev)
    {
        if (!IsValidIndex(index, DoubleWordButtons, DSWords)) return;

        var btn = DoubleWordButtons[index];
        var prefab = (!isShiftPressed)
            ? (index < DSWords.Length ? DSWords[index] : null)
            : (index < DDWords.Length ? DDWords[index] : null);
        if (prefab == null) return;

        int cost = isShiftPressed ? 2 : 1;
        if (!TryConsumeAndRefresh(index, cost)) { NotEnoughFeedback(index); return; }
        BeginDragSpawn(btn, prefab, ev, index, cost);
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
    }

    bool CanBuildWord()
    {
        return TryBuildWord(out _, validateOnly: true);
    }

    public bool TryBuildWord(out string word, bool validateOnly = false)
    {
        word = null;
        if (!uiSpawnRoot || !allowedArea) return false;

        // 0) 먼저 SyllableBlock 기준으로 시도
        var blocks = GetBlocksInAllowedArea();
        if (blocks.Count > 0)
        {
            var ok = TryBuildFromBlocks(blocks, out word);
            if (!ok) return false;

            // validateOnly면 여기서 true만 리턴해도 되고,
            // 어차피 지금은 대부분 실제 word가 필요하니까 그냥 word 세팅 유지
            return true;
        }

        // 1) SyllableBlock이 하나도 없으면, 예전 JamoMagnet 방식으로 시도 (호환용)
        //    만약 완전히 새 구조만 쓴다면 아래를 통째로 지워도 됨.
        // --- 기존 JamoMagnet 기반 로직 그대로 ---
        var magnets = uiSpawnRoot.GetComponentsInChildren<JamoMagnet>(includeInactive: false);
        var inArea = new List<JamoMagnet>();
        foreach (var m in magnets)
        {
            if (!m) continue;
            if (IsInside(allowedArea, m.GetComponent<RectTransform>())) inArea.Add(m);
        }

        var bases = new List<JamoMagnet>();
        foreach (var m in inArea)
        {
            if (IsBase(m)) bases.Add(m);
        }
        if (bases.Count == 0) return false;

        foreach (var m in inArea)
        {
            if (IsBase(m)) continue;
            if (!IsUnderAnyBase(m, bases)) return false;
        }

        var ordered = new List<(JamoMagnet b, float x2)>();
        foreach (var b in bases)
        {
            var V = GetMedialGlyph(b);
            if (string.IsNullOrEmpty(V)) return false;

            var brt = b.GetComponent<RectTransform>();
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(uiCamera, brt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(allowedArea, sp, uiCamera, out var local);
            ordered.Add((b, local.x));
        }

        ordered.Sort((a, b) => a.x2.CompareTo(b.x2));

        var chars = new List<char>();
        foreach (var (b, _) in ordered)
        {
            var L = (b.glyph ?? "").Trim();
            var V = (GetMedialGlyph(b) ?? "").Trim();
            var T = b.attachedFinal ? (b.attachedFinal.glyph ?? "").Trim() : null;

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


    void BeginDragSpawn(Button button, GameObject prefab, PointerEventData ev, int invIndex, int amount)
    {
        var buttonRT = button.GetComponent<RectTransform>();
        Vector2 buttonScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, buttonRT.position);
        Vector2 startScreen = ev != null ? ev.position : buttonScreen;

        bool isUIPrefab = prefab.GetComponent<RectTransform>() && prefab.GetComponent<CanvasRenderer>();

        if (isUIPrefab)
        {
            var root = ResolveUISpawnRoot();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, startScreen, uiCamera, out var local);
            var go = Instantiate(prefab, root);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = local + (Vector2)spawnOffset;
            rt.localScale = Vector3.one;

            var drag = go.GetComponent<DraggableWordUI>();
            drag.Init(root, allowedArea, trashArea, uiCamera);
            drag.BindSource(this, invIndex, amount);     // ★ 원본 인덱스 바인딩

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

            var drag = go.GetComponent<DraggableWordUI>();
            if (drag != null)
            {
                // 월드 프리팹이어도 바인딩만은 해둔다(환불용)
                drag.Init(null, null, null, null);
                drag.BindSource(this, invIndex, amount);
            }

            dragIsUI = false;
            dragUIRect = null;
            dragWorldTr = go.transform;
        }

        dragging = true;
        activePointerId = ev != null ? ev.pointerId : -1;
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

    }

    public void AddKeyAt(int index, int add)
    {
        if (!InRange(index) || add == 0 || !KeyCount.isReady) return;
        KeyCount.AddAt(index, add);
    }


    bool TryConsumeAndRefresh(int index, int amount = 1)
    {
        if (!InRange(index)) return false;
        if (!KeyCount.TryConsume(index, amount))
        {
            NotEnoughFeedback(index);
            return false;
        }
        RecordSpend(index, amount);

        return true;
    }

    public void OnPieceDeleted(int invIndex, int amount)
    {
        // 수량 환불
        KeyCount.AddAt(invIndex, amount);
        // 세션 장부도 감소
        RecordRefund(invIndex, amount);
    }

    public int GrantRandomLetters(int amount, bool singlesOnly = false, bool doublesOnly = false)
    {
        if (amount <= 0 || longPressKeys == null || longPressKeys.Length == 0) return 0;

        // 후보 인덱스 수집 (최대치 미만만)
        var eligible = new List<int>();
        for (int i = 0; i < longPressKeys.Length; i++)
        {
            // 타입 필터 (패딩 null 사용 중이라는 전제)
            bool isSingleSlot = (SingleWordButtons != null && i < SingleWordButtons.Length && SingleWordButtons[i] != null);
            bool isDoubleSlot = (DoubleWordButtons != null && i < DoubleWordButtons.Length && DoubleWordButtons[i] != null);

            if (singlesOnly && !isSingleSlot) continue;
            if (doublesOnly && !isDoubleSlot) continue;

            if (KeyCount.Get(i) < KeyCount.MaxCount)
                eligible.Add(i);
        }

        if (eligible.Count == 0) return 0;

        int actuallyAdded = 0;

        for (int n = 0; n < amount; n++)
        {
            // 남은 후보가 없으면 중단
            if (eligible.Count == 0) break;

            // 랜덤 후보 하나 뽑기
            int pick = Random.Range(0, eligible.Count);
            int idx = eligible[pick];

            // 한 칸 올리기 (KeyCount가 OnChanged로 UI 자동 갱신)
            KeyCount.AddAt(idx, 1);
            actuallyAdded++;

            // 꽉 찼으면 후보에서 제거
            if (KeyCount.Get(idx) >= KeyCount.MaxCount)
                eligible.RemoveAt(pick);
        }

        return actuallyAdded;
    }


    public void ClosePanelAndRestore()
    {
        foreach (var kv in _sessionSpent)
        {
            int index = kv.Key;
            int amt = kv.Value;
            if (amt > 0) KeyCount.AddAt(index, amt);
        }
        _sessionSpent.Clear();
        ClearAllSpawnedPieces();
    }

    void ClearAllSpawnedPieces()
    {
        if (!uiSpawnRoot) return;

        // 드래그 프리팹 제거
        var drags = uiSpawnRoot.GetComponentsInChildren<DraggableWordUI>(true);
        foreach (var d in drags) if (d) Destroy(d.gameObject);

        // 자모 파츠 제거(합성 포함)
        var magnets = uiSpawnRoot.GetComponentsInChildren<JamoMagnet>(true);
        foreach (var m in magnets) if (m) Destroy(m.gameObject);

        EndDrag();
    }

    void NotEnoughFeedback(int index)
    {
        
    }

    void RecordSpend(int index, int amount)
    {
        if (!_sessionSpent.ContainsKey(index)) _sessionSpent[index] = 0;
        _sessionSpent[index] += Mathf.Max(1, amount);
    }

    void RecordRefund(int index, int amount)
    {
        if (!_sessionSpent.ContainsKey(index)) return;
        _sessionSpent[index] = Mathf.Max(0, _sessionSpent[index] - Mathf.Max(1, amount));
        if (_sessionSpent[index] == 0) _sessionSpent.Remove(index);
    }

    bool TryGetPointerScreenPos(int pointerId, out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        // 새 입력 시스템: Pointer.current가 있으면 그 좌표 사용
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer != null)
        {
            pos = pointer.position.ReadValue();
            return true;
        }
#endif

        // 구 입력 시스템: 에디터/PC는 마우스, 모바일은 터치
#if UNITY_EDITOR || UNITY_STANDALONE
        pos = Input.mousePosition;
        return true;
#else
    for (int i = 0; i < Input.touchCount; i++)
    {
        var t = Input.GetTouch(i);
        if (t.fingerId == pointerId) { pos = t.position; return true; }
    }
    // 만약 위가 실패하면 마우스 좌표로도 폴백 (일부 기기에서 유효)
    pos = Input.mousePosition;
    return true;
#endif
    }


    bool IsPointerReleased(int pointerId)
    {
#if ENABLE_INPUT_SYSTEM
        var pointer = UnityEngine.InputSystem.Pointer.current;
        // pointer가 없거나 press가 풀리면 released로 간주
        return pointer == null || !pointer.press.isPressed;
#else
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
#endif
    }
}
