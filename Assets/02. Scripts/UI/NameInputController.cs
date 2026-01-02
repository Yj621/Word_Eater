using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordEater.Core;

public class NameInputController : MonoBehaviour
{
    [Header("UI 컴포넌트 (기능)")]
    [SerializeField] private TMP_InputField nameInputField; // 실제 입력받는 곳 (투명하게 숨김 추천)
    [SerializeField] private Button submitButton;           // 제출 버튼

    [Header("UI 컴포넌트 (연출)")]
    [SerializeField] private TextMeshProUGUI displayOutput; // 유저 눈에 보이는 텍스트 (커서 포함)

    [Header("연출 설정")]
    [SerializeField] private string cursorChar = "|";       // 커서 모양
    [SerializeField] private float blinkInterval = 0.5f;    // 깜빡임 속도

    [Header("시스템 연결")]
    [SerializeField] private WordEater.Core.WordEater wordEater;
    [SerializeField] private InfoPanelController infoPanel;

    private Coroutine blinkCoroutine;
    private bool isCursorVisible = true;

    private void Start()
    {
        // 비트(Bit) 단계가 아니면 패널 끄기
        if (wordEater != null && wordEater.CurrentStage != GrowthStage.Bit)
        {
            gameObject.SetActive(false);
            return; // 이후 로직 실행 안 함
        }

        // 버튼 리스너 연결
        submitButton.onClick.AddListener(OnSubmitName);

        // 타이핑 감지 리스너 연결
        nameInputField.onValueChanged.AddListener(OnInputValueChanged);

        // 초기화 및 커서 깜빡임 시작
        nameInputField.text = ""; // 초기화
        UpdateDisplay("");        // 화면 초기화
        blinkCoroutine = StartCoroutine(CursorBlinkRoutine());
    }

    // ========================================================================
    // [연출 로직] 타이핑 효과 및 커서
    // ========================================================================

    // 인풋 필드 값이 바뀔 때마다 호출
    private void OnInputValueChanged(string str)
    {
        isCursorVisible = true; // 타이핑 중엔 커서 보이게

        // 깜빡임 주기 리셋 (반응성 향상)
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(CursorBlinkRoutine());

        UpdateDisplay(str);
    }

    // 화면 갱신
    private void UpdateDisplay(string text)
    {
        if (displayOutput != null)
        {
            displayOutput.text = isCursorVisible ? text + cursorChar : text;
        }
    }

    // 커서 깜빡임 코루틴
    IEnumerator CursorBlinkRoutine()
    {
        while (true)
        {
            isCursorVisible = true;
            UpdateDisplay(nameInputField.text);
            yield return new WaitForSeconds(blinkInterval);

            isCursorVisible = false;
            UpdateDisplay(nameInputField.text);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    // ========================================================================
    // [기능 로직] 이름 저장 및 패널 닫기
    // ========================================================================
    private void OnSubmitName()
    {
        string inputName = nameInputField.text;

        if (string.IsNullOrWhiteSpace(inputName))
        {
            Debug.LogWarning("이름을 입력해주세요.");
            return; // 빈 이름은 진행 안 함
        }

        if (FileManager.Instance != null)
        {
            // FileManager 내부의 SetPlayerName 함수가 
            // CurrentPlayerName 변수 업데이트 + JSON 저장을 모두 수행합니다.
            FileManager.Instance.SetPlayerName(inputName);
        }

        // 정보창 갱신 요청
        if (infoPanel != null)
        {
            infoPanel.UpdateInfoUI();
        }

        // 패널 닫기
        gameObject.SetActive(false);
    }
}