using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// 도감 상세 화면(두 번째 화면)
/// 선택된 항목을 받아 제목/날짜/횟수/이미지를 표시
/// </summary>
public class GalleryDetailView : MonoBehaviour
{
    [SerializeField] private RawImage thumb1, thumb2, thumb3;
    [SerializeField] private TMP_Text titleText, dateText, countText;

    public void Open(GalleryItem item)
    {
        gameObject.SetActive(true);

        titleText.text = item.displayName;
        dateText.text = item.dateCaught;
        countText.text = $"만난 횟수 : {item.meetCount}";

        string baseDir = Application.persistentDataPath;

        // ID 기반으로 3단계 경로 생성
        // 예: thumb_2-천문_s0.png, thumb_2-천문_s1.png, thumb_2-천문_s2.png
        string pathBit = Path.Combine(baseDir, $"thumb_{item.id}_s0.png");
        string pathByte = Path.Combine(baseDir, $"thumb_{item.id}_s1.png");
        string pathWord = Path.Combine(baseDir, $"thumb_{item.id}_s2.png");

        // 각각 로드 및 적용
        ApplyTexture(thumb1, pathBit);
        ApplyTexture(thumb2, pathByte);
        ApplyTexture(thumb3, pathWord); // <- 여기가 문제의 s2 이미지
    }

    /// <summary>
    /// 텍스처를 로드하고 RawImage에 적용하는 헬퍼 함수
    /// (기존 텍스처 삭제 + 파일 없으면 투명 처리)
    /// </summary>
    private void ApplyTexture(RawImage target, string path)
    {
        // 1. 기존 텍스처 메모리 해제 (필수)
        if (target.texture != null)
        {
            Destroy(target.texture);
            target.texture = null;
        }

        // 2. 파일 로드 시도
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); // Mipmap 끔
            if (tex.LoadImage(bytes))
            {
                target.texture = tex;
                target.color = Color.white; // 이미지가 보임
                return;
            }
        }

        // 3. 파일이 없거나 로드 실패 시
        target.texture = null;

        // 파일이 없으면 안 보이게 투명 처리 (회색 박스가 싫다면 Color.clear 사용)
        target.color = Color.clear;
    }

    public void Close()
    {
        // 닫을 때도 메모리 정리해주면 좋음
        CleanUp(thumb1);
        CleanUp(thumb2);
        CleanUp(thumb3);
        gameObject.SetActive(false);
    }

    private void CleanUp(RawImage target)
    {
        if (target.texture != null)
        {
            Destroy(target.texture);
            target.texture = null;
        }
    }
}
