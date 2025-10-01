using UnityEngine;
using System.IO;

/// <summary>
/// 스프라이트를 잘라 정사각형 썸네일 PNG로 저장하는 유틸리티
/// 파일은 persistentDataPath 하위에 저장된다
/// </summary>
public static class GalleryCapture
{
    /// <summary>
    /// 스프라이트에서 텍스처 영역만 잘라 size x size로 리사이즈 후 PNG 저장
    /// 반환값: 저장된 로컬 경로
    /// </summary>
    public static string SaveSpriteThumb(Sprite sp, string fileNameNoExt, int size = 256)
    {
        // ❗이 함수는 Read/Write가 꺼져있거나 아틀라스면 실패할 수 있음
        Texture2D src = sp.texture;
        Rect r = sp.textureRect;

        var tmp = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
        var pixels = src.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height); // 여기서 예외 발생
        tmp.SetPixels(pixels); tmp.Apply();

        var thumb = ScaleTo(tmp, size, size);
        byte[] png = thumb.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, $"{fileNameNoExt}.png");
        File.WriteAllBytes(path, png);
        Object.Destroy(tmp);
        Object.Destroy(thumb);
        return path;
    }
    public static string SaveSpriteThumb(SpriteRenderer sr, string fileNameNoExt, int size = 256)
    {
        // 1) 임시 카메라/RT 준비
        var go = new GameObject("ThumbCam");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.clear;
        cam.cullingMask = 1 << 30; // 임시 레이어 전용 렌더

        int originalLayer = sr.gameObject.layer;
        sr.gameObject.layer = 30; // 임시 레이어로 이동

        // 2) 스프라이트를 꽉 차게 보이도록 카메라 프레이밍
        var b = sr.bounds; // 월드 공간 바운딩
        float halfH = b.extents.y;
        float halfW = b.extents.x;
        float aspect = 1f; // 정사각 썸네일
        cam.orthographicSize = Mathf.Max(halfH, halfW / aspect);
        cam.transform.position = b.center + new Vector3(0, 0, -10);

        // 3) 렌더 → ReadPixels
        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        cam.targetTexture = null;

        // 4) 저장
        byte[] png = tex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, $"{fileNameNoExt}.png");
        File.WriteAllBytes(path, png);

        // 5) 정리/복구
        sr.gameObject.layer = originalLayer;
        Object.Destroy(tex);
        Object.Destroy(rt);
        Object.Destroy(go);

        return path;
    }

    static Texture2D ScaleTo(Texture2D src, int w, int h)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }
}
