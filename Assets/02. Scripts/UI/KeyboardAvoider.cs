using UnityEngine;

public class KeyboardAvoider : MonoBehaviour
{
    [Header("전체 채팅 패널 (이 UI가 통째로 올라갑니다)")]
    [SerializeField] private RectTransform chatRoot;

    private Vector2 originalOffsetMin;
    private Canvas rootCanvas;

    void Start()
    {
        if (chatRoot == null)
            chatRoot = GetComponent<RectTransform>();

        // 1. 캔버스의 scaleFactor를 가져오기 위해 Canvas 참조를 찾습니다.
        rootCanvas = chatRoot.GetComponentInParent<Canvas>();

        // 2. 키보드가 없을 때의 chatRoot 하단(offsetMin) 원본 값을 저장합니다.
        originalOffsetMin = chatRoot.offsetMin;

        // 에디터나 PC 환경에서는 이 스크립트가 필요 없으므로 비활성화합니다.
        if (Application.platform != RuntimePlatform.IPhonePlayer &&
            Application.platform != RuntimePlatform.Android)
        {
            enabled = false;
        }
    }

    void Update()
    {
        // 3. 모바일 키보드가 화면에 보이는지 확인합니다.
        if (TouchScreenKeyboard.visible)
        {
            // 4. 키보드의 실제 높이를 '스크린 픽셀' 단위로 가져옵니다.
            float keyboardHeight = TouchScreenKeyboard.area.height;

            // 5. '스크린 픽셀' 높이를 'UI 픽셀' 높이로 변환합니다.
            // (Canvas Scaler의 scaleFactor로 나눠줍니다)
            float keyboardHeightInUIPixels = keyboardHeight / rootCanvas.scaleFactor;

            // 6. chatRoot의 하단(offsetMin.y)을 변환된 키보드 높이(UI 픽셀)만큼 밀어 올립니다.
            if (chatRoot.offsetMin.y != keyboardHeightInUIPixels)
            {
                chatRoot.offsetMin = new Vector2(originalOffsetMin.x, keyboardHeightInUIPixels);
            }
        }
        else
        {
            // 7. 키보드가 보이지 않으면 chatRoot를 원래 위치로 복원합니다.
            if (chatRoot.offsetMin.y != originalOffsetMin.y)
            {
                chatRoot.offsetMin = originalOffsetMin;
            }
        }
    }

    // InputField의 OnSelect/OnDeselect 이벤트는 더 이상 필요 없습니다.
    // Update에서 실시간으로 감지하는 것이 훨씬 안정적입니다.
}