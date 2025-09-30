using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PuzzlePiece2X2 : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public RectTransform Rect => _rect;
    public int CorrectSlotIndex { get; private set; }
    public int CurrentSlotIndex { get; private set; } = -1;
    public bool IsAtCorrectRotation => (rotationSteps % 4) == 0; // 정답은 0°로 가정
    public bool isCountedCorrect { get; set; } // 매니저가 카운팅 중인지 표시

    [Header("조각 이미지")]
    public Image image;

    [Header("더블탭 시간(초)")]
    public float doubleTapWindow = 0.3f;

    Puzzle2X2Game manager;
    Canvas canvas;
    RectTransform _rect;
    CanvasGroup _cg;

    Vector2 startLocalPos;
    Transform startParent;
    float lastTapTime = -10f;
    int rotationSteps = 0; // 90° 단위, 0=정답 각

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (image == null) image = GetComponent<Image>();
    }

    public void Setup(Puzzle2X2Game manager, Canvas canvas, int correctSlotIndex, Sprite sprite)
    {
        this.manager = manager;
        this.canvas = canvas;
        this.CorrectSlotIndex = correctSlotIndex;

        if (image) image.sprite = sprite;

        transform.localRotation = Quaternion.identity;
        rotationSteps = 0;
        isCountedCorrect = false;
        CurrentSlotIndex = -1;
    }

    public void SetRotationSteps(int steps)
    {
        rotationSteps = ((steps % 4) + 4) % 4;
        Rect.localRotation = Quaternion.Euler(0, 0, -90f * rotationSteps);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = false;
        startLocalPos = _rect.anchoredPosition;
        startParent = _rect.parent;
        _rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        var cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)startParent, eventData.position, cam, out var lp))
        {
            _rect.anchoredPosition = lp;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;

        // 가까운 슬롯 찾기
        int slotIdx = manager.GetSnapSlotIndex(eventData.position);
        if (slotIdx == -1)
        {
            // 스냅 불가 → 원위치
            _rect.SetParent(startParent, false);
            _rect.anchoredPosition = startLocalPos;
            manager.RecheckPieceState(this);
            return;
        }

        // 점유 시도
        if (!manager.TryOccupySlot(slotIdx, this))
        {
            // 이미 차 있으면 복귀
            _rect.SetParent(startParent, false);
            _rect.anchoredPosition = startLocalPos;
            manager.RecheckPieceState(this);
            return;
        }

        // 슬롯에 스냅
        var slot = manager.GetSlot(slotIdx);
        _rect.SetParent(slot, false);
        _rect.anchoredPosition = Vector2.zero;
        CurrentSlotIndex = slotIdx;

        manager.RecheckPieceState(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // PC: 우클릭 → 회전
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RotateCW();
            return;
        }

        // 모바일/좌클릭: 더블탭 체크
        float now = Time.unscaledTime;
        if (now - lastTapTime <= doubleTapWindow)
        {
            RotateCW();
            lastTapTime = -10f;
        }
        else
        {
            lastTapTime = now;
        }
    }

    void RotateCW()
    {
        SetRotationSteps(rotationSteps + 1);
        if (manager != null) manager.RecheckPieceState(this);
    }
}
