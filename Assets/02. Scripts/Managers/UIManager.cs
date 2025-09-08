using DG.Tweening;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Keyborad UI")]
    [SerializeField] private RectTransform _keyboardRect; // 키보드 UI의 RectTransform
    [SerializeField] private Vector2 _showPosition; // 키보드가 펴졌을 때의 목표 위치 (앵커 기준)
    [SerializeField] private Vector2 _hidePosition; // 키보드가 접혔을 때의 목표 위치 (앵커 기준)
    public float animationDuration = 0.5f; // 애니메이션 지속 시간

    private bool isKeyboardOpen = false; // 키보드 상태 추적 변수

    void Start()
    {
    }

    /// <summary>
    /// 키보드 상태를 토글하는 함수
    /// </summary>
    public void ToggleKeyboard()
    {
        if (isKeyboardOpen)
        {
            CloseKeyboard();
        }
        else
        {
            OpenKeyboard();
        }
    }

    /// <summary>
    /// 키보드를 펴는 함수
    /// </summary>
    public void OpenKeyboard()
    {
        if (isKeyboardOpen) return; // 이미 열려있으면 실행 안 함
        isKeyboardOpen = true;

        // DOTween 시퀀스를 사용하여 위치와 크기 애니메이션을 동시에 실행
        Sequence sequence = DOTween.Sequence();

        // anchoredPosition을 showPosition으로 이동
        sequence.Append(_keyboardRect.DOAnchorPos(_showPosition, animationDuration).SetEase(Ease.OutCirc));

        // scale을 (1, 1, 1)로 변경 (동시에 실행)
        sequence.Join(_keyboardRect.DOScale(1f, animationDuration).SetEase(Ease.OutCirc));

        // 시퀀스 실행
        sequence.Play();
    }

    /// <summary>
    /// 키보드를 접는 함수
    /// </summary>
    public void CloseKeyboard()
    {
        if (!isKeyboardOpen) return; // 이미 닫혀있으면 실행 안 함
        isKeyboardOpen = false;

        Sequence sequence = DOTween.Sequence();

        // anchoredPosition을 hidePosition으로 이동
        sequence.Append(_keyboardRect.DOAnchorPos(_hidePosition, animationDuration).SetEase(Ease.InCirc));


        sequence.Play();
    }
}
