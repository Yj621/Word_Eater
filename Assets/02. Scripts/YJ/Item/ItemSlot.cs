using System.Collections.Generic;
using UnityEngine;
using WordEater.Systems;

[System.Serializable]
public struct ItemInfo
{
    public ItemType type;
    public Sprite icon; // 인스펙터에서 아이콘 등록용
}

public class ItemSlot : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private Item slotPrefab; // 슬롯 프리팹
    [SerializeField] private Transform slotParent;  // 슬롯이 생성될 부모 (Grid/Horizontal Layout)
    [SerializeField] private ItemEffectController effectController; // 효과 발동기 연결
    [SerializeField] private BatterySystem batterySystem; // 배터리 시스템 참조 (배터리 리필 아이템 사용 시 필요)

    [Header("아이템 데이터 등록")]
    public List<ItemInfo> itemIcons; // 인스펙터에서 각 타입별 이미지 등록

    // 생성된 슬롯들을 관리하는 딕셔너리
    private Dictionary<ItemType, Item> _spawnedSlots = new Dictionary<ItemType, Item>();

    private void Start()
    {
        // 슬롯 미리 다 생성하기 (4종류)
        foreach (var info in itemIcons)
        {
            var newSlot = Instantiate(slotPrefab, slotParent);
            // 초기 개수 가져오기
            int count = ItemManager.Instance.GetCount(info.type);

            newSlot.Setup(info.type, info.icon, count, effectController, batterySystem);
            _spawnedSlots.Add(info.type, newSlot);
        }

        // 인벤토리 변경 이벤트 구독
        ItemManager.Instance.OnInventoryChanged += RefreshUI;
    }

    private void OnDestroy()
    {
        if (ItemManager.Instance != null)
            ItemManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    // 아이템을 쓰거나 얻었을 때 UI 갱신
    private void RefreshUI()
    {
        foreach (var kvp in _spawnedSlots)
        {
            ItemType type = kvp.Key;
            Item slot = kvp.Value;

            int count = ItemManager.Instance.GetCount(type);
            slot.UpdateCount(count);
        }
    }
}