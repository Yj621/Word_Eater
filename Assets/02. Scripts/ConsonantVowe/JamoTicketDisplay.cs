using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WordEater.Systems;

public class JamoTicketDisplay : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject ticketPrefab;
    [SerializeField] private GameObject pagePrefab;     // ★ 추가: Page 프리팹(안에 GridLayoutGroup 있음)

    [Header("Scroll")]
    [SerializeField] private RectTransform content;     // ★ ScrollRect의 Content
    [SerializeField] private RectTransform viewport;    // ★ ScrollRect의 Viewport

    [SerializeField] private ItemType targetItem = ItemType.JamoSelectionTicket;


    [Header("Scene References for Spawned Button")]
    [SerializeField] private JamoChooserUI chooserPanel;
    [SerializeField] private Transform targetPanel;
    [SerializeField] private GameObject folderPanel;
    [SerializeField] private GameObject jamoConfirmPanel;
    [SerializeField] private GameObject closePanel;

    private readonly List<GameObject> _spawnedTickets = new();
    private readonly List<RectTransform> _pages = new();

    private const int ITEMS_PER_PAGE = 9;
    private void Start()
    {
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnInventoryChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        if (ItemManager.Instance == null) return;

        int count = ItemManager.Instance.GetCount(targetItem);

        // 1) 티켓 수 맞추기
        while (_spawnedTickets.Count < count) SpawnTicket();
        while (_spawnedTickets.Count > count) RemoveTicket();

        // 2) 페이지 재배치(인덱스 기준으로 올바른 페이지로 이동)
        RelayoutAll();

        // 3) Content 사이즈 갱신
        UpdateContentSize();

        // 4) 레이아웃 강제 갱신
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void SpawnTicket()
    {
        if (ticketPrefab == null) return;

        int index = _spawnedTickets.Count;
        RectTransform page = GetOrCreatePage(index / ITEMS_PER_PAGE);

        GameObject go = Instantiate(ticketPrefab, page);
        go.SetActive(true);

        var comp = go.GetComponent<ClickIconChoiceJamo>();
        if (comp != null)
            comp.Initialize(chooserPanel, targetPanel, folderPanel, jamoConfirmPanel, closePanel, gameObject);

        _spawnedTickets.Add(go);
    }

    private void RemoveTicket()
    {
        if (_spawnedTickets.Count == 0) return;

        int lastIdx = _spawnedTickets.Count - 1;
        var go = _spawnedTickets[lastIdx];
        _spawnedTickets.RemoveAt(lastIdx);
        Destroy(go);

        CleanupEmptyPages();
    }

    private void RelayoutAll()
    {
        for (int i = 0; i < _spawnedTickets.Count; i++)
        {
            RectTransform correctPage = GetOrCreatePage(i / ITEMS_PER_PAGE);
            var t = _spawnedTickets[i].transform;
            if (t.parent != correctPage)
                t.SetParent(correctPage, false);
        }

        CleanupEmptyPages();
        UpdatePagesPosition();
    }

    private RectTransform GetOrCreatePage(int pageIndex)
    {
        while (_pages.Count <= pageIndex)
        {
            var pageGo = Instantiate(pagePrefab, content);
            var pageRt = pageGo.GetComponent<RectTransform>();
            _pages.Add(pageRt);
        }
        return _pages[pageIndex];
    }

    private void CleanupEmptyPages()
    {
        // 마지막 페이지들이 비었으면 제거
        for (int i = _pages.Count - 1; i >= 0; i--)
        {
            if (_pages[i].childCount == 0 && i == _pages.Count - 1)
            {
                Destroy(_pages[i].gameObject);
                _pages.RemoveAt(i);
            }
            else break;
        }
    }

    private void UpdatePagesPosition()
    {
        float w = viewport.rect.width;
        float h = viewport.rect.height;

        for (int i = 0; i < _pages.Count; i++)
        {
            RectTransform p = _pages[i];
            p.anchorMin = new Vector2(0, 1);
            p.anchorMax = new Vector2(0, 1);
            p.pivot = new Vector2(0, 1);
            p.sizeDelta = new Vector2(w, h);
            p.anchoredPosition = new Vector2(i * w, 0); // ★ 가로로 페이지 배치
        }
    }

    private void UpdateContentSize()
    {
        int pageCount = Mathf.Max(1, _pages.Count);
        float w = viewport.rect.width;
        float h = viewport.rect.height;

        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(0, 1);
        content.pivot = new Vector2(0, 1);
        content.sizeDelta = new Vector2(pageCount * w, h);

        var snap = content.GetComponentInParent<HorizontalPageSnap>();
        if (snap != null) snap.Refresh();

        UpdatePagesPosition();
    }
    // 테스트용: 버튼에 연결
    public void Test_AddTicket()
    {
        SpawnTicket();
        RelayoutAll();
        UpdateContentSize();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}