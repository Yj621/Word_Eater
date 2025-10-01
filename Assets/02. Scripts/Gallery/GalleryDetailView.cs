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
    [SerializeField] private RawImage thumb1, thumb2, thumb3; // 예시로 3개 영역(같은 이미지 사용)
    [SerializeField] private TMP_Text titleText, dateText, countText;

    /// <summary>
    /// 상세 화면을 열어 선택된 아이템 정보로 UI를 채움
    /// </summary>
    public void Open(GalleryItem item)
    {
        gameObject.SetActive(true);

        titleText.text = item.displayName;
        dateText.text = item.dateCaught;
        countText.text = $"만난 횟수 : {item.meetCount}";

        // id 기반 파일 경로 만들기
        string baseDir = Application.persistentDataPath;
        string pathBit = Path.Combine(baseDir, $"thumb_{item.id}_s0.png");
        string pathByte = Path.Combine(baseDir, $"thumb_{item.id}_s1.png");
        string pathWord = Path.Combine(baseDir, $"thumb_{item.id}_s2.png");

        // 로더
        Texture2D LoadTex(string p)
        {
            if (!File.Exists(p)) return null;
            var t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(p));
            return t;
        }

        // Bit / Byte / Word 순서로 바인딩
        thumb1.texture = LoadTex(pathBit);
        thumb2.texture = LoadTex(pathByte);
        thumb3.texture = LoadTex(pathWord);
    }

    public void Close() => gameObject.SetActive(false);
}
