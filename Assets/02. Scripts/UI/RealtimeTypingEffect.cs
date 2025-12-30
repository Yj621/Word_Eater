using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RealtimeTypingEffect : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputSource;      // 유저가 입력하는 인풋 필드
    public TextMeshProUGUI displayOutput;   // 결과가 출력될 텍스트 (커서 효과 포함)

    [Header("Settings")]
    public string cursorChar = "|";         // 커서 모양
    public float blinkInterval = 0.5f;      // 깜빡임 속도

    private Coroutine blinkCoroutine;
    private bool isCursorVisible = true;

    private void Start()
    {
        // 1. 초기 텍스트 설정
        UpdateDisplay(inputSource.text);

        // 2. 인풋 필드에 내용이 바뀔 때마다 실행될 함수 연결
        inputSource.onValueChanged.AddListener(OnInputValueChanged);

        // 3. 커서 깜빡임 코루틴 시작
        blinkCoroutine = StartCoroutine(CursorBlinkRoutine());
    }

    // 인풋 필드 값이 변경될 때 호출됨
    public void OnInputValueChanged(string str)
    {
        // 타이핑 중에는 커서가 계속 보이게 하여 반응성을 높임 (선택 사항)
        isCursorVisible = true;

        // 깜빡임 타이머를 리셋하고 싶다면 코루틴을 재시작하면 됩니다 (여기서는 생략하고 즉시 갱신만 처리)
        StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(CursorBlinkRoutine());

        UpdateDisplay(str);
    }

    // 화면에 텍스트와 커서를 조합해서 출력
    private void UpdateDisplay(string text)
    {
        if (isCursorVisible)
        {
            displayOutput.text = text + cursorChar;
        }
        else
        {
            displayOutput.text = text;
        }
    }

    // 무한 반복하며 커서 상태를 토글함
    IEnumerator CursorBlinkRoutine()
    {
        while (true)
        {
            // 커서 보임
            isCursorVisible = true;
            UpdateDisplay(inputSource.text);
            yield return new WaitForSeconds(blinkInterval);

            // 커서 숨김
            isCursorVisible = false;
            UpdateDisplay(inputSource.text);
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}