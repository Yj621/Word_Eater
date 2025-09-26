using UnityEngine;
using WordEater.Data;
using WordEater.Services;
using WordEater.Systems;

namespace WordEater.Core
{
    /// <summary>
    /// 한 마리 워드 이터의 생애 주기를 관리하는 상태 머신
    /// </summary>
    public class WordEater : MonoBehaviour
    {
        [Header("할당")]
        [SerializeField] private GrowthConfig growthConfig;          // 단계 규칙 SO
        [SerializeField] private WordAssignmentService wordService;  // 단어 배정 
        [SerializeField] private BatterySystem battery;
        [SerializeField] private SubmitManager submitmanager;
        [SerializeField] private GameManager gamemanager;


        [SerializeField] private Sprite BitImg;
        [SerializeField] private Sprite ByteImg;
        [SerializeField] private Sprite WordImg;


        [Header("Runtime (read-only)")]
        [SerializeField] private GrowthStage stage = GrowthStage.Bit; // 현재 단계
        [SerializeField] private string currentAnswer;                // 현재 정답(프로토타입용 노출)

        private TurnController turn;   // 턴/오답 관리자
        private WordEntry currentEntry; // 현재 단어 데이터(주제/연관어 포함)



        private void Awake()
        {
            turn = new TurnController(growthConfig);
        }

        /// <summary>
        /// 단계 시작(턴/오답 초기화 + 단어 배정)
        /// </summary>
        public void BeginStage(GrowthStage s, bool initial = false)
        {
            turn.StartStage(s);


            //처음 (다시시작이나 게임 클리어 포함)
            if (initial)
            {
                //BIT상태로 변경
                stage = GrowthStage.Bit;
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = BitImg;
                }

                currentEntry = wordService.PickInitialWord(s);

                // 꿈에서 주는거 같은 애니메이션 추가
                submitmanager.OnRelevantButton();
            }
            else
            {
                currentEntry = wordService.PickNextLinkedWord(currentEntry, s);

                // 단계별 이미지 변경
                if (s == GrowthStage.Byte)
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = ByteImg;
                    }
                }
                else if (s == GrowthStage.Word)
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = WordImg;
                    }
                }
            }


            currentAnswer = currentEntry.word;
            GameEvents.OnNewWordAssigned?.Invoke(currentAnswer); // UI: "새 단어 등장" (정답 직접 노출 대신 디버그/프로토타입용)
        }


        /// <summary>
        /// 데이터 주입(정답/오답 판정, 다음 문제 배정, 진화 체크)
        /// </summary>
        public void DoFeedData(string userInput)
        {
            /*
            //배터리 먼저 확인
            if (!battery.TryConsume(ActionType.FeedData))
                return;
                */


            // 턴 소모(FeedData는 1턴)
            if (!turn.ConsumeTurn(ActionType.FeedData))
            {
                Die(); return;
            }

            // 정답 판정(v1 : 완전 일치, v2 : 오타/의미 유사도 확정 예정)
            bool ok = IsCorrect(userInput, currentAnswer);
            GameEvents.OnFeedResult?.Invoke(userInput, ok);

            if (ok)
            {
                currentEntry = wordService.PickNextLinkedWord(currentEntry, stage);
                currentAnswer = currentEntry.word;
                GameEvents.OnNewWordAssigned?.Invoke(currentAnswer);

                EvolveOrFinish();
            }
            else
            {
                // 오답 1회 등록, 한계 초과시 사망
                if (!turn.RegisterMistake()) { Die(); return; }
            }

            // 턴 바닥나면 사망
            if (turn.TurnsLeft < 0) { Die(); return; }
        }

        /// <summary>
        /// 미니게임/힌트(턴 1 소모)
        /// </summary>
        public void DoOptimizeAlgo() // 미니게임 자리(힌트/버프 지급)
        {
            if (!turn.ConsumeTurn(ActionType.OptimizeAlgo))
            {
                Die(); return;
            }
            // TODO: 힌트 토큰 +1, 상성 버프 스택 등
        }

        /// <summary>
        /// 노이즈 제거(턴 2 소모, 배율/버프 예정)
        /// </summary>
        public void DoCleanNoise() // 2턴 소모, 배율/보상 증가 버프
        {
            if (!turn.ConsumeTurn(ActionType.CleanNoise))
            {
                Die(); return;
            }
            // TODO: 배율 스택 += 1
        }

        // === 내부 로직 =======================================================

        private bool IsCorrect(string input, string answer)
        {
            // v1: 완전 일치. v2: 유사도(레벤슈타인/임베딩) 도입.
            return string.Equals(input.Trim(), answer.Trim(), System.StringComparison.Ordinal);
        }

        /// <summary>
        /// 단계 종료 처리(다음 단계 or 성체)
        /// </summary>
        private void EvolveOrFinish()
        {
            if (stage == GrowthStage.Word)
            {
                // 성체 달성 → 도감 등록·보상 지급(추후 연결)
                GameEvents.OnEvolved?.Invoke(stage);
                // 다음 개체 생성 루틴으로 넘어가거나 휴지통 UI 호출



                //게임 클리어. 게임메니저에서 함수 호출
                gamemanager.EndingController(2);

                return;
            }

            // 다음 단계로
            stage = (GrowthStage)((int)stage + 1);
            GameEvents.OnEvolved?.Invoke(stage);

            BeginStage(stage);
        }

        /// <summary>
        /// 사망 처리(휴지통/광고 보상 등 훅)
        /// </summary>
        private void Die()
        {
            GameEvents.OnDied?.Invoke();
            // TODO: 휴지통 연출·부활 아이템·광고보상 등 트리거

            enabled = false;

            // 게임오버. 게임메니저에서 함수 호출
            gamemanager.EndingController(1);
        }

        public WordEntry returnCurrentEnrty() {
            return currentEntry;
        }

        public string CurrentAnswer => currentAnswer;

        public GrowthStage ReturnStage() {
            return stage;
        }
    }
}
