using System;
using UnityEngine;

namespace WordEater.Systems
{
    public enum ItemType
    {
        BatteryRefill,  // 배터리 채우기
        HintChosung,    // 초성 힌트
        FillKeyCounts,  // 자음/모음 채우기
        ReviveTicket,    // 부활권
        JamoSelectionTicket // 자음/모음 선택권
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

        /// <summary>
        /// 아이템 획득
        /// </summary>
        public void AddItem(ItemType type, int amount = 1)
        {
            // FileManager를 통해 저장 및 데이터 갱신
            FileManager.Instance.UpdateItemCount(type, amount);

            // UI 갱신 알림
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// 아이템 사용 
        /// </summary>
        /// <returns>성공 시 true, 실패 시 false</returns>
        public bool TryUseItem(ItemType type)
        {
            // FileManager에서 현재 개수 조회
            int current = FileManager.Instance.GetItemCount(type);

            if (current > 0)
            {
                // FileManager를 통해 개수 차감 (-1)
                FileManager.Instance.UpdateItemCount(type, -1);

                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        public int GetCount(ItemType type)
        {
            // FileManager에서 조회
            if (FileManager.Instance == null) return 0;
            return FileManager.Instance.GetItemCount(type);
        }
    }
}