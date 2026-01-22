using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordEater.Core;
using WordEater.Systems;
using System.Collections;
using DG.Tweening;
using System.Globalization;

public class AlgorithmMessage : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private PythonConnectManager pythonConnectManager;
    [SerializeField] private GameManager gamemanager;
    [SerializeField] private WordEater.Core.WordEater wordEater;
    [SerializeField] private BatterySystem batterySystem;
    [SerializeField] private UILoadingText loading;

    [Header("상태 변수")]
    private string lastDateKey = "";

    [Header("횟수 표시")]
    [SerializeField] private TextMeshProUGUI countText;
    private int currentTryCount = 0;
    private const int MaxTryCount = 10;

    [Header("스크롤 뷰 관련")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform chatContent;

    [Header("프리팹")]
    [SerializeField] private GameObject dateLinePrefab;   // 날짜 구분선 프리팹 연결
    [SerializeField] private GameObject inputPanelPrefab; // 내 말풍선 프리팹 연결
    [SerializeField] private GameObject resultPanelPrefab;// 결과 말풍선 프리팹 연결

    [Header("애니메이션")]
    [SerializeField] private float duration = 0.2f;

void Awake()
{
    inputField.onSubmit.AddListener(_ => OnCheckSimilarity());
    inputField.onEndEdit.AddListener(_ => OnCheckSimilarity()); // 기기별 보험
}
    void Start()
    {
        CheckAndShowDate();
        UpdateCountText();

    }

    void Update()
    {

    }

    void OnEnable()
    {
        CheckAndShowDate();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            CheckAndShowDate();
        }
    }

    private void CheckAndShowDate()
    {
        string todayKey = System.DateTime.Now.ToString("yyyy-MM-dd");

        if (lastDateKey != todayKey)
        {
            AddDateLine();
            lastDateKey = todayKey;
        }
    }

    private void AddDateLine()
    {
        if (dateLinePrefab == null) return;

        GameObject dateObj = Instantiate(dateLinePrefab, chatContent);

        // 날짜 구분선에는 보통 시간은 안 적고 날짜만 적습니다. 취향껏 수정하세요.
        TextMeshProUGUI dateText = dateObj.GetComponentInChildren<TextMeshProUGUI>();
        if (dateText != null)
        {
            // 예: "2026년 1월 5일 월요일"
            dateText.text = System.DateTime.Now.ToString("yyyy년 M월 d일 dddd", new CultureInfo("ko-KR"));
        }

        StartCoroutine(CoScrollToBottom());
    }

    private void UpdateCountText()
    {
        if (countText != null)
        {
            countText.text = $"{currentTryCount}/{MaxTryCount}";
        }
    }

    // [중요] 스크롤을 확실하게 내리기 위한 수정
    private void ScrollToBottom()
    {
        // 레이아웃을 즉시 다시 계산 (가장 확실한 방법)
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
        scrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator CoScrollToBottom()
    {
        // UI 생성 후 1프레임 대기 후 스크롤
        yield return null;
        ScrollToBottom();
    }

    /// <summary>
    /// 채팅창 기록과 횟수를 모두 초기화하는 함수
    /// </summary>
    public void ClearAllMessages()
    {
        // 채팅창의 모든 자식 오브젝트(메세지들) 삭제
        if (chatContent != null)
        {
            foreach (Transform child in chatContent)
            {
                Destroy(child.gameObject);
            }
        }

        //횟수 초기화 (새로운 단계니까 0부터 다시 시작)
        currentTryCount = 0;
        UpdateCountText();

        // 날짜 구분선은 다시 띄워주기
        lastDateKey = ""; // 날짜 키를 초기화해서 다시 뜨게 하거나
        CheckAndShowDate(); // 바로 다시 생성
    }

    private void SpawnMessage(GameObject prefab, string message, bool animate)
    {
        GameObject newMsg = Instantiate(prefab, chatContent);

        // 1. 메세지 본문
        // (주의: 프리팹 구조가 복잡하면 이름으로 찾는게 안전합니다)
        TextMeshProUGUI msgText = newMsg.GetComponentInChildren<TextMeshProUGUI>();
        if (msgText != null) msgText.text = message;

        // 2. 시간 텍스트 ("Time_Text"라는 이름의 오브젝트를 찾음)
        // Transform.Find는 직계 자식만 찾으므로, 깊숙이 있다면 GetComponentsInChildren을 쓰는 기존 방식이 안전합니다.
        Transform timeObj = newMsg.transform.Find("Time_Text");

        // 못 찾았다면 전체 탐색 (안전장치)
        if (timeObj == null)
        {
            var texts = newMsg.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts)
            {
                if (t.gameObject.name == "Time_Text")
                { // 이름 확인 필수!
                    timeObj = t.transform;
                    break;
                }
            }
        }

        if (timeObj != null)
        {
            TextMeshProUGUI tText = timeObj.GetComponent<TextMeshProUGUI>();
            if (tText != null)
            {
                // "오늘 오전 09:30"
                tText.text = "오늘 " + System.DateTime.Now.ToString("tt hh:mm", new CultureInfo("ko-KR"));
            }
        }

        // 3. 애니메이션
        if (animate)
        {
            newMsg.transform.localScale = Vector3.zero;
            newMsg.transform.DOScale(1f, duration).SetEase(Ease.OutBack);
        }

        StartCoroutine(CoScrollToBottom());
    }

    public async void OnCheckSimilarity()
    {
        string userInput = inputField ? inputField.text : string.Empty;
        if (string.IsNullOrEmpty(userInput)) return;

        // 배터리 체크 (0회차일 때만)
        if (currentTryCount == 0)
        {
            if (!AlgoGuards.EnsureBattery(batterySystem, ActionType.OptimizeAlgoMessage, null))
            {
                // 이미 있는 알림 시스템 호출
                NoticeManager.Instance.ShowSticky("배터리가 부족합니다");
                return; // 함수 종료 (메시지 전송 안 함)
            }
        }

        currentTryCount++;
        UpdateCountText();

        SpawnMessage(inputPanelPrefab, userInput, false);
        inputField.text = "";
        loading?.StartAnim("유사도 계산 중");

        string answerWord = wordEater ? wordEater.Answer : string.Empty;

        float? similarity = await pythonConnectManager.SimilartyTwoWord(answerWord, userInput);

        loading?.StopAnim();

        string finalResultText = "";
        if (similarity.HasValue)
        {
            if (similarity.Value == 1)
            {
                finalResultText = "정답!";
                Handheld.Vibrate();
            }
            else
            {
                finalResultText = $"유사도 : {(similarity.Value * 100f).ToString("F0")}%";
            }

            // 파일에 저장 ( 히스토리에서 확인 가능)
            gamemanager.HistoryLIne += userInput + "," + (similarity.Value * 100f).ToString("F0") + "%" + "|";
            gamemanager.saveCountInmanager(1);
            gamemanager.UpdateHistoryLineInFile(gamemanager.HistoryLIne);

        }
        else
        {
            finalResultText = "오류 발생";
        }

        SpawnMessage(resultPanelPrefab, finalResultText, true);

        // 10회 다 썼으면 초기화
        if (currentTryCount >= MaxTryCount)
        {
            currentTryCount = 0;
            UpdateCountText();
        }
        
        SoundManager.Instance.SFXStart(SoundManager.SFXType.msgPopup);
    }
}