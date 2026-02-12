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
        
        // [수정] 스냅할 슬롯이 없으면? -> 그냥 놓은 자리에 둠 (다시 돌아가지 않음)
        if (slotIdx == -1)
        {
            // 원래 있던 슬롯은 비워줌
            manager.VacateSlot(this);
            CurrentSlotIndex = -1;

            // 부모를 트레이(SpawnArea)나 최상위로 변경해 유지
            // (여기선 activeSelf 체크 없이 그냥 spawnArea로 보낸다고 가정)
            if (manager.spawnArea != null)
                _rect.SetParent(manager.spawnArea, true);

            manager.RecheckPieceState(this);
            return;
        }

        // 점유 시도
        if (!manager.TryOccupySlot(slotIdx, this))
        {
            // 이미 차 있으면? -> 여기서도 그냥 튕겨내거나 제자리에 둠.
            // 기획 의도상 "다른데 두면 다시 붙지 않고 그 곳에 있게" 하려면
            // 실패 시 원위치보다는 현재 위치 유지가 맞으나, 
            // 슬롯 위에 겹쳐 보이면 곤란하므로 여기서는 "튕겨내기(트레이 근처)" 혹은 "제자리" 중 선택.
            // 일단은 "슬롯 진입 실패 -> 그냥 그 근처에 둠" 처리 (위와 동일 로직)
            
            manager.VacateSlot(this);
            CurrentSlotIndex = -1;
            if (manager.spawnArea != null)
                _rect.SetParent(manager.spawnArea, true);
            
            manager.RecheckPieceState(this);
            return;
        }

        // 슬롯에 스냅 성공
        var slot = manager.GetSlot(slotIdx);
        _rect.SetParent(slot, false);
        _rect.anchoredPosition = Vector2.zero;
        CurrentSlotIndex = slotIdx;

        manager.RecheckPieceState(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // [수정] 회전 기능 삭제 요청으로 인하여 입력 무시
        return;

        /*
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
        */
    }

    void RotateCW()
    {
        // [수정] 회전 기능 삭제
        // SetRotationSteps(rotationSteps + 1);
        // if (manager != null) manager.RecheckPieceState(this);
    }
}
