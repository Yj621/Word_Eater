using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordEater.Systems;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private Image iconImage;       // 아이콘 이미지
    [SerializeField] private Button btn;            // 클릭 버튼
    [SerializeField] private TextMeshProUGUI countText; // (선택) 개수 표시 텍스트

    private ItemType _myType;
    private ItemEffectController _controller;

    // 초기화 함수
    public void Setup(ItemType type, Sprite sprite, int count, ItemEffectController controller)
    {
        _myType = type;
        _controller = controller;

        // 이미지 설정
        if (iconImage != null) iconImage.sprite = sprite;

        // 버튼 클릭 이벤트 연결
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClickSlot);

        // 개수 업데이트
        UpdateCount(count);
    }

    // 개수가 0이면 숨기고, 1 이상이면 보이게 처리
    public void UpdateCount(int count)
    {
        // 아이템이 없으면 슬롯 자체를 꺼버림 (사진처럼 있는 것만 나오게)
        // 만약 회색으로 남기고 싶으면 gameObject.SetActive(true) 하고 버튼만 비활성화 하세요.
        gameObject.SetActive(count > 0);

        if (countText != null)
            countText.text = count > 1 ? count.ToString() : ""; // 1개일 땐 숫자 숨김 (취향껏)
    }
    /// <summary>
    /// 슬롯 클릭 시 실행되는 함수
    /// </summary>
    private void OnClickSlot()
    {
        // 1. 아이템 이름 한글로 변환 (팝업 표시용)
        string itemName = GetItemNameKR(_myType);

        GameManager.Instance.HidePanel_Item();

        // 2. UIManager를 통해 확인 팝업 호출
        UIManager.Instance.ShowConfirmPopup(
            title: "아이템 사용",
            message: $"<color=yellow>{itemName}</color>을(를) 사용하시겠습니까?",
            onYes: () =>
            {
                // 3. '네' 버튼을 눌렀을 때만 실제 아이템 사용 로직 실행
                _controller.UseItem(_myType);
                
                Debug.Log($"아이템 사용 완료: {itemName}");
            },
            onNo: () =>
            {
                // '아니오' 버튼 눌렀을 때 (아무것도 안 함 or 로그)
                Debug.Log("아이템 사용 취소");
            }
        );
    }

    // 아이템 타입에 따른 한글 이름 반환 헬퍼 함수
    private string GetItemNameKR(ItemType type)
    {
        return type switch
        {
            ItemType.BatteryRefill => "배터리 충전",
            ItemType.HintChosung => "초성 힌트",
            ItemType.FillKeyCounts => "자음/모음 채우기",
            ItemType.ReviveTicket => "부활권",
            _ => "알 수 없는 아이템"
        };
    }
}