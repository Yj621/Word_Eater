using System;
using UnityEngine;

namespace WordEater.Systems
{
    public enum ItemType
    {
        BatteryRefill,  // 배터리 채우기
        HintChosung,    // 초성 힌트
        FillKeyCounts,  // 자음/모음 채우기
        ReviveTicket    // 부활권
    }

    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance;
        public event Action OnInventoryChanged;
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // 아이템 획득 (테스트나 상점 구매 시 호출)
        public void AddItem(ItemType type, int amount = 1)
        {
            int current = GetCount(type);
            PlayerPrefs.SetInt($"Item_{type}", current + amount);
            PlayerPrefs.Save(); 
            OnInventoryChanged?.Invoke();
        }

        // 아이템 사용 (성공 시 true, 실패 시 false)
        public bool TryUseItem(ItemType type)
        {
            int current = GetCount(type);
            if (current > 0)
            {
                PlayerPrefs.SetInt($"Item_{type}", current - 1);
                PlayerPrefs.Save(); 
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        public int GetCount(ItemType type)
        {
            return PlayerPrefs.GetInt($"Item_{type}", 0); // 기본값 0개
        }
    }
}