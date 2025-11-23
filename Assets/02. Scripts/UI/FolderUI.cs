using UnityEngine;

public class FolderUI : MonoBehaviour
{
    [SerializeField] private GameObject listPanel;      // 목록 패널(첫 화면)
    [SerializeField] private GalleryDetailView detail;  // 상세 패널(두 번째 화면)

    void Awake()
    {
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
