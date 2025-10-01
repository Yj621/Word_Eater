using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

/// <summary>
/// 도감의 "한 칸(셀)"을 표현하는 컴포넌트.
/// - 썸네일/텍스트를 표시하고
/// - 클릭되면 상위 매니저(GalleryUIManager)에게 어떤 아이템이 클릭됐는지 알려준다.
/// </summary>
public class GalleryCell : MonoBehaviour
{
    [SerializeField] private Button clickArea;  // 셀 전체를 덮는 버튼
    [SerializeField] private RawImage thumb;    // 썸네일 이미지

    private GalleryItem boundItem;              // 현재 셀에 바인딩된 데이터
    public event Action<GalleryItem> OnClicked; // 외부(리스트 매니저)로 클릭 알림

    /// <summary>
    /// 셀에 도감 항목 데이터를 바인딩한다.
    /// ※ (간단 버전) 썸네일만 로드해서 보여주고, 클릭 이벤트만 연결
    /// </summary>
    public void Bind(GalleryItem item)
    {
        boundItem = item;

        // 썸네일 텍스처 로드(없으면 null)
        if (File.Exists(item.thumbPath))
        {
            var bytes = File.ReadAllBytes(item.thumbPath);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            thumb.texture = tex;
        }
        else
        {
            thumb.texture = null;
        }

        // 클릭 시 현재 바운드된 아이템을 이벤트로 전달
        clickArea.onClick.RemoveAllListeners();
        clickArea.onClick.AddListener(() => OnClicked?.Invoke(boundItem));
    }
}
