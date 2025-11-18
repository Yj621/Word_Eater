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
        _rect.offsetMin = _originalOffsetMin;
        return;
#endif

        if (TouchScreenKeyboard.visible)
        {
            var kb = TouchScreenKeyboard.area;
            float h = kb.height;

            _rect.offsetMin = new Vector2(
                _originalOffsetMin.x,
                _originalOffsetMin.y + h
            );
        }
        else
        {
            _rect.offsetMin = _originalOffsetMin;
        }
    }
}
