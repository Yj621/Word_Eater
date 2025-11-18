using UnityEngine;

public class NativeKeyboardAvoider : MonoBehaviour
{
    [Header("전체 채팅 패널 (이 UI가 통째로 올라갑니다)")]
    [SerializeField] private RectTransform chatRoot;

    private Vector2 originalOffsetMin;
    private Canvas rootCanvas;
    private float currentKeyboardHeight = 0;

    void Start()
    {
        if (chatRoot == null)
            chatRoot = GetComponent<RectTransform>();

        rootCanvas = chatRoot.GetComponentInParent<Canvas>();

        //  키보드가 없을 때의 chatRoot 하단(offsetMin) 원본 값을 저장
        originalOffsetMin = chatRoot.offsetMin;
    }

    void Update()
    {
        // 안드로이드/iOS에서만 작동하도록 
#if UNITY_ANDROID || UNITY_IOS
        float keyboardHeight = GetNativeKeyboardHeight();

        // 키보드 높이가 변경되었는지 확인
        if (currentKeyboardHeight != keyboardHeight)
        {
            currentKeyboardHeight = keyboardHeight;

            // '스크린 픽셀' 높이를 'UI 픽셀' 높이로 변환
            // (Canvas Scaler의 scaleFactor로 나눔)
            float keyboardHeightInUIPixels = keyboardHeight / rootCanvas.scaleFactor;

            // chatRoot의 하단(offsetMin.y)을 변환된 키보드 높이만큼 밀어 올리기
            chatRoot.offsetMin = new Vector2(originalOffsetMin.x, originalOffsetMin.y + keyboardHeightInUIPixels);
        }
#endif
    }

    /// <summary>
    /// 네이티브 OS(안드로이드)로부터 실제 키보드 높이를 픽셀 단위로 가져옴
    /// </summary>
    /// <returns>키보드 높이 (픽셀)</returns>
    private float GetNativeKeyboardHeight()
    {
        // 에디터에서는 TouchScreenKeyboard.area를 쓰거나 0을 반환
#if UNITY_EDITOR
        // return TouchScreenKeyboard.area.height; // 에디터에서 테스트 시
        return 0; // 혹은 0
#elif UNITY_ANDROID
        // 안드로이드 네이티브 코드를 호출하여 키보드 높이를 가져옵니다.
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var rootView = currentActivity.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
            var visibleRect = new AndroidJavaObject("android.graphics.Rect");
            
            rootView.Call("getWindowVisibleDisplayFrame", visibleRect);
            
            // (전체 화면 높이 - 보이는 영역의 높이) = 키보드 높이
            int screenHeight = rootView.Call<int>("getHeight");
            int visibleHeight = visibleRect.Call<int>("height");
            int keyboardHeight = screenHeight - visibleHeight;

            // 간혹 네비게이션 바(소프트키) 등이 포함될 수 있으므로, 
            // 너무 작은 값은 키보드가 아니라고 판단합니다.
            if (keyboardHeight < screenHeight * 0.15f)
            {
                return 0; // 키보드가 올라오지 않음
            }
            
            return keyboardHeight;
        }
#elif UNITY_IOS
        // iOS에서는 TouchScreenKeyboard.area가 비교적 잘 작동합니다.
        return TouchScreenKeyboard.area.height;
#else
        // 기타 플랫폼
        return 0;
#endif
    }
}