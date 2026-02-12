using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
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

    [Header("안내 UI")]
    public TextMeshProUGUI PushWarningText;    // 꾹 누르세요 안내 텍스트

    Dictionary<int, int> _sessionSpent = new(); // 세션 동안 소비한 키 수량 기록
    bool InRange(int i) => (longPressKeys != null && i >= 0 && i < longPressKeys.Length);
    public int GetCount(int index) => (KeyCount.isReady ? KeyCount.Get(index) : 0);

    Vector2 lastPointerScreenPos;
    Vector2 _dragOffset;
   
    /// <summary>
    /// Canvas 모드에 따라 적절한 카메라(Overlay면 null, 아니면 worldCamera/uiCamera)를 반환
    /// </summary>
    Camera GetRefinedCamera(RectTransform root)
    {
        if (!root) return uiCamera;
        var canvas = root.GetComponentInParent<Canvas>();
        if (!canvas) return uiCamera;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera ? canvas.worldCamera : uiCamera;
    }

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

        // [추가] FileManager에서 로드된 키 개수가 있으면 적용
        if (FileManager.Instance != null && FileManager.Instance.tempLoadedKeyCounts != null)
        {
            // [수정] 저장된 최대 개수를 불러오되, Inspector에서 수정한 값과 비교해서 더 큰 값을 사용
            // 이렇게 하면 저장 파일에는 5로 되어있더라도, 개발자가 10으로 늘렸으면 10이 적용됨
            int loadedMax = FileManager.Instance.tempLoadedMaxKeyCount;
            if (loadedMax > 0)
            {
                // 인스펙터 설정(maxCount)과 저장된 데이터(loadedMax) 중 큰 걸 선택
                int finalMax = Mathf.Max(maxCount, loadedMax);
                
                KeyCount.SetMaxCount(finalMax);
                maxCount = finalMax; // 로컬 변수도 동기화
            }

            var loaded = FileManager.Instance.tempLoadedKeyCounts.ToArray();
            if (loaded.Length > 0)
            {
                KeyCount.SetAllCounts(loaded);
            }
        }
    }

    void OnEnable()
    {
        _sessionSpent.Clear();
        UpdateDoubleLabels();
    }

    void OnDestroy()
    {
        KeyCount.OnChanged -= OnKeyCountChanged;
        // 파괴될 때(씬 이동 등) 저장
        if (FileManager.Instance != null) FileManager.Instance.UpdateAndSaveKeyCounts();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            // 앱 내려갈 때 저장
            if (FileManager.Instance != null) FileManager.Instance.UpdateAndSaveKeyCounts();
        }
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
        // 1. 버튼 유효성 체크
        if (!IsValidIndex(index, DoubleWordButtons, DSWords))
        {
            Debug.LogError($"[PressDouble] Invalid Button Index or DSWords missing: {index}");
            return;
        }

        // 2. 프리팹 찾기
        GameObject prefab = (!isShiftPressed)
            ? (index < DSWords.Length ? DSWords[index] : null)
            : (index < DDWords.Length ? DDWords[index] : null);

        if (prefab == null)
        {
            Debug.LogWarning($"[PressDouble] Prefab Not Found. Index:{index}, Shift:{isShiftPressed}");
            return;
        }

        // [롤백] 사용자 환경에 맞춰 Offset이나 FindWithout 없이 그대로 Index 사용
        // SingleWords의 앞부분이 비어있고 DoubleWords가 그 자리를 쓰는 구조로 추정됨
        int realIndex = index;

        // 범위 체크
        if (!InRange(realIndex))
        {
             Debug.LogError($"[PressDouble] Index Out of Range! Index:{index} Max:{longPressKeys?.Length}");
             return;
        }

        int cost = isShiftPressed ? 2 : 1;
        if (!TryConsumeAndRefresh(realIndex, cost))
        {
            Debug.Log($"[PressDouble] Not Enough Keys. Index:{realIndex}, Cost:{cost}, Current:{GetCount(realIndex)}");
            NotEnoughFeedback(realIndex);
            return;
        }
        
        BeginDragSpawn(DoubleWordButtons[index], prefab, ev, realIndex, cost, isLongPress: false);
    }

    public void PressShift()
    {
        isShiftPressed = !isShiftPressed;
        UpdateDoubleLabels();
    }

    // ... (UpdateDoubleLabels 등 중략) ...

    int FindSlotIndexByGlyph(string glyph)
    {
        if (longPressKeys == null) return -1;
        glyph = glyph.Trim();

        for (int i = 0; i < longPressKeys.Length; i++)
        {
            GameObject prefab = null;

            // 1순위: SingleWords 쪽 확인
            if (SingleWords != null && i < SingleWords.Length && SingleWords[i] != null)
            {
                prefab = SingleWords[i];
            }
            
            // 2순위: SingleWords에 없으면(None이면) DSWords(더블키 기본값) 확인
            // 이유: 인스펙터상 앞쪽 인덱스를 더블키들이 쓰고 있음
            if (prefab == null)
            {
                if (DSWords != null && i < DSWords.Length && DSWords[i] != null)
                {
                    prefab = DSWords[i];
                }
            }

            if (!prefab) continue;

            var mag = prefab.GetComponent<JamoMagnet>();
            if (mag != null && mag.glyph == glyph)
                return i;
        }

        return -1;
    }

    void UpdateDoubleLabels()
    {
        if (DoubleText == null) return;

        for (int i = 0; i < DoubleText.Length; i++)
        {
            if (DoubleText[i] == null) continue;

            if (!isShiftPressed)
            {
                if (DSWords != null && i < DSWords.Length && DSWords[i] != null)
                    DoubleText[i].text = DSWords[i].name;
            }
            else
            {
                if (DDWords != null && i < DDWords.Length && DDWords[i] != null)
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

            // [수정] 헬퍼 사용
            Camera camToUse = GetRefinedCamera(uiSpawnRoot);
            var sp = RectTransformUtility.WorldToScreenPoint(camToUse, rt.position);
            if (RectTransformUtility.RectangleContainsScreenPoint(allowedArea, sp, camToUse))
                result.Add(b);
        }

        return result;
    }

    /// <summary>SyllableBlock 리스트를 X좌표 정렬 후 HangulCompose로 문자열로 합친다.</summary>
    bool TryBuildFromBlocks(List<SyllableBlock> blocks, out string word)
    {
        word = null;
        if (blocks == null || blocks.Count == 0) return false;

        Camera camToUse = GetRefinedCamera(uiSpawnRoot);

        // 1) 고아 JamoMagnet이 있는지 검사 (블럭에 안 붙어 있는 자모가 있으면 실패)
        var magnets = uiSpawnRoot.GetComponentsInChildren<JamoMagnet>(includeInactive: false);
        foreach (var m in magnets)
        {
            if (!m) continue;

            var rt = m.GetComponent<RectTransform>();
            if (!rt) continue;

            var sp = RectTransformUtility.WorldToScreenPoint(camToUse, rt.position);
            if (!RectTransformUtility.RectangleContainsScreenPoint(allowedArea, sp, camToUse))
                continue;

            var parentBlock = m.GetComponentInParent<SyllableBlock>();
            if (parentBlock == null)
            {
                // Debug.Log("[TryBuildWord] orphan jamo found → invalid word");
                return false;
            }
        }

        // 2) X좌표 기준으로 블럭 정렬
        var ordered = new List<(SyllableBlock b, float x)>();
        foreach (var b in blocks)
        {
            if (string.IsNullOrEmpty(b.choseong) || string.IsNullOrEmpty(b.jungseong))
            {
                // Debug.Log($"[TryBuildWord] invalid block(need L+V): L='{b.choseong}' V='{b.jungseong}'");
                return false;
            }

            var brt = b.GetComponent<RectTransform>();
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(camToUse, brt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(allowedArea, sp, camToUse, out var local);
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
                // Debug.LogWarning($"[Submit] compose fail from block L='{L}' V='{V}' T='{T}': {e.Message}");
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

            // [수정] Update에서도 동일하게 카메라 보정
            Camera camToUse = uiCamera;
            if (root != null)
            {
                var canvas = root.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    camToUse = null;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, camToUse, out var local))
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
        
        // [수정] 정확한 카메라 사용 (Overlay면 null)
        var root = ResolveUISpawnRoot();
        Camera camToUse = GetRefinedCamera(root);

        // 1) 쓰레기통 위 → 환불 + 삭제
        if (trashArea &&
            RectTransformUtility.RectangleContainsScreenPoint(
                trashArea, lastPointerScreenPos, camToUse))
        {
            if (drag != null) drag.RefundAndDestroy();
            else Destroy(dragUIRect.gameObject);
            SoundManager.Instance.SFXStart(SoundManager.SFXType.trashcan);
            return;
        }

        // 2) 허용 영역 밖 → 환불 + 삭제
        if (allowedArea &&
            !RectTransformUtility.RectangleContainsScreenPoint(
                allowedArea, lastPointerScreenPos, camToUse))
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
        
        // [수정] 좌표 계산 전에 먼저 Root와 Camera를 확정해야 함
        var root = ResolveUISpawnRoot();
        Camera camToUse = GetRefinedCamera(root);

        // [수정] startScreen 계산 시에도 camToUse(Overlay면 null)를 사용해야 정확함
        // 기존에는 무조건 uiCamera를 써서 Overlay 모드일 때 오차 발생 가능성 있었음
        Vector2 startScreen = ev != null
            ? ev.position
            : RectTransformUtility.WorldToScreenPoint(camToUse, buttonRT.position);

        bool isUIPrefab = prefab.GetComponent<RectTransform>() && prefab.GetComponent<CanvasRenderer>();

        if (isUIPrefab)
        {
            // root, camToUse는 위에서 이미 구함

            bool convertSuccess = RectTransformUtility.ScreenPointToLocalPointInRectangle(root, startScreen, camToUse, out var local);
            
            // [디버그] 좌표 변환 실패 시 원인 파악용 로그
            if (!convertSuccess || local == Vector2.zero) 
            {
                 // Debug.LogWarning($"[KeyBoard] Spawn Check - Success:{convertSuccess}, Local:{local}");
                 // Debug.LogWarning($"[KeyBoard] Root: {root.name} / Active: {root.gameObject.activeInHierarchy}");
                 // // Debug.LogWarning($"[KeyBoard] Cam Used: {(camToUse != null ? camToUse.name : "Null")}");
            }

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
                drag.Init(root, allowedArea, trashArea, camToUse);
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
            // Debug.LogWarning($"[KeyBoardManager] '{glyph}' 에 해당하는 슬롯을 찾지 못했어.");
            return false;
        }

        KeyCount.AddAt(index, amount);
        // 아이템으로 준 거라서 _sessionSpent 에는 기록 안 함
        return true;
    }

    public bool CanAddKey(string glyph)
    {
        if (!KeyCount.isReady || string.IsNullOrEmpty(glyph)) return false;
        int index = FindSlotIndexByGlyph(glyph);
        if (index < 0) return false;
        
        return KeyCount.Get(index) < KeyCount.MaxCount;
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
            if (d)
            {
                // [중요] 드래그 중인 물체가 있다면 상태 해제 먼저
                d.ForceStopDrag(); // 드래그 상태 강제 종료 메서드 호출 (DraggableWordUI에 있다고 가정)
                // 만약 저 메서드가 없으면 최소한 DraggableWordUI 내부에서 OnDisable/OnDestroy 시 처리가 되어있어야 함
                
                // 여기서는 안전하게 DOKill 하고 파괴
                d.transform.DOKill();
                Destroy(d.gameObject);
            }

        var magnets = uiSpawnRoot.GetComponentsInChildren<JamoMagnet>(true);
        foreach (var m in magnets)
            if (m) Destroy(m.gameObject);

        // 매니저 측 상태도 초기화
        dragging = false;
        activePointerId = int.MinValue;
        dragUIRect = null;
        dragWorldTr = null;
        _dragOffset = Vector2.zero;
    }

    void NotEnoughFeedback(int index)
    {
        // 키 부족 시 이펙트/사운드 넣고 싶으면 여기서 처리
        Debug.Log($"[KeyBoard] Not enough keys for index {index}. Current: {GetCount(index)}");
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

    public void ConfirmUse()
    {
        _sessionSpent.Clear();   // 환불 기록 버리기 (다시는 돌려주지 않음)
        ClearAllSpawnedPieces(); // 화면 청소
    }
    // -----------------------------
    // 안내 UI 메서드
    // -----------------------------

    /// <summary>
    /// 버튼을 짧게 눌렀을 때 "꾹 누르세요" 경고 표시 (TMP 버전)
    /// </summary>
    public void ShowPushWarning()
    {
        if (PushWarningText == null) return;

        PushWarningText.gameObject.SetActive(true);
        PushWarningText.DOKill();
        PushWarningText.alpha = 1f;

        // 0.5초 대기 후 0.5초 동안 페이드 아웃
        PushWarningText.DOFade(0f, 0.5f).SetDelay(0.5f).OnComplete(() =>
        {
            PushWarningText.gameObject.SetActive(false);
        });
    }
}
