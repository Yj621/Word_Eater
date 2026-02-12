using UnityEngine;
using System.IO;
using UnityEngine.UI;

/// <summary>
/// 스프라이트를 잘라 정사각형 썸네일 PNG로 저장하는 유틸리티
/// 파일은 persistentDataPath 하위에 저장된다
/// </summary>
public static class GalleryCapture
{
    /// <summary>
    /// [UI Image용] 임시로 SpriteRenderer를 만들어 촬영 로직을 위임
    /// Read/Write Enabled 설정이나 아틀라스 여부와 상관없이 작동
    /// </summary>
    public static string SaveSpriteThumb(Image img, string fileNameNoExt, int size = 256)
    {
        if (img == null || img.sprite == null) return null;

        // 촬영을 위한 임시 게임오브젝트 생성
        GameObject tempGo = new GameObject("TempCaptureObject");

        // SpriteRenderer 부착 및 Image의 스프라이트 복사
        SpriteRenderer sr = tempGo.AddComponent<SpriteRenderer>();
        sr.sprite = img.sprite;

        // sr.color = img.color; 

        // 이미 잘 작동하는 SpriteRenderer용 촬영 함수를 호출 (재사용)
        string path = SaveSpriteThumb(sr, fileNameNoExt, size);

        // 임시 객체 삭제
        Object.Destroy(tempGo);

        return path;
    }

    /// <summary>
    /// [SpriteRenderer용] 별도의 카메라를 생성하여 대상을 렌더링 후 저장
    /// </summary>
    public static string SaveSpriteThumb(SpriteRenderer sr, string fileNameNoExt, int size = 256)
    {
        if (sr == null || sr.sprite == null) return null;

        // 임시 카메라/RT 준비
        var go = new GameObject("ThumbCam");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.clear;
        cam.cullingMask = 1 << 30; // 30번 레이어(임시)만 렌더링

        int originalLayer = sr.gameObject.layer;
        sr.gameObject.layer = 30; // 대상을 임시 레이어로 이동

        // 스프라이트를 꽉 차게 보이도록 카메라 프레이밍
        var b = sr.bounds; // 월드 공간 바운딩

        // 바운딩 박스가 0일 경우 대비 (거의 없지만 안전장치)
        if (b.size == Vector3.zero) b = new Bounds(sr.transform.position, Vector3.one);

        float halfH = b.extents.y;
        float halfW = b.extents.x;
        float aspect = 1f; // 정사각 썸네일

        // 여백 없이 꽉 차게 찍기 위해 Max 사용
        cam.orthographicSize = Mathf.Max(halfH, halfW / aspect);
        cam.transform.position = b.center + new Vector3(0, 0, -10);

        // 렌더 → ReadPixels
        var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt); // RT 해제

        // 저장
        byte[] png = tex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, $"{fileNameNoExt}.png");
        File.WriteAllBytes(path, png);

        // 정리/복구
        sr.gameObject.layer = originalLayer; // 레이어 원상복구
        Object.Destroy(tex);
        Object.Destroy(go); // 카메라 삭제

        return path;
    }
}