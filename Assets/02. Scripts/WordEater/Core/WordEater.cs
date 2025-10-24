using System;
using System.IO;
using UnityEngine;
using WordEater.Data;
using WordEater.Services;
using WordEater.Systems;
using static UnityEngine.EventSystems.EventTrigger;

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
        [SerializeField] private GalleryUIManager galleryUIManager;


        [SerializeField] private Sprite BitImg;
        [SerializeField] private Sprite ByteImg;
        [SerializeField] private Sprite WordImg;
        [SerializeField] private bool isDead = false;

        [Header("Runtime (read-only)")]
        [SerializeField] private GrowthStage stage = GrowthStage.Bit; // 현재 단계
        [SerializeField] private string currentAnswer;                // 현재 정답(프로토타입용 노출)

        private TurnController turn;   // 턴/오답 관리자
        private WordEntry currentEntry; // 현재 단어 데이터(주제/연관어 포함)

        private string pendingEvoId; // Bit/Byte 동안 쓸 임시 키=


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

                //실패 최대 횟수 2로 변경
                turn.SetMistake(2);
                // 죽은 상태 해제
                isDead = false;

                // 현재 단어 선택
                currentEntry = wordService.PickInitialWord();

                // ✅ 임시 키 생성 (한 생애 내내 고정)
                pendingEvoId = $"evo_{System.DateTime.UtcNow.Ticks}";

                // ✅ Bit 썸네일을 임시 키로 저장
                if (sr != null)
                    GalleryCapture.SaveSpriteThumb(sr, $"thumb_{pendingEvoId}_s0", 256);

                submitmanager.OnRelevantButton();
            }
            else
            {
                var sr = GetComponent<SpriteRenderer>();
                if (s == GrowthStage.Byte)
                {
                    if (sr != null) sr.sprite = ByteImg;
                    // ✅ Byte 썸네일을 임시 키로 저장
                    if (sr != null)
                        GalleryCapture.SaveSpriteThumb(sr, $"thumb_{pendingEvoId}_s1", 256);
                }
                else if (s == GrowthStage.Word)
                {
                    if (sr != null) sr.sprite = WordImg;
                }
            }

            // BeginStage 끝부분(초기 진입 포함)
            if (GameReviveSystem.I != null && battery != null)
            {
                GameReviveSystem.I.SaveCheckpoint(this, battery.CurrentPercent);
            }

            // EvolveOrFinish에서 다음 단계 단어 배정 직후
            if (GameReviveSystem.I != null && battery != null)
            {
                GameReviveSystem.I.SaveCheckpoint(this, battery.CurrentPercent);
            }

            currentAnswer = currentEntry.word;
            GameEvents.OnNewWordAssigned?.Invoke(currentAnswer); // UI: "새 단어 등장" (정답 직접 노출 대신 디버그/프로토타입용)
        }


        /// <summary>
        /// 데이터 주입(정답/오답 판정, 다음 문제 배정, 진화 체크)
        /// </summary>
        public void DoFeedData(string userInput)
        {

            if (!battery.TryConsume(ActionType.FeedData))
                return; // OnActionBlockedLowBattery 이벤트로 HUD/토스트 띄우기


            // 턴 소모(FeedData는 1턴)
            if (!turn.ConsumeTurn(ActionType.FeedData))
            {
                WordEaterDie();
                return;
            }

            if (turn.TurnsLeft <= 0) { WordEaterDie(); return; }
            // 정답 판정(v1 : 완전 일치, v2 : 오타/의미 유사도 확정 예정)
            bool ok = IsCorrect(userInput, currentAnswer);
            GameEvents.OnFeedResult?.Invoke(userInput, ok);

            if (ok)
            {
                EvolveOrFinish();
            }
            else
            {
                if (!turn.RegisterMistake()) {WordEaterDie(); return; }
            }


            // 턴 바닥나면 사망
            if (turn.TurnsLeft <= 0) { WordEaterDie(); return; }
        }

        /// <summary>
        /// 미니게임/힌트(턴 1 소모)
        /// </summary>
        public void DoOptimizeAlgo() // 미니게임 자리(힌트/버프 지급)
        {
            if (isDead) return;
            if (!turn.ConsumeTurn(ActionType.OptimizeAlgo))
            {
                WordEaterDie(); return;
            }
            // TODO: 힌트 토큰 +1, 상성 버프 스택 등
        }

        /// <summary>
        /// 노이즈 제거(턴 2 소모, 배율/버프 예정)
        /// </summary>
        public void DoCleanNoise() // 2턴 소모, 배율/보상 증가 버프
        {
            if (isDead) return;
            if (!turn.ConsumeTurn(ActionType.CleanNoise))
            {
                WordEaterDie(); return;
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
        /// - 현재 단계가 최종 단계(Word)이면: 도감에 등록하고 엔딩 처리
        /// - 아니면: 다음 단계로 진화
        /// </summary>
        private void EvolveOrFinish()
        {
            if (stage == GrowthStage.Word)
            {
                // 성체 달성 → UI/연출 등 외부 구독자에게 알림
                GameEvents.OnEvolved?.Invoke(stage);

                // 성체가 되었으므로 "도감"에 현재 개체를 등록
                RegisterToGallery();

                // 게임 클리어 처리(엔딩 등)
                gamemanager.EndingController(2);
                galleryUIManager.Refresh();
                return;
            }

            // (성체가 아니라면) 다음 단계 단어 배정 및 진화
            currentEntry = wordService.PickNextLinkedWord(currentEntry, stage);
            currentAnswer = currentEntry.word;
            GameEvents.OnNewWordAssigned?.Invoke(currentAnswer);


            stage = (GrowthStage)((int)stage + 1);
            GameEvents.OnEvolved?.Invoke(stage);

            BeginStage(stage);
        }

        /// <summary>
        /// 사망 처리(휴지통/광고 보상 등 훅)
        /// </summary>
        private void WordEaterDie()
        {
            if (isDead) return; 
            isDead = true;
            GameEvents.OnDied?.Invoke();

            enabled = false;

            // 광고 팝업 띄우고, 거절하면 그때 엔딩
            if (GameReviveSystem.I != null)
            {
                Debug.Log("광고 팝업 띄울겨");
                GameReviveSystem.I.OnPlayerDied(onGiveUp: () =>
                {
                    // 정말 포기한 경우에만 게임오버 연출로 이동
                    gamemanager.EndingController(1);
                });
            }
            else
            {
                // 시스템이 없으면 안전하게 기존 흐름 유지
                gamemanager.EndingController(1);
            }

        }

        // 부활시
        public void Reactivate()
        {
            isDead = false;       // 죽은 상태 해제
            enabled = true;       // 다시 동작
                                  // 필요하면 무적 타이머/상태 초기화/입력언락 등을 여기서 처리
        }

        public int GetTurnsLeft() => turn.TurnsLeft;
        public int GetMistakesLeft() => turn.MistakesLeft; // TurnController에 프로퍼티 노출 필요

        public void RestoreTurns(int turnsLeft, int mistakesLeft)
        {
            turn.ForceRestore(turnsLeft, mistakesLeft); // TurnController에 강제 복원 API 추가
        }

        public void RestoreAnswer(string answer, GrowthStage s)
        {
            stage = s;
            currentAnswer = answer;
            GameEvents.OnNewWordAssigned?.Invoke(currentAnswer);

            // 스프라이트 동기화
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (stage == GrowthStage.Bit) sr.sprite = BitImg;
                if (stage == GrowthStage.Byte) sr.sprite = ByteImg;
                if (stage == GrowthStage.Word) sr.sprite = WordImg;
            }
        }


        public WordEntry returnCurrentEnrty()
        {
            return currentEntry;
        }

        public string CurrentAnswer => currentAnswer;

        public GrowthStage ReturnStage()
        {
            return stage;
        }

        /// <summary>
        /// [도감 등록] 성체 달성 시 현재 워드이터를 도감 JSON에 등록한다.
        /// 동작 순서:
        /// 1) 현재 단어/단계를 이용해 "고유 ID" 생성 (충돌 방지용)
        /// 2) UI에 표시할 제목/카테고리(설명) 텍스트 구성
        /// 3) 현재 SpriteRenderer의 스프라이트를 썸네일 PNG로 저장
        /// 4) GalleryStore(싱글톤)로 Upsert → gallery.json에 반영
        /// </summary>
        private void RegisterToGallery()
        {
            var entry = currentEntry;

            // ✅ 최종 키: Word 단계의 MakeStableId (예: "2-수학")
            string finalId = MakeStableId(entry);

            // 임시 경로/최종 경로
            string baseDir = Application.persistentDataPath;
            string tmpS0 = Path.Combine(baseDir, $"thumb_{pendingEvoId}_s0.png");
            string tmpS1 = Path.Combine(baseDir, $"thumb_{pendingEvoId}_s1.png");
            string finS0 = Path.Combine(baseDir, $"thumb_{finalId}_s0.png");
            string finS1 = Path.Combine(baseDir, $"thumb_{finalId}_s1.png");

            // ✅ Bit/Byte 파일을 최종 키 이름으로 이동(있을 때만)
            MoveIfExists(tmpS0, finS0);
            MoveIfExists(tmpS1, finS1);

            // ✅ Word 썸네일은 최종 키로 저장
            var sr = GetComponent<SpriteRenderer>();
            string finS2 = Path.Combine(baseDir, $"thumb_{finalId}_s2.png");
            GalleryCapture.SaveSpriteThumb(sr, $"thumb_{finalId}_s2", 256);

            // 도감 등록: 대표 썸네일은 Word
            var item = new GalleryItem
            {
                id = finalId,
                displayName = entry.word,
                desc = GetTopicForDisplay(entry),
                thumbPath = finS2,
                dateCaught = System.DateTime.Now.ToString("yyyy-MM-dd")
            };
            GalleryStore.Instance.Upsert(item);
        }

        static void MoveIfExists(string src, string dst)
        {
            try
            {
                if (File.Exists(src))
                {
                    // 덮어쓰기 방지: 기존 있으면 삭제
                    if (File.Exists(dst)) File.Delete(dst);
                    File.Move(src, dst);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Gallery] Move fail {src} -> {dst} : {ex.Message}");
            }
        }
        /// <summary>
        /// [도감 고유키 생성] 단어가 중복될 수 있으므로 "단계-단어" 형태로 고정 키를 만든다.
        /// 예: stage=2, word="수학" → "2-수학"
        /// </summary>
        static string MakeStableId(WordEntry e)
        {
            string slug = e.word.Trim().Replace(" ", ""); // 공백 제거 등 최소 정규화
            return $"{e.stage}-{slug}";
        }

        /// <summary>
        /// [표시용 카테고리 텍스트] 데이터 구조상 topic 필드는 없으므로 다음 규칙 적용:
        /// - Word(2단계)는 상위 개념이므로 "자기 자신"을 카테고리로 사용
        /// - Bit/Byte는 related[] 첫 번째 항목을 대표 카테고리로 사용
        /// - 없을 경우 "기타"
        /// </summary>
        static string GetTopicForDisplay(WordEntry e)
        {
            if (e.stage == 2) return e.word;                     // 최상위 카테고리
            if (e.related != null && e.related.Length > 0)
                return e.related[0];                              // 대표 카테고리 하나만 표시
            return "기타";
        }

    }
}
