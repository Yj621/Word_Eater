using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class BackGround : MonoBehaviour
{
    public Image targetImage;

    public Texture2D basic;

    private string filePath;

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "bg.png");
        LoadBGImageIfExists();
    }

    //갤러리에서 이미지 선택
    public void PickAndSaveBGImage()
    {
        // NativeGallery 최신 버전 및 유니티 6 대응: MediaType 인자 추가
        bool hasPermission = NativeGallery.CheckPermission(NativeGallery.PermissionType.Read, NativeGallery.MediaType.Image);

        if (!hasPermission)
        {
            // 권한이 없다면 네이티브 팝업 호출 (설정창 유도)
            ShowNativeDialog();
            return;
        }

        // 2. 이미지 선택 실행 (이 버전의 GetImageFromGallery는 void를 반환함)
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (string.IsNullOrEmpty(path)) return;

            // NativeGallery 전용 로드 함수 사용 (성능 및 메모리에 더 좋음)
            Texture2D tex = NativeGallery.LoadImageAtPath(path, 1024, false);
            if (tex == null) return;

            ApplyTexture(tex);
            SaveTexture(tex);
        }, "배경 이미지 선택", "image/*");
    }

    private void ShowNativeDialog()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

    currentActivity.Call("runOnUiThread", new AndroidJavaObject("java.lang.Runnable", new AndroidJavaRunnable(() =>
    {
        AndroidJavaObject alertDialogBuilder = new AndroidJavaObject("android.app.AlertDialog$Builder", currentActivity);

        alertDialogBuilder.Call<AndroidJavaObject>("setTitle", "권한 필요");
        alertDialogBuilder.Call<AndroidJavaObject>("setMessage", "배경화면을 변경하려면 저장소 접근 권한이 필요합니다. 설정 화면에서 권한을 허용해 주세요.");
        alertDialogBuilder.Call<AndroidJavaObject>("setCancelable", false);

        // '설정' 버튼 클릭 시
        alertDialogBuilder.Call<AndroidJavaObject>("setPositiveButton", "설정", new AndroidJavaObject("android.content.DialogInterface$OnClickListener", new AndroidDialogListener(() => {
            NativeGallery.OpenSettings();
        })));

        // '취소' 버튼 클릭 시
        alertDialogBuilder.Call<AndroidJavaObject>("setNegativeButton", "취소", new AndroidJavaObject("android.content.DialogInterface$OnClickListener", new AndroidDialogListener(() => {
            // 아무 작업도 하지 않음
        })));

        AndroidJavaObject dialog = alertDialogBuilder.Call<AndroidJavaObject>("create");
        dialog.Call("show");
    })));
#else
        Debug.Log("이 기능은 안드로이드 기기에서만 작동합니다. (설정창 이동 시뮬레이션)");
        NativeGallery.OpenSettings();
#endif
    }

    // 안드로이드 다이얼로그 클릭 이벤트를 받기 위한 헬퍼 클래스
    public class AndroidDialogListener : AndroidJavaProxy
    {
        private System.Action callback;
        public AndroidDialogListener(System.Action callback) : base("android.content.DialogInterface$OnClickListener")
        {
            this.callback = callback;
        }
        public void onClick(AndroidJavaObject dialog, int which)
        {
            callback?.Invoke();
        }
    }

    // 설정 화면으로 보내는 함수
    private void OpenSettingsAndShowMessage()
    {
        // 유저에게 왜 설정에 가야 하는지 알림을 띄우는 것이 좋습니다 (UI Tooltip 등 활용)
        // 여기서는 일단 바로 설정창을 여는 코드를 작성합니다.
#if UNITY_ANDROID && !UNITY_EDITOR
    NativeGallery.OpenSettings();
#endif
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

    public void returnBasicImg() {
        Sprite sprite = Sprite.Create(
                    basic,
                    new Rect(0, 0, basic.width, basic.height),
                    new Vector2(0.5f, 0.5f)
                    );

        targetImage.sprite = sprite;

        SaveTexture(basic);
    }

}
