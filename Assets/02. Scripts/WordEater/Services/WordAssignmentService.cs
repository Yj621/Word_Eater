using System.Linq;
using UnityEngine;
using WordEater.Core;
using WordEater.Data;

namespace WordEater.Services
{    
     /// <summary>
     /// WordBank(단어 풀)에서 현재 단계/맥락에 맞는 단어를 선택해 주는 서비스
     /// </summary>
    public class WordAssignmentService : MonoBehaviour
    {
        [SerializeField][Tooltip("단어 생성")] private WordBank wordBank;

        // TODO: 추후 GPT 연동 시 여기서 프롬프트 생성/후처리

        /// <summary>
        /// 단계 난이도에 맞는 초기 단어 선택
        /// </summary>
        public WordEntry PickInitialWord(GrowthStage stage)
        {
            // 간단한 규칙: Bit=쉬움, Byte=중간, Word=어려움 위주
            int targetDiff = stage == GrowthStage.Bit ? 0 : (stage == GrowthStage.Byte ? 1 : 2);
            var pool = wordBank.entries.Where(e => e.difficulty <= targetDiff + 1).ToList();
            if (pool.Count == 0) pool = wordBank.entries;
            return pool[Random.Range(0, pool.Count)];
        }

        /// <summary>
        /// 이전 단어와 topic/related 가 이어지는 후보를 우선 선택
        /// </summary>
        /// 
        public WordEntry PickNextLinkedWord(WordEntry prev, GrowthStage stage)
        {
            // "주사위→확률→통계학", "거울→반사→광학" 같은 계열을 연상
            // 동일 topic/related가 겹치는 항목 우선
            var pool = wordBank.entries
                .Where(e => e.word != prev.word &&
                            (e.topic == prev.topic || e.related.Intersect(prev.related).Any()))
                .ToList();

            if (pool.Count == 0) pool = wordBank.entries.Where(e => e.word != prev.word).ToList();
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
