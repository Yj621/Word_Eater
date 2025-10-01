using UnityEngine;

public class GalleryUIManager : MonoBehaviour
{
    [SerializeField] private Transform content;         // GridLayoutGroup가 붙은 Content
    [SerializeField] private GalleryCell cellPrefab;    // 셀 프리팹
    [SerializeField] private GameObject listPanel;      // 목록 패널(첫 화면)
    [SerializeField] private GalleryDetailView detail;  // 상세 패널(두 번째 화면)

    void Awake()
    {
        if (detail != null) detail.gameObject.SetActive(false);
        if (listPanel != null) listPanel.SetActive(true);
    }
    private System.Collections.IEnumerator Start()
    {
        yield return null; // 1 frame 대기
        Refresh();
    }

    /// <summary>
    /// 목록 갱신 함수
    /// 1) Content 자식 정리
    /// 2) gallery.json을 로드한 GalleryStore의 items를 순회
    /// 3) 셀을 생성하여 Bind 후, 클릭 이벤트로 상세화면 오픈 연결
    /// </summary>
    public void Refresh()
    {
        if (GalleryStore.Instance == null)
        {
            Debug.LogWarning("[GalleryUIManager] GalleryStore가 아직 없습니다.");
            return;
        }

        foreach (Transform t in content) Destroy(t.gameObject);

        var items = GalleryStore.Instance.Data.items;
        Debug.Log($"[GalleryUIManager] items: {items.Count}");

        if (items.Count == 0)
        {
            // 비어있을 때 Empty State를 보여주거나, 디버그로 1개 추가해볼 수 있음
            DebugAddDummy(); // 필요시 테스트용
            return;
        }

        foreach (var item in items)
        {
            var cell = Instantiate(cellPrefab, content);
            cell.Bind(item);
            cell.OnClicked += (clicked) => {
                listPanel.SetActive(false);
                detail.Open(clicked);
            };
        }
    }

    // 테스트용(선택): 도감 항목 강제 하나 추가
    void DebugAddDummy()
    {
        var dummy = new GalleryItem
        {
            id = "debug-1",
            displayName = "디버그",
            desc = "테스트",
            thumbPath = "",
            dateCaught = System.DateTime.Now.ToString("yyyy-MM-dd"),
            meetCount = 1
        };
        GalleryStore.Instance.Upsert(dummy);
        Debug.Log("[GalleryUIManager] Dummy 추가, 다시 Refresh 호출");
        Refresh();
    }


    /// <summary>
    /// 뒤로가기(상세 → 목록) 버튼에서 호출
    /// </summary>
    public void BackToList()
    {
        detail.Close();
        listPanel.SetActive(true);
    }
}
