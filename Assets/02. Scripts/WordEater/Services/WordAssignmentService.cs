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
        public WordEntry PickInitialWord()
        {
            // stage 0(Bit) 단어 중에서 랜덤 선택
            var pool = wordBank.entries
                .Where(e => e.stage == 0)
                .ToList();

            return pool[Random.Range(0, pool.Count)];
        }

        /// <summary>
        /// 이전 단어와 topic/related 가 이어지는 후보를 우선 선택
        /// </summary>
        /// 
        public WordEntry PickNextLinkedWord(WordEntry prev, GrowthStage stage)
        {
            int nextStage = (int)stage+1;

            // 현재 단어의 related에 있는 단어 중, 다음 단계에 속하는 단어만 선택
            var pool = wordBank.entries
                    .Where(e => (int)e.stage == nextStage && prev.related.Contains(e.word))
                    .ToList();


            /*
            // 만약 related가 없다면(아마 오타) 다음 단계 전체에서 랜덤 선택
            if (pool.Count == 0)
            {
                pool = wordBank.entries
                    .Where(e => (int)e.stage == nextStage)
                    .ToList();
            }
            */

            return pool[Random.Range(0, pool.Count)];
        }
    }
}
