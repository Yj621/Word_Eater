using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DragableHeart : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform Rect => _rect;

    private RectTransform _rect;
    private CanvasGroup _cg;
    private Canvas _canvas;
    private RectTransform _playArea;
    private RectTransform _folderZone;
    private Action<DragableHeart> _onPlaced;

    private Vector2 _startPos;
    private Transform _startParent;
    private bool _locked;

    // 외부에서 초기화
    public void Init(Canvas canvas, RectTransform playArea, RectTransform folderZone, Action<DragableHeart> onPlaced)
    {
        _canvas = canvas;
        _playArea = playArea;
        _folderZone = folderZone;
        _onPlaced = onPlaced;
    }

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        _startPos = _rect.anchoredPosition;
        _startParent = _rect.parent;
        _locked = false;
        if (_cg)
        {
            _cg.blocksRaycasts = true;
            _cg.interactable = true;
        }
    }

    public void Lock()
    {
        _locked = true;
        if (_cg)
        {
            _cg.blocksRaycasts = false;
            _cg.interactable = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_locked) return;

        _startPos = _rect.anchoredPosition;
        _startParent = _rect.parent;

        if (_cg) _cg.blocksRaycasts = false; // 드래그 중 폴더가 레이캐스트 받게

        // 최상단으로
        _rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_locked || _canvas == null) return;

        var cam = GetCanvasCamera(_canvas);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _playArea, eventData.position, cam, out var localPos))
        {
            _rect.anchoredPosition = localPos;
            ClampInsidePlayArea();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_locked) return;

        if (_cg) _cg.blocksRaycasts = true;

        var cam = GetCanvasCamera(_canvas);
        bool overFolder = RectTransformUtility.RectangleContainsScreenPoint(_folderZone, eventData.position, cam);

        if (overFolder)
        {
            // 성공 처리: 잠그고 카운트 보고, 바로 사라지게
            Lock();
            _onPlaced?.Invoke(this);
            Destroy(gameObject);
        }
        else
        {
            // 원위치 복귀
            _rect.SetParent(_startParent, worldPositionStays: false);
            _rect.anchoredPosition = _startPos;
        }
    }

    private Camera GetCanvasCamera(Canvas c)
    {
        // Overlay는 null, 나머지는 worldCamera 사용
        if (c.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return c.worldCamera;
    }

    private void ClampInsidePlayArea()
    {
        // 하트가 영역 밖으로 나가지 않게 살짝 클램프 (pivot 0.5,0.5 기준)
        var area = _playArea.rect;
        var sz = _rect.rect.size;

        float xHalf = (area.width - sz.x) * 0.5f;
        float yHalf = (area.height - sz.y) * 0.5f;

        var p = _rect.anchoredPosition;
        p.x = Mathf.Clamp(p.x, -xHalf, xHalf);
        p.y = Mathf.Clamp(p.y, -yHalf, yHalf);
        _rect.anchoredPosition = p;
    }
}
