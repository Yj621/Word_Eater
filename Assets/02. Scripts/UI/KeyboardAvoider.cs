using UnityEngine;

public class KeyboardAvoider : MonoBehaviour
{
    private RectTransform _rect;
    private Vector2 _originalOffsetMin;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originalOffsetMin = _rect.offsetMin;
    }

    void Update()
    {
#if UNITY_EDITOR
        // 에디터에서는 키보드가 없으니 항상 원래 위치
        _rect.offsetMin = _originalOffsetMin;
        return;
#endif

        // 모바일에서만 동작
        if (TouchScreenKeyboard.visible)
        {
            // 키보드가 차지하는 영역 (스크린 픽셀 단위)
            Rect kbArea = TouchScreenKeyboard.area;

            // Screen Space - Overlay 캔버스라면 픽셀 == UI 단위라 그대로 사용 가능
            float keyboardHeight = kbArea.height;

            // 아래쪽 여백만 키보드 높이만큼 올려줌
            _rect.offsetMin = new Vector2(
                _originalOffsetMin.x,
                _originalOffsetMin.y + keyboardHeight
            );
        }
        else
        {
            // 키보드 내려가면 원래 위치로
            _rect.offsetMin = _originalOffsetMin;
        }
    }
}
