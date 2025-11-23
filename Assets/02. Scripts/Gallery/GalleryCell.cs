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

        // 기존 텍스처 정리
        if (thumb.texture != null)
        {
            Destroy(thumb.texture);
            thumb.texture = null;
        }

        // 1. 경로가 비어있는지 확인
        if (string.IsNullOrEmpty(item.thumbPath))
        {
            Debug.LogError($"[GalleryCell] 썸네일 경로가 비어있습니다! ID: {item.id}");
            thumb.color = Color.red; // 오류 시 빨간색으로 표시해서 눈에 띄게 함
            return;
        }

        // 2. 실제 파일이 존재하는지 확인
        if (File.Exists(item.thumbPath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(item.thumbPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (tex.LoadImage(bytes))
                {
                    thumb.texture = tex;
                    thumb.color = Color.white;
                    // Debug.Log($"[GalleryCell] 로드 성공! 크기: {tex.width}x{tex.height} / 경로: {item.thumbPath}");
                }
                else
                {
                    Debug.LogError($"[GalleryCell] 이미지는 찾았으나 로드 실패 (데이터 손상 가능성): {item.thumbPath}");
                    thumb.color = Color.magenta; // 로드 실패 시 자주색
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GalleryCell] 파일 읽기 에러: {e.Message}");
            }
        }
        else
        {
            // 3. 파일이 없음
            Debug.LogWarning($"[GalleryCell] 파일이 존재하지 않습니다. 경로: {item.thumbPath}");
            thumb.texture = null;
            thumb.color = Color.gray; // 파일 없으면 회색 처리 (기존 Clear는 너무 안 보여서 수정)
        }

        clickArea.onClick.RemoveAllListeners();
        clickArea.onClick.AddListener(() => OnClicked?.Invoke(boundItem));
    }

    // 오브젝트가 파괴될 때도 텍스처 정리
    private void OnDestroy()
    {
        if (thumb.texture != null)
        {
            Destroy(thumb.texture);
        }
    }
}