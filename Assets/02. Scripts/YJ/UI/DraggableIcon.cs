using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("참조 설정")]
    public UIGridArea gridArea;     // 부모 Grid
    public Canvas canvas;           // 최상위 캔버스 (드래그 시 위로 띄우기 위함)

    private RectTransform _rt;
    private CanvasGroup _canvasGroup;
    private Transform _originalParent;

    // 빈 공간을 유지해줄 가짜 객체
    private GameObject _placeholder;

    public SlideManager slidemanager;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = _rt.parent;

        int originalIndex = _rt.GetSiblingIndex(); // 원래 인덱스 미리 기억
         // ✅ 아이콘을 먼저 Canvas로 옮김 (Grid에서 제거)
        _rt.SetParent(canvas.transform);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.6f;

        // ✅ 아이콘이 빠진 자리에 Placeholder 생성
        CreatePlaceholder(originalIndex);
    }
    private void CreatePlaceholder(int siblingIndex)
    {
        _placeholder = new GameObject("Placeholder");
        _placeholder.transform.SetParent(_originalParent);

        var le = _placeholder.AddComponent<LayoutElement>();
        le.preferredWidth = _rt.rect.width;
        le.preferredHeight = _rt.rect.height;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        // ✅ 아이콘이 있던 정확한 자리에 배치 (이미 아이콘은 빠진 상태)
        _placeholder.transform.SetSiblingIndex(siblingIndex);
    }
    public void OnDrag(PointerEventData eventData)
    {
        slidemanager.isOK = false;

        // 아이콘을 마우스 따라 이동
        _rt.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // 현재 내 위치가 Grid의 어느 인덱스에 해당하는지 계산
        if (gridArea != null)
        {
            int newIndex = gridArea.GetInsertIndex(_rt.position);

            // Placeholder의 순서를 바꿔주면, GridLayoutGroup이 알아서 아이콘들을 밀어냄
            _placeholder.transform.SetSiblingIndex(newIndex);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        StartCoroutine(SetIsOKNextFrame());

        // 아이콘 상태 복구
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1.0f;

        // 아이콘을 다시 원래 부모(Grid)로 복귀
        _rt.SetParent(_originalParent);

        // Placeholder가 있던 위치(인덱스)로 내 자리를 확정
        int finalIndex = _placeholder.transform.GetSiblingIndex();
        _rt.SetSiblingIndex(finalIndex);

        // Placeholder 삭제
        Destroy(_placeholder);
    }

    IEnumerator SetIsOKNextFrame()
    {
        yield return null; // 1 frame 대기
        slidemanager.isOK = true;
    }

    /// <summary>
    /// GridLayoutGroup 안에서 공간을 차지할 투명한 더미 오브젝트 생성
    /// </summary>
    private void CreatePlaceholder()
    {
        _placeholder = new GameObject("Placeholder");
        _placeholder.transform.SetParent(_originalParent);

        // 크기를 내 아이콘과 똑같이 설정하여 레이아웃 유지
        var le = _placeholder.AddComponent<LayoutElement>();
        le.preferredWidth = _rt.rect.width;
        le.preferredHeight = _rt.rect.height;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        // 내 원래 위치(순서)에 배치
        _placeholder.transform.SetSiblingIndex(_rt.GetSiblingIndex());
    }
}