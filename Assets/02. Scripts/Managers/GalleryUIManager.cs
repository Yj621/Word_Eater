using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GalleryUIManager : MonoBehaviour
{
    [SerializeField] private Transform content;         // GridLayoutGroup가 붙은 Content
    [SerializeField] private GalleryCell cellPrefab;    // 셀 프리팹
    [SerializeField] private GameObject listPanel;      // 목록 패널(첫 화면)
    [SerializeField] private GalleryDetailView detail;  // 상세 패널(두 번째 화면)

    [Header("비었을 때 표시할 UI")]
    [SerializeField] private GameObject emptyStatePanel;

    void Awake()
    {
        if (detail != null) detail.gameObject.SetActive(false);
        if (listPanel != null) listPanel.SetActive(true);
    }

    private IEnumerator Start()
    {
        yield return null; // 1 frame 대기 (FileManager 초기화 대기)
        Refresh();
    }

    /// <summary>
    /// 목록 갱신 함수
    /// 1) Content 자식 정리
    /// 2) FileManager의 galleryData를 순회
    /// 3) 셀을 생성하여 Bind 후, 클릭 이벤트로 상세화면 오픈 연결
    /// </summary>
    public void Refresh()
    {
        if (FileManager.Instance == null)
        {
            Debug.LogWarning("[GalleryUIManager] FileManager가 없습니다.");
            return;
        }

        foreach (Transform t in content) Destroy(t.gameObject);

        // FileManager에서 데이터 가져오기
        var items = FileManager.Instance.galleryData.items;

        if (items.Count == 0)
        {
            emptyStatePanel?.SetActive(true);
            return;
        }
        else
        {
            emptyStatePanel?.SetActive(false);
        }

        // (이하 동일)
        foreach (var item in items)
        {
            var cell = Instantiate(cellPrefab, content);
            cell.Bind(item);
            cell.OnClicked += (clicked) =>
            {
                listPanel.SetActive(false);
                detail.Open(clicked);
            };
        }
    }


    /// <summary>
    /// 뒤로가기(상세 → 목록) 버튼에서 호출
    /// </summary>
    public void BackToList()
    {
        if (detail != null) detail.Close();
        if (listPanel != null) listPanel.SetActive(true);
    }
}