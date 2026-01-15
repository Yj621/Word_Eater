using UnityEngine;
using WordEater.Core;

namespace WordEater.Systems
{
    public class ItemEffectController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private WordEater.Core.WordEater wordEater;
        [SerializeField] private GameManager gameManager;

        /// <summary>
        /// UI 버튼 등에서 호출: 아이템 사용 시도
        /// </summary>
        public void UseItem(ItemType type)
        {
            Debug.Log("아이템 사용!");
            // 아이템 보유 체크 및 소모
            if (!ItemManager.Instance.TryUseItem(type))
            {
                Debug.Log("아이템이 부족합니다!");
                // UI 알림 띄우기 (UIManager.Instance.ShowToast("아이템이 없습니다.");)
                return;
            }

            // 효과 적용
            ApplyEffect(type);
        }

        private void ApplyEffect(ItemType type)
        {
            switch (type)
            {
                case ItemType.BatteryRefill:
                    // 배터리 채우기
                    batterySystem.RefillToMax();
                    Debug.Log("배터리 완충 완료!");
                    break;

                case ItemType.HintChosung:
                    string answer = wordEater.Answer;
                    string chosung = KoreanUtils.GetChosungString(answer);
                    UIManager.Instance.Show($"정답의 초성은 [{chosung}] 입니다!");
                    break;

                case ItemType.ReviveTicket:
                    if (wordEater.isDead)
                    {
                        wordEater.RevivePlayer();
                        batterySystem.RefillToMax();
                    }
                    break;

                case ItemType.FillKeyCounts:
                    // 자음/모음 1개씩 채우기
                    // KeyCount는 static 클래스이므로 바로 접근 가능
                    // Length만큼 돌면서 1개씩 추가
                    for (int i = 0; i < KeyCount.Length; i++)
                    {
                        KeyCount.AddAt(i, 1);
                    }
                    Debug.Log("모든 자판 카운트 +1 완료");
                    break;
            }
        }
    }
}