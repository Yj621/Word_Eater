using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordEater.Systems;

[Serializable]
public struct ItemSpriteMapping
{
    public ItemType itemType;
    public Sprite iconSprite;
}

public class CalendarRewardUI : MonoBehaviour
{
    [Header("UI 대상 패널/탭")]
    public GameObject rewardPanel;     // 캘린더 아이콘 누르면 뜰 창 전체 (배경 포함)

    [Header("달력 메인 아이콘 연동")]
    public Image calendarIconImage;    // 메인화면의 달력 아이콘 이미지 컴포넌트
    public Sprite iconAvailable;       // 받을 수 있을 때의 달력 이미지
    public Sprite iconClaimed;         // 이미 받았을 때의 달력 이미지
    public Button calendarButton;      // 달력 열기 버튼 (Optional, Inspector에서 OnClick 연결 가능)

    [Header("보상창 내용 연동")]
    public Image itemImageDisplay;     // 보상창 내 아이템 이미지
    public TextMeshProUGUI itemNameText; // 보상창 내 아이템 이름
    public Button getButton;           // 획득 버튼
    public Button cancelButton;        // 취소(닫기) 버튼

    [Header("아이템 이미지 매핑")]
    public ItemSpriteMapping[] itemSprites; 

    // 내부 관리
    private bool isClaimedToday = false;
    private ItemType currentRolledItem;
    private bool hasRolledForToday = false; // 창을 여러번 열고 닫아도 같은 아이템이 나오도록 보장

    private const string PREF_KEY_DATE = "DailyReward_LastClaimDate";

    void Start()
    {
        // 취소/획득 버튼 리스너 등록
        if (getButton != null) getButton.onClick.AddListener(OnClickGetReward);
        if (cancelButton != null) cancelButton.onClick.AddListener(CloseRewardPanel);
        
        // 달력 버튼 자체에 리스너가 있다면 등록
        if (calendarButton != null) calendarButton.onClick.AddListener(OpenRewardPanel);

        // 창은 처음에 닫아둠
        if (rewardPanel != null) rewardPanel.SetActive(false);

        CheckDailyStatus();
    }

    /// <summary>
    /// 오늘 이미 보상을 받았는지 체크하고 아이콘을 갱신합니다.
    /// </summary>
    private void CheckDailyStatus()
    {
        string lastDate = PlayerPrefs.GetString(PREF_KEY_DATE, "");
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");

        isClaimedToday = (lastDate == todayDate);

        // 아이콘 갱신
        if (calendarIconImage != null)
        {
            calendarIconImage.sprite = isClaimedToday ? iconClaimed : iconAvailable;
        }

        // 이미 받았다면 오늘 뽑은 기록을 초기화 (안 해도 되지만 확실히 하기 위해)
        if (isClaimedToday)
        {
            hasRolledForToday = false;
        }
    }

    /// <summary>
    /// 캘린더 아이콘 클릭 시 보상창 열기
    /// </summary>
    public void OpenRewardPanel()
    {
        // 열기 전에 다시 한 번 날짜 체크 (자정 넘어갔을 수 있음)
        CheckDailyStatus();

        if (rewardPanel != null) rewardPanel.SetActive(true);

        if (isClaimedToday)
        {
            // 이미 획득한 상태
            itemNameText.text = "오늘은 이미 보상을\n받았습니다!";
            itemImageDisplay.sprite = iconClaimed; // 또는 투명 처리 등
            
            getButton.interactable = false; // 버튼 잠금
        }
        else
        {
            // 보상을 받을 수 있는 상태
            getButton.interactable = true;

            // 아직 오늘 분량의 아이템을 굴리지 않았다면 랜덤 뽑기 진행
            if (!hasRolledForToday)
            {
                RollDailyItem();
                hasRolledForToday = true;
            }

            // UI 갱신 (이미지, 텍스트)
            itemNameText.text = ItemUtils.GetItemNameKR(currentRolledItem);
            itemImageDisplay.sprite = GetSpriteForType(currentRolledItem);
        }
    }

    /// <summary>
    /// 보상창 닫기 (취소 버튼)
    /// </summary>
    public void CloseRewardPanel()
    {
        if (rewardPanel != null) rewardPanel.SetActive(false);
    }

    /// <summary>
    /// 획득 버튼 클릭
    /// </summary>
    private void OnClickGetReward()
    {
        if (isClaimedToday) return; // 방어 코드

        // 아이템 지급 (ItemManager 활용)
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.AddItem(currentRolledItem, 1);
            
            // 중앙 알림 팝업 (원한다면 유지, 아니면 이 스크립트에서만 처리)
            if (UIManager.Instance != null)
            {
                string krName = ItemUtils.GetItemNameKR(currentRolledItem);
                UIManager.Instance.Show($"일일 출석 보상!\n<color=yellow>[{krName}]</color> 획득!");
            }
        }
        else
        {
            Debug.LogWarning("ItemManager.Instance 가 없습니다. 아이템이 지급되지 않았습니다.");
        }

        // 획득 날짜 저장
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(PREF_KEY_DATE, todayDate);
        PlayerPrefs.Save();

        // 상태 갱신 및 창 닫기
        CheckDailyStatus();
        CloseRewardPanel();
    }

    /// <summary>
    /// ItemDropManager의 확률표를 빌려오되, 지급(AddItem)은 하지 않고 타입만 반환받는 유사 로직 구현.
    /// (ObtainRandomItem이 직접 지급까지 해버리므로, 그걸 피하기 위해 별도 굴림)
    /// 만약 ItemDropManager에 '타입만 반환'하는 함수가 있다면 그걸 쓰는 게 좋습니다.
    /// 여기서는 꼼수로 얻어서, 현재 굴려진 아이템이 뭔지만 기억합니다.
    /// </summary>
    private void RollDailyItem()
    {
        // 1. 편의상 ItemDropManager.ObtainRandomItem을 쓰고, 방금 들어간 1개를 다시 FileManager에서 강제로 빼는 것도 가능하나 위험.
        // 2. 가장 안전한 방법은 일단 모든 아이템 (enum) 중 단순 랜덤 1개 적용 (기본 확률)
        // 여기서는 단순 균등 랜덤을 적용합니다. (만약 가중치가 꼭 필요하면 ItemDropManager에 GetRandomType() 메서드를 추가해야 합니다)
        
        Array values = Enum.GetValues(typeof(ItemType));
        int randomIndex = UnityEngine.Random.Range(0, values.Length);
        currentRolledItem = (ItemType)values.GetValue(randomIndex);
    }

    private Sprite GetSpriteForType(ItemType type)
    {
        foreach (var mapping in itemSprites)
        {
            if (mapping.itemType == type) return mapping.iconSprite;
        }
        return null; // 매핑된 이미지가 없을 시
    }
}
