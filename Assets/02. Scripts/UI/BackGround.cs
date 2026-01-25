using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class BackGround : MonoBehaviour
{
    public Image targetImage;
    private string filePath;

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "bg.png");
        LoadBGImageIfExists();
    }

    //갤러리에서 이미지 선택
    public void PickAndSaveBGImage()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Texture2D tex = NativeGallery.LoadImageAtPath(path, 1024, false);
            if (tex == null) return;

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
                );

            targetImage.sprite = sprite;

            SaveTexture(tex);
            //LoadBGImageIfExists();
        }, "배경 이미지 선택");
    }

    void SaveTexture(Texture2D tex)
    {
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, png);
        //ApplyTexture(tex);
    }

    // 이미지 -> 스프라이트 연결
    public void LoadBGImageIfExists()
    {
        if (!File.Exists(filePath)) return;

        byte[] bytes = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        ApplyTexture(tex);
    }

    void ApplyTexture(Texture2D tex)
    {
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        targetImage.sprite = sprite;
    }

}
