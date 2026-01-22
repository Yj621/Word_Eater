using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems; // DOTween 활용

public class FolderPaging : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private float pageWidth = 800f; // 폴더 가로 크기
    [SerializeField] private float snapSpeed = 0.3f; // 스냅 속도

    private int _currentPage = 0;

    // 드래그가 끝났을 때 호출 (IEndDragHandler 인터페이스 구현)
    public void OnEndDrag(PointerEventData eventData)
    {
        // 현재 컨텐트의 X 좌표를 기준으로 몇 번째 페이지에 가까운지 계산
        // Content의 X는 왼쪽으로 갈수록 마이너스 값이므로 절대값을 사용합니다.
        float currentPosX = Mathf.Abs(content.anchoredPosition.x);

        // 반올림을 통해 가장 가까운 페이지 인덱스를 구함
        _currentPage = Mathf.RoundToInt(currentPosX / pageWidth);

        // 최대 페이지 제한 (아이템 개수에 따라 동적으로 조절 가능)
        int maxPage = Mathf.Max(0, Mathf.CeilToInt(content.rect.width / pageWidth) - 1);
        _currentPage = Mathf.Clamp(_currentPage, 0, maxPage);

        SnapToPage(_currentPage);
    }

    public void SnapToPage(int pageIndex)
    {
        float targetPosX = -pageIndex * pageWidth;

        // DOTween을 이용한 부드러운 이동
        content.DOAnchorPosX(targetPosX, snapSpeed).SetEase(Ease.OutCubic);

        Debug.Log($"현재 페이지: {pageIndex + 1}");
    }

    // 좌우 버튼(< >) 기능용
    public void MoveNext() => SnapToPage(_currentPage + 1);
    public void MovePrev() => SnapToPage(_currentPage - 1);
}