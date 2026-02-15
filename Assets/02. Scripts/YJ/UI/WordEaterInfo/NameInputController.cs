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
    [SerializeField] private TextMeshProUGUI ErrorText;

    [Header("연출 설정")]
    [SerializeField] private string cursorChar = "|";       // 커서 모양
    [SerializeField] private float blinkInterval = 0.5f;    // 깜빡임 속도

    [Header("시스템 연결")]
    [SerializeField] private WordEater.Core.WordEater wordEater;
    [SerializeField] private InfoPanelController infoPanel;

    [SerializeField] private SubmitManager submitManager;

    private Coroutine blinkCoroutine;
    private bool isCursorVisible = true;

    public PhoneSwiper phoneswiper;

private void Awake()
{
    // 리스너 초기화
    submitButton.onClick.RemoveAllListeners();
    submitButton.onClick.AddListener(OnSubmitName);
    
    nameInputField.onValueChanged.RemoveAllListeners();
    nameInputField.onValueChanged.AddListener(OnInputValueChanged);

    // [중요] 기존 onSubmit이나 onEndEdit이 자동으로 제출을 호출하지 않도록 비웁니다.
    nameInputField.onSubmit.RemoveAllListeners();
    nameInputField.onEndEdit.RemoveAllListeners();
}
    private void OnEnable()
    {
        ResetUI();
    }


private void Update()
{
    // 1. 안드로이드 뒤로가기 (키보드만 닫기)
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        if (nameInputField.isFocused)
        {
            // 포커스를 해제하여 키보드를 내리되, OnSubmitName은 절대 호출하지 않음
            nameInputField.DeactivateInputField();
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            return; // 뒤로가기 시에는 아래 엔터 체크를 하지 않음
        }
    }

    // 2. [수정] 키보드 엔터/패드 완료 버튼 감지 (진짜 제출 시에만)
    if (nameInputField.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
    {
        OnSubmitName();
    }
}
    private void ResetUI()
    {
        // 입력 필드 및 화면 텍스트 초기화
        if (nameInputField != null)
        {
            phoneswiper.isUsingTab = true;

            nameInputField.text = "";
            nameInputField.ActivateInputField();
            nameInputField.Select();
        }

        UpdateDisplay("");

        // 2. 커서 깜빡임 코루틴 재시작
        isCursorVisible = true;
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(CursorBlinkRoutine());
    }
    // 패널이 꺼질 때 안전하게 코루틴 정리
    private void OnDisable()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
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
    public void OnSubmitName()
    {
        string inputName = nameInputField.text;

        if (inputName.Length > 8)
        {
            //글자 수 제한
            ErrorText.gameObject.SetActive(true);
            return;
        }
        else
        {
            ErrorText.gameObject.SetActive(false);
            if (string.IsNullOrWhiteSpace(inputName))
            {
                // Debug.LogWarning("이름을 입력해주세요.");
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

            if (submitManager != null)
            {
                submitManager.OnRelevantButton();
            }
            else
            {
                // 혹시 인스펙터 연결을 깜빡했을 경우를 대비해 Find로 찾기 (안전장치)
                var sm = FindAnyObjectByType<SubmitManager>();
                if (sm != null) sm.OnRelevantButton();
            }

            // 패널 닫기
            phoneswiper.isUsingTab = false;
            gameObject.SetActive(false);
        }
    }
}