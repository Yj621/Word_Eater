using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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


    public void PressSingle(int index) => PressSingle(index, null);
    public void PressDouble(int index) => PressDouble(index, null);

    // 롱프레스 + 드래그 시작 (PointerEventData 포함)
    public void PressSingle(int index, PointerEventData ev)
    {
        if (!IsValidIndex(index, SingleWordButtons, SingleWords)) return;
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
        BeginDragSpawn(btn, prefab, ev);
    }

    public void PressShift()
    {
        isShiftPressed = !isShiftPressed;
        UpdateDoubleLabels();
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
        Debug.Log(dragging);
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
