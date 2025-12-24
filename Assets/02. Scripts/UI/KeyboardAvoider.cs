using UnityEngine;

/// <summary>
/// 모바일 키보드가 올라올 때 UI가 가려지지 않도록 위치를 조정하는 클래스임
/// 네이티브 키보드 높이를 계산하여 타겟 UI를 위로 밀어올림
/// </summary>
public class KeyboardAvoider : MonoBehaviour
{
    [Header("키보드에 맞춰 올릴 RectTransform (Input_Group)")]
    [SerializeField] private RectTransform target;

    [Header("키보드 높이에 곱해줄 값 (1.0 = 그대로, 0.9 = 살짝 덜 올리기)")]
    [SerializeField] private float heightMultiplier = 1.05f;

    [Header("추가로 미세하게 조절할 오프셋 (UI px 단위, 음수면 아래로)")]
    [SerializeField] private float extraOffset = -50f;

    // 현재 캔버스 (CanvasScaler의 scaleFactor 읽기 위함)
    private Canvas rootCanvas;

    // target의 원래 위치 저장용 (키보드 닫히면 복귀함)
    private Vector2 originalAnchoredPos;

    // 이전 프레임 키보드 높이 저장용 (중복 연산 방지)
    private float currentKeyboardHeight;

    void Awake()
    {
        // 타겟 없으면 자기 자신 할당함
        if (target == null)
            target = GetComponent<RectTransform>();

        rootCanvas = target.GetComponentInParent<Canvas>();
        originalAnchoredPos = target.anchoredPosition;
    }

    void OnEnable()
    {
        // 활성화될 때 위치 리셋함
        if (target != null)
            target.anchoredPosition = originalAnchoredPos;

        currentKeyboardHeight = 0f;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        float keyboardHeight = GetNativeKeyboardHeight();

        // 키보드가 안 떠 있으면 원위치로 복귀함
        if (keyboardHeight <= 0f)
        {
            if (!Mathf.Approximately(currentKeyboardHeight, 0f))
            {
                currentKeyboardHeight = 0f;
                target.anchoredPosition = originalAnchoredPos;
            }
            return;
        }

        // 키보드가 떠 있으면 높이에 맞춰 위치 조정함
        if (!Mathf.Approximately(currentKeyboardHeight, keyboardHeight))
        {
            currentKeyboardHeight = keyboardHeight;

            // 캔버스 스케일에 맞춰 높이 변환함
            float uiKeyboardHeight = keyboardHeight / rootCanvas.scaleFactor;
            float finalY = uiKeyboardHeight * heightMultiplier + extraOffset;

            target.anchoredPosition = originalAnchoredPos + new Vector2(0f, finalY);
        }
#endif
    }

    /// <summary>
    /// 플랫폼별 네이티브 키보드 높이를 픽셀 단위로 반환함
    /// 안드로이드는 계산 로직이 복잡하여 네이티브 호출을 사용함
    /// </summary>
    private float GetNativeKeyboardHeight()
    {
#if UNITY_EDITOR
        return 0f; // 에디터는 0 반환함

#elif UNITY_ANDROID
        // 1차 시도: Unity 내장 값 확인함
        if (TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height > 0)
            return TouchScreenKeyboard.area.height;

        // 2차 시도: 안드로이드 네이티브 코드로 높이 계산함
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var rootView = currentActivity.Call<AndroidJavaObject>("getWindow")
                                          .Call<AndroidJavaObject>("getDecorView");
            var visibleRect = new AndroidJavaObject("android.graphics.Rect");

            // 전체 화면에서 키보드 제외 영역 계산함
            rootView.Call("getWindowVisibleDisplayFrame", visibleRect);

            int screenHeight = rootView.Call<int>("getHeight");      
            int visibleHeight = visibleRect.Call<int>("height");     
            int keyboardHeight = screenHeight - visibleHeight;       

            // 오차 범위 필터링함 (네비게이션 바 등)
            if (keyboardHeight < screenHeight * 0.15f)
                return 0;

            return keyboardHeight;
        }

#elif UNITY_IOS
        // iOS는 내장 값이 정확함
        if (TouchScreenKeyboard.visible)
            return TouchScreenKeyboard.area.height;
        return 0f;

#else
        return 0f;
#endif
    }
}