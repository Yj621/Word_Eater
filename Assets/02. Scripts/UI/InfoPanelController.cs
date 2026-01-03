using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용
using WordEater.Core; // WordEater 클래스 참조용
using WordEater.Systems; // GameManager 등 참조용

public class InfoPanelController : MonoBehaviour
{
    [Header("시스템 연결")]
    [SerializeField] private WordEater.Core.WordEater wordEater; // 상태(Stage) 확인용
    [SerializeField] private GameManager gameManager;   // 시도 횟수 확인용
    [SerializeField] private GameObject galleryPanel;   // 도감 패널 (도감 바로가기용)

    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI nameText;        // 워드이터 이름
    [SerializeField] private TextMeshProUGUI dateText;        // 오늘 날짜
    [SerializeField] private TextMeshProUGUI stateText;      // 현재 상태 (비트/바이트/워드)
    [SerializeField] private TextMeshProUGUI tryText;     // 시도 횟수

    [Header("버튼")]
    [SerializeField] private Button galleryShortcutBtn; // 도감 바로가기 버튼 (>)

    private void Awake()
    {

        if (galleryShortcutBtn != null)
            galleryShortcutBtn.onClick.AddListener(OnGoToGallery);
    }

    /// <summary>
    /// 패널이 활성화될 때마다 정보를 갱신
    /// </summary>
    private void OnEnable()
    {
        // 패널이 켜질 때 전체 UI 한번 갱신 (기존 로직)
        UpdateInfoUI();

        // 이름 변경 이벤트 구독 (켜져 있는 동안 이름 바뀌면 즉시 반영)
        if (FileManager.Instance != null)
        {
            FileManager.Instance.OnNameChanged += UpdateNameText;
        }
    }
    private void OnDisable()
    {
        // 패널 꺼질 때 구독 해제 (메모리 누수 방지)
        if (FileManager.Instance != null)
        {
            FileManager.Instance.OnNameChanged -= UpdateNameText;
        }
    }

    // 이벤트가 발생했을 때(이름이 바뀔 때) 실행되는 함수
    private void UpdateNameText(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
        }
    }

    /// <summary>
    /// UI 텍스트들을 현재 상태에 맞춰 갱신하는 함수
    /// </summary>
    public void UpdateInfoUI()
    {
        if (FileManager.Instance != null)
        {
            nameText.text = FileManager.Instance.CurrentPlayerName;
        }
        else
        {
            nameText.text = "로딩중...";
        }
        // 날짜 설정 (오늘 날짜)
        dateText.text = DateTime.Now.ToString("yyyy년 MM월 dd일");

        // 현재 상태 설정 (WordEater의 Stage 정보 활용)
        if (wordEater != null)
        {
            string statusString = "";
            switch (wordEater.CurrentStage)
            {
                case GrowthStage.Bit:
                    statusString = "비트 (Bit)";
                    break;
                case GrowthStage.Byte:
                    statusString = "바이트 (Byte)";
                    break;
                case GrowthStage.Word:
                    statusString = "워드 (Word)";
                    break;
            }
            stateText.text = $"{statusString}";
        }

        // 시도 횟수 설정 (History.cs 참고)
        // GameManager의 HistoryLine 문자열 개수를 세어 시도 횟수를 파악
        if (gameManager != null && !string.IsNullOrEmpty(gameManager.HistoryLIne))
        {
            // History.cs 로직을 참고하여 '|' 기준으로 나눔
            string[] attempts = gameManager.HistoryLIne.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            tryText.text = $"{attempts.Length}번";
        }
        else
        {
            tryText.text = "0번";
        }
    }

    /// <summary>
    /// 도감 바로가기 버튼 클릭 시
    /// </summary>
    private void OnGoToGallery()
    {
        // 정보창을 닫고 도감을 엽니다.
        gameObject.SetActive(false);

        if (galleryPanel != null)
        {
            galleryPanel.SetActive(true);

            galleryPanel.GetComponent<GalleryUIManager>()?.Refresh();
        }
        else
        {
            Debug.LogWarning("도감 패널이 연결되지 않았습니다.");
        }
    }
}