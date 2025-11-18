using UnityEngine;

/// <summary>
/// 모바일(안드로이드/IOS)에서 소프트 키보드가 올라올 때,
/// 화면 하단에 위치한 Input_Group(입력창 + 전송 버튼)을
/// 키보드 높이에 맞춰 위로 올려주는 스크립트
///
/// 목적:
/// - 키보드가 InputField를 가리는 현상을 방지
/// 
/// 원리:
/// 1) 네이티브 키보드 높이를 픽셀 단위로 가져온다
/// 2) Canvas Scaler의 scaleFactor로 나눠 'UI 좌표'로 변환
/// 3) 입력창의 anchoredPosition.y 를 그만큼 위로 올린다
/// 
/// 이 스크립트는 오직 target(Input_Group)만 움직이며,
/// 메시지 스크롤뷰 등 다른 UI는 그대로 둔다
/// (채팅 앱에서 하단 입력창만 올라가는 방식)
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

    // target의 원래 anchoredPosition 값 (키보드 닫으면 원래 위치로 복귀)
    private Vector2 originalAnchoredPos;

    // 이전 프레임 키보드 높이 (같은 값일 경우 연산 안 하려고 저장)
    private float currentKeyboardHeight;

    void Awake()
    {
        // 타겟이 비어 있으면 자기 자신 RectTransform 참조
        if (target == null)
            target = GetComponent<RectTransform>();

        // 가장 가까운 부모 Canvas 찾기
        rootCanvas = target.GetComponentInParent<Canvas>();

        // 초기 anchoredPosition 저장
        originalAnchoredPos = target.anchoredPosition;
    }

    void OnEnable()
    {
        // 씬 다시 열리거나 활성화될 때 위치 리셋
        if (target != null)
            target.anchoredPosition = originalAnchoredPos;

        currentKeyboardHeight = 0f;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        // 네이티브로 실시간 키보드 높이 가져오기 (스크린 px 단위)
        float keyboardHeight = GetNativeKeyboardHeight();

        // 키보드 높이가 변경되었을 때만 UI 갱신
        if (!Mathf.Approximately(currentKeyboardHeight, keyboardHeight))
        {
            currentKeyboardHeight = keyboardHeight;

            // Canvas Scaler 고려 → 스크린 픽셀 → UI 픽셀로 변환
            float uiKeyboardHeight = keyboardHeight / rootCanvas.scaleFactor;

            // multiplier & 추가 offset 적용
            float finalY = uiKeyboardHeight * heightMultiplier + extraOffset;

            // Input_Group을 위로 이동
            target.anchoredPosition =
                originalAnchoredPos + new Vector2(0f, finalY);
        }
#endif
    }

    /// <summary>
    /// 실제 모바일 환경에서 "진짜" 키보드 높이를 가져오는 함수
    /// 
    /// 에디터(PC)에서는 0만 반환
    /// 안드로이드에서는 TouchScreenKeyboard.area → 부족하면 네이티브 수동 계산
    /// iOS는 TouchScreenKeyboard.area가 정확하게 동작함
    /// 
    /// 반환값: 키보드 높이 (스크린 픽셀 기준)
    /// </summary>
    private float GetNativeKeyboardHeight()
    {
#if UNITY_EDITOR
        // 에디터에선 실제 키보드 없음
        return 0f;

#elif UNITY_ANDROID
        // 1차 시도: Unity 내장 TouchScreenKeyboard 값 사용
        if (TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height > 0)
            return TouchScreenKeyboard.area.height;

        // 2차 시도: 안드로이드 네이티브 코드로 높이 계산
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var rootView = currentActivity.Call<AndroidJavaObject>("getWindow")
                                          .Call<AndroidJavaObject>("getDecorView");
            var visibleRect = new AndroidJavaObject("android.graphics.Rect");

            // 현재 화면에서 키보드가 차지하지 않는 영역을 가져옴
            rootView.Call("getWindowVisibleDisplayFrame", visibleRect);

            int screenHeight = rootView.Call<int>("getHeight");      // 전체 화면 높이
            int visibleHeight = visibleRect.Call<int>("height");     // 키보드 제외 보이는 영역
            int keyboardHeight = screenHeight - visibleHeight;       // 차이 = 키보드 높이

            // 값이 너무 작으면 네비게이션 바로 오판할 수 있으니 제외
            if (keyboardHeight < screenHeight * 0.15f)
                return 0;

            return keyboardHeight;
        }

#elif UNITY_IOS
        // iOS에서는 TouchScreenKeyboard.area 값이 정확함
        if (TouchScreenKeyboard.visible)
            return TouchScreenKeyboard.area.height;
        return 0f;

#else
        // 그 외 플랫폼은 키보드가 없다고 간주
        return 0f;
#endif
    }
}
