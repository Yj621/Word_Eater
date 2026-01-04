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
    public Button[] SingleWordButtons;          // 단일 글자 키
    public Button[] DoubleWordButtons;          // 쌍자/쌍모음 키

    [Header("소환 프리팹")]
    public GameObject[] SingleWords;            // 단일 자모 프리팹
    public GameObject[] DSWords;                // 기본 자모(Shift OFF)
    public GameObject[] DDWords;                // Shift 시 자모(쌍자/쌍모음 등)

    [Header("표시 라벨(TMP)")]
    public TextMeshProUGUI[] DoubleText;        // 더블키 위에 찍히는 글자

    [Header("입력 상태 / 인벤토리")]
    public bool isShiftPressed = false;
    public LongPressKey[] longPressKeys;        // 키 한 칸 정보 (개수, 쿨 같은 거)
    public int DefaultCount = 2;
    public int maxCount = 5;

    [Header("UI/World 스폰 설정")]
    public Canvas targetCanvas;                 // UI 캔버스
    public RectTransform uiSpawnRoot;           // 드래그 프리팹이 붙을 루트
    public Camera uiCamera;                     // UI 카메라 (Overlay면 null)
    public float worldDepth = 10f;              // 월드 스폰 시 Z 거리
    public Vector3 spawnOffset;                 // 버튼 위치 기준 오프셋

    [Header("드래그 상태")]
    bool dragging;
    int activePointerId;
    bool dragIsUI;
    RectTransform dragUIRect;                   // UI 프리팹 드래그
    Transform dragWorldTr;                      // 월드 프리팹 드래그

    [Header("음절 블럭")]
    public SyllableBlock syllablePrefab;        // SyllableBlock 프리팹 (초기 1개 할당용)

    [Header("드롭/삭제 영역(UI)")]
    public RectTransform allowedArea;           // 유효 영역(이 안에 있어야 제출)
    public RectTransform trashArea;             // 쓰레기통(여기 떨구면 삭제)

    [Header("롱프레스 스폰 보정")]
    public Vector2 longPressSpawnOffset = new Vector2(0f, 120f);

    [Header("디버그/출력용")]
    public TextMeshProUGUI resultText;          // 제출 결과 표시용

    Dictionary<int, int> _sessionSpent = new(); // 세션 동안 소비한 키 수량 기록
    bool InRange(int i) => (longPressKeys != null && i >= 0 && i < longPressKeys.Length);
    public int GetCount(int index) => (KeyCount.isReady ? KeyCount.Get(index) : 0);

    Vector2 lastPointerScreenPos;
    Vector2 _dragOffset;
   
    // -----------------------------
    // 초기화
    // -----------------------------

    void Awake()
    {
        KeyCount.OnChanged -= OnKeyCountChanged;
        KeyCount.OnChanged += OnKeyCountChanged;

        KeyCount.Init(longPressKeys.Length, DefaultCount, maxCount);

        if (longPressKeys != null)
        {
            for (int i = 0; i < longPressKeys.Length; i++)
            {
                if (longPressKeys[i])
                    longPressKeys[i].manager = this;
            }
        }

        // SyllableBlock에서 사용할 프리팹 레퍼런스 설정
        SyllableBlock.Prefab = syllablePrefab;
    }

    void Start()
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

    // -----------------------------
    // 키 인벤토리 UI 갱신
    // -----------------------------

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

    // -----------------------------
    // 단일/더블 키 입력
    // -----------------------------

    public void PressSingle(int index) => PressSingle(index, null);
    public void PressDouble(int index) => PressDouble(index, null);

    public void PressSingle(int index, PointerEventData ev)
    {
        if (!IsValidIndex(index, SingleWordButtons, SingleWords)) return;
        if (!TryConsumeAndRefresh(index, 1)) return;

        BeginDragSpawn(SingleWordButtons[index], SingleWords[index], ev, index, 1, isLongPress: false);
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
        if (!TryConsumeAndRefresh(index, cost))
        {
            NotEnoughFeedback(index);
            return;
        }
        BeginDragSpawn(btn, prefab, ev, index, cost, isLongPress: false);
    }

    public void PressShift()
    {
        isShiftPressed = !isShiftPressed;
        UpdateDoubleLabels();
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

    // -----------------------------
    // 단어 빌드 & 제출
    // -----------------------------

    public void onClickSubmit()
    {
        if (!TryBuildWord(out var word)) return;
        if (resultText) resultText.text = word;
    }

    /// <summary>현재 allowedArea 안의 SyllableBlock을 읽어 단어 문자열로 만든다.</summary>
    public bool TryBuildWord(out string word)
    {
        word = null;
        if (!uiSpawnRoot || !allowedArea) return false;

        // 1) 유효 영역 안의 블럭 모으기
        var blocks = GetBlocksInAllowedArea();
        if (blocks.Count == 0) return false;

        // 2) 블럭 기준으로 음절 합성
        return TryBuildFromBlocks(blocks, out word);
    }

    /// <summary>allowedArea 안의 SyllableBlock 목록을 찾는다.</summary>
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

    /// <summary>SyllableBlock 리스트를 X좌표 정렬 후 HangulCompose로 문자열로 합친다.</summary>
    bool TryBuildFromBlocks(List<SyllableBlock> blocks, out string word)
    {
        word = null;
        if (blocks == null || blocks.Count == 0) return false;

        // 1) 고아 JamoMagnet이 있는지 검사 (블럭에 안 붙어 있는 자모가 있으면 실패)
        var magnets = uiSpawnRoot.GetComponentsInChildren<JamoMagnet>(includeInactive: false);
        foreach (var m in magnets)
        {
            if (!m) continue;

            var rt = m.GetComponent<RectTransform>();
            if (!rt) continue;

            var sp = RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position);
            if (!RectTransformUtility.RectangleContainsScreenPoint(allowedArea, sp, uiCamera))
                continue;

            var parentBlock = m.GetComponentInParent<SyllableBlock>();
            if (parentBlock == null)
            {
                Debug.Log("[TryBuildWord] orphan jamo found → invalid word");
                return false;
            }
        }

        // 2) X좌표 기준으로 블럭 정렬
        var ordered = new List<(SyllableBlock b, float x)>();
        foreach (var b in blocks)
        {
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
            catch (Exception e)
            {
                Debug.LogWarning($"[Submit] compose fail from block L='{L}' V='{V}' T='{T}': {e.Message}");
                return false;
            }
        }

        word = new string(chars.ToArray());
        return true;
    }

    // -----------------------------
    // 드래그 업데이트
    // -----------------------------

    void Update()
    {
        if (!dragging) return;

        if (!TryGetPointerScreenPos(activePointerId, out var screenPos))
        {
            EndDrag();
            return;
        }

        lastPointerScreenPos = screenPos;

        if (dragIsUI && dragUIRect)
        {
            var root = ResolveUISpawnRoot();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, uiCamera, out var local))
                dragUIRect.anchoredPosition = local + _dragOffset;
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
            HandleInitialDropForUI();
            EndDrag();
        }
    }

    void HandleInitialDropForUI()
    {
        if (!dragIsUI || !dragUIRect) return;

        var drag = dragUIRect.GetComponent<DraggableWordUI>();

        // 1) 쓰레기통 위 → 환불 + 삭제
        if (trashArea &&
            RectTransformUtility.RectangleContainsScreenPoint(
                trashArea, lastPointerScreenPos, uiCamera))
        {
            if (drag != null) drag.RefundAndDestroy();
            else Destroy(dragUIRect.gameObject);
            return;
        }

        // 2) 허용 영역 밖 → 환불 + 삭제
        if (allowedArea &&
            !RectTransformUtility.RectangleContainsScreenPoint(
                allowedArea, lastPointerScreenPos, uiCamera))
        {
            if (drag != null) drag.RefundAndDestroy();
            else Destroy(dragUIRect.gameObject);
            return;
        }

        // 3) 허용 영역 안이라면 그냥 그 자리에 남겨둠
    }


    // -----------------------------
    // 프리팹 스폰 & 드래그 시작
    // -----------------------------

    void BeginDragSpawn(Button button, GameObject prefab, PointerEventData ev, int invIndex, int amount, bool isLongPress)
    {
        var buttonRT = button.GetComponent<RectTransform>();

        Vector2 startScreen = ev != null
            ? ev.position
            : RectTransformUtility.WorldToScreenPoint(uiCamera, buttonRT.position);

        bool isUIPrefab = prefab.GetComponent<RectTransform>() && prefab.GetComponent<CanvasRenderer>();

        if (isUIPrefab)
        {
            var root = ResolveUISpawnRoot();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, startScreen, uiCamera, out var local);

            _dragOffset = longPressSpawnOffset;

            var go = Instantiate(prefab, root);
            var rt = go.GetComponent<RectTransform>();

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.anchoredPosition = local + (Vector2)spawnOffset + _dragOffset;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();

            var jamo = go.GetComponent<JamoMagnet>();
            if (jamo) jamo.PlaySpawnAnim();

            var drag = go.GetComponent<DraggableWordUI>();
            if (drag)
            {
                drag.Init(root, allowedArea, trashArea, uiCamera);
                drag.BindSource(this, invIndex, amount);
            }

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

            _dragOffset = Vector2.zero;
        }

        dragging = true;
        activePointerId = ev != null ? ev.pointerId : -1;
    }


    RectTransform ResolveUISpawnRoot()
    {
        if (uiSpawnRoot) return uiSpawnRoot;

        var canvas = targetCanvas;
        if (!canvas)
            canvas = FindAnyObjectByType<Canvas>();

        return canvas ? canvas.transform as RectTransform : null;
    }

    void EndDrag()
    {
        if (dragging && dragIsUI && dragUIRect)
        {
            var drag = dragUIRect.GetComponent<DraggableWordUI>();

            if (drag && EventSystem.current != null &&
                TryGetPointerScreenPos(activePointerId, out var screenPos))
            {
                var root = ResolveUISpawnRoot();
                Vector2 screenOffset = Vector2.zero;

                if (root)
                {
                    Vector2 p0 = RectTransformUtility.WorldToScreenPoint(uiCamera, root.TransformPoint(Vector3.zero));
                    Vector2 p1 = RectTransformUtility.WorldToScreenPoint(uiCamera, root.TransformPoint((Vector3)_dragOffset));
                    screenOffset = p1 - p0;
                }

                var fakeEvent = new PointerEventData(EventSystem.current)
                {
                    position = screenPos + screenOffset  
                };

                drag.OnEndDrag(fakeEvent);
            }
        }

        dragging = false;
        activePointerId = int.MinValue;
        dragUIRect = null;
        dragWorldTr = null;
        // _dragOffset은 여기서 0으로 꺼도 됨
        _dragOffset = Vector2.zero;
    }


    public bool AddKeyByGlyph(string glyph, int amount = 1)
    {
        if (!KeyCount.isReady || string.IsNullOrEmpty(glyph))
            return false;

        int index = FindSlotIndexByGlyph(glyph);
        if (index < 0)
        {
            Debug.LogWarning($"[KeyBoardManager] '{glyph}' 에 해당하는 슬롯을 찾지 못했어.");
            return false;
        }

        KeyCount.AddAt(index, amount);
        // 아이템으로 준 거라서 _sessionSpent 에는 기록 안 함
        return true;
    }

    // 🔹 prefab들에서 glyph를 보고 longPressKeys 인덱스 찾기
    int FindSlotIndexByGlyph(string glyph)
    {
        if (longPressKeys == null) return -1;
        glyph = glyph.Trim();

        for (int i = 0; i < longPressKeys.Length; i++)
        {
            GameObject prefab = null;

            // 이 인덱스가 싱글키면 SingleWords 쪽에서 찾고
            if (i < SingleWords.Length && SingleWords[i] != null)
            {
                prefab = SingleWords[i];
            }
            // 아니면 더블키(자/모)에서 찾고
            else if (i < DSWords.Length && DSWords[i] != null)
            {
                prefab = DSWords[i];   // 기본(Shift OFF) 자모 기준
            }
            else if (i < DDWords.Length && DDWords[i] != null)
            {
                prefab = DDWords[i];   // 필요하면 이쪽도 사용
            }

            if (!prefab) continue;

            var mag = prefab.GetComponent<JamoMagnet>();
            if (mag != null && mag.glyph == glyph)
                return i;
        }

        return -1;
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
        KeyCount.AddAt(invIndex, amount);
        RecordRefund(invIndex, amount);
    }

    public int GrantRandomLetters(int amount, bool singlesOnly = false, bool doublesOnly = false)
    {
        if (amount <= 0 || longPressKeys == null || longPressKeys.Length == 0) return 0;

        var eligible = new List<int>();
        for (int i = 0; i < longPressKeys.Length; i++)
        {
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
            if (eligible.Count == 0) break;

            int pick = Random.Range(0, eligible.Count);
            int idx = eligible[pick];

            KeyCount.AddAt(idx, 1);
            actuallyAdded++;

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

        var drags = uiSpawnRoot.GetComponentsInChildren<DraggableWordUI>(true);
        foreach (var d in drags)
            if (d) Destroy(d.gameObject);

        var magnets = uiSpawnRoot.GetComponentsInChildren<JamoMagnet>(true);
        foreach (var m in magnets)
            if (m) Destroy(m.gameObject);

        EndDrag();
    }

    void NotEnoughFeedback(int index)
    {
        // 키 부족 시 이펙트/사운드 넣고 싶으면 여기서 처리
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

    // -----------------------------
    // 입력 시스템 래핑 (마우스/터치 공통 처리)
    // -----------------------------

    bool TryGetPointerScreenPos(int pointerId, out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer != null)
        {
            pos = pointer.position.ReadValue();
            return true;
        }
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
        pos = Input.mousePosition;
        return true;
#else
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.fingerId == pointerId)
            {
                pos = t.position;
                return true;
            }
        }
        pos = Input.mousePosition;
        return true;
#endif
    }

    bool IsPointerReleased(int pointerId)
    {
#if ENABLE_INPUT_SYSTEM
        var pointer = UnityEngine.InputSystem.Pointer.current;
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

    // -----------------------------
    // 유틸
    // -----------------------------

    bool IsValidIndex(int index, Button[] buttons, GameObject[] prefabs)
    {
        if (buttons == null || prefabs == null) return false;
        if (index < 0 || index >= buttons.Length) return false;
        if (index >= prefabs.Length) return false;
        if (buttons[index] == null || prefabs[index] == null) return false;
        return true;
    }

    // KeyBoardManager 클래스 안 어딘가(public 메서드들 근처)에 추가
    /// <summary>
    /// 단어 제출 등으로 키 사용을 확정할 때 호출.
    /// - 지금 세션에서 쓴 키(_sessionSpent)는 환불하지 않고 버린다.
    /// - 화면 위에 놓인 조각들은 정리만 한다.
    /// </summary>
    public void ConfirmUse()
    {
        _sessionSpent.Clear();   // 환불 기록 버리기 (다시는 돌려주지 않음)
        ClearAllSpawnedPieces(); // 화면에 남은 자모/Syl 블럭 제거
    }

}
