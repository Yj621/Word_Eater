using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WordEater.Systems;

public class Item : MonoBehaviour
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
        // 부활권은 인벤토리에서 직접 사용할 수 없으므로 클릭 무시
        if (_myType == ItemType.ReviveTicket)
        {
            NoticeManager.Instance.ShowSticky("부활권은 사망 시\n사용 가능합니다.");
            GameManager.Instance.HidePanel_Item();
            return;
        }
        string itemName = ItemUtils.GetItemNameKR(_myType);

        GameManager.Instance.HidePanel_Item();

        // UIManager를 통해 확인 팝업 호출
        UIManager.Instance.ShowConfirmPopup(
            title: "아이템 사용",
            message: $"<color=orange>{itemName}</color>을(를) 사용하시겠습니까?",
            onYes: () =>
            {
                // 실제 아이템 기능 실행 (개수 차감 등)
                _controller.UseItem(_myType);

                // 상단 알림창 호출 (아이콘 전달)
                // - message: 표시할 텍스트
                // - duration: 2.0초 동안 표시
                // - onComplete: null (알림 끝난 뒤 할 일 없음)
                // - icon: 현재 슬롯의 iconImage.sprite 전달
                UIManager.Instance.ShowEmergencyAlarm(
                    "아이템 사용",
                    $"{itemName} 사용!",
                    2.0f,
                    null,
                    iconImage.sprite
                );

                Debug.Log($"아이템 사용 완료: {itemName}");
            },
            onNo: () =>
            {
                Debug.Log("아이템 사용 취소");
            },
            itemIcon: iconImage.sprite
        );
    }

}