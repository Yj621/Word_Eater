using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PageIndicatorDots : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HorizontalPageSnap snap; // 위 스냅 컴포넌트
    [SerializeField] private Transform dotsParent;     // 점들이 들어갈 부모(가로 Layout 권장)
    [SerializeField] private Image dotPrefab;          // Image 프리팹(원형 스프라이트)

    [Header("Style")]
    [SerializeField] private float inactiveAlpha = 0.35f;
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private Vector2 inactiveSize = new Vector2(10, 10);
    [SerializeField] private Vector2 activeSize = new Vector2(14, 14);

    private readonly List<Image> dots = new();

    private void Awake()
    {
        if (dotsParent == null) dotsParent = transform;
    }

    private void OnEnable()
    {
        if (snap != null)
        {
            snap.OnPageChanged += HandlePageChanged;
            Rebuild(snap.CurrentPage, GetPageCountSafe());
        }
    }

    private void OnDisable()
    {
        if (snap != null) snap.OnPageChanged -= HandlePageChanged;
    }

    private int GetPageCountSafe()
    {
        // snap.Refresh() 호출 후에 pageCount가 확정되는 흐름이 보통이라,
        // 여기서는 이벤트를 신뢰하는 방식으로 설계했음.
        // 그래도 초기 1로는 잡히게.
        return 1;
    }

    private void HandlePageChanged(int current, int pageCount)
    {
        Rebuild(current, pageCount);
    }

    public void Rebuild(int currentPage, int pageCount)
    {
        pageCount = Mathf.Max(1, pageCount);

        // dot 개수 맞추기
        while (dots.Count < pageCount)
        {
            var dot = Instantiate(dotPrefab, dotsParent);
            dots.Add(dot);
        }
        while (dots.Count > pageCount)
        {
            var last = dots[^1];
            dots.RemoveAt(dots.Count - 1);
            if (last != null) Destroy(last.gameObject);
        }

        // 활성 표시
        for (int i = 0; i < dots.Count; i++)
        {
            bool active = (i == currentPage);
            var img = dots[i];
            if (img == null) continue;

            var c = img.color;
            c.a = active ? activeAlpha : inactiveAlpha;
            img.color = c;

            var rt = img.rectTransform;
            rt.sizeDelta = active ? activeSize : inactiveSize;
        }

        // 페이지가 1개면 숨기고 싶으면:
        // gameObject.SetActive(pageCount > 1);
    }
}
