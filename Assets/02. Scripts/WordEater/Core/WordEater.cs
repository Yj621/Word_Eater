using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using WordEater.Data;
using WordEater.Services;
using WordEater.Systems;

namespace WordEater.Core
{
    /// <summary>
    /// 워드 이터의 생애 주기와 상태를 관리하는 메인 클래스임
    /// </summary>
    public class WordEater : MonoBehaviour
    {
        #region [Dependencies & Config]
        [Header("시스템 연결")]
        [SerializeField] private WordAssignmentService wordService;  // 단어 배정 서비스
        [SerializeField] private BatterySystem battery;              // 배터리 시스템
        [SerializeField] private SubmitManager submitmanager;        // 제출 관리자
        [SerializeField] private GameManager gamemanager;            // 게임 매니저
        [SerializeField] private GalleryUIManager galleryUIManager;  // 도감 UI
        [SerializeField] private FileManager filemanager;            // 파일 관리자
        [SerializeField] private AlgorithmMessage algoMessage;
        [SerializeField] private ADPopup adPopup;
        private string nameBit, nameByte, nameWord;

        [Header("에셋 연결")]
        [SerializeField] private Sprite[] stageSprites;      // 0:Bit, 1:Byte, 2:Word 단계별 이미지
        [SerializeField] private Sprite reviveTicketSprite;  // 부활권 아이콘
        #endregion

        #region [State Data]
        [Header("런타임 상태")]
        [SerializeField] private GrowthStage stage = GrowthStage.Bit; // 현재 성장 단계
        [SerializeField] private string currentAnswer;                // 현재 정답 단어
        private WordEntry currentEntry;                               // 현재 단어 데이터 (연관어 포함)
        private string pendingEvoId;                                  // 진화 전까지 사용할 임시 ID

        /// <summary>
        /// 현재 사망 상태인지 확인하는 프로퍼티임
        /// </summary>
        public bool isDead { get; private set; } = false;

        /// <summary>
        /// 현재 배터리 잔량 (%) 외부 공개
        /// </summary>
        public int CurrentBatteryPercent => battery != null ? battery.CurrentPercent : 0;

        // 이미지 컴포넌트 캐싱용 변수임
        private Image _targetImage;
        private Image TargetImage => _targetImage ? _targetImage : (_targetImage = GetComponent<Image>());
        #endregion

        // [외부 접근용 프로퍼티]
        public WordEntry CurrentEntry => currentEntry;
        public GrowthStage CurrentStage => stage;
        public string Answer => currentAnswer;

        public WordImageDatabase wordimgdatabase;
        public string wordImgString = "";

        private void Awake()
        {
        }

        #region [Initialization & Stage Management]

        /// <summary>
        /// 저장된 파일 데이터를 기반으로 워드이터 상태를 복구함
        /// </summary>
        public void LoadFromSaveData(int level, string savedAnswer)
        {
            // 레벨 범위 벗어나지 않게 클램핑함
            stage = (GrowthStage)Mathf.Clamp(level, 0, 2);

            // 초기 단어 데이터 로드하고 WordBank 에서 찾아서 매칭
            currentEntry = wordService.PickWordFromFile(level, savedAnswer);
            currentAnswer = currentEntry.word;

            if (stage == GrowthStage.Bit) nameByte = currentAnswer;

            // 외형 업데이트하고 이벤트 알림
            UpdateVisuals(1);
            NotifyNewWordAssigned();
            SaveCheckpoint();
        }

        /// <summary>
        /// 단계를 시작하거나 초기화함
        /// </summary>
        public void BeginStage(GrowthStage nextStage, bool initial = false)
        {
            // 히스토리 초기화함
            gamemanager.HistoryLIne = "";
            gamemanager.RelevantLine = "";
            gamemanager.RelevantResult.Clear();

            if (algoMessage != null)
            {
                algoMessage.ClearAllMessages();
            }

            if (initial)
            {
                // 완전 초기화일 경우 Bit 상태로 리셋함
                ResetToBitConfig();

                // Count 값들 0으로 초기화
                gamemanager.callCount = 0;
                gamemanager.msgCount = 0;
                gamemanager.submitCount = 0;
                gamemanager.lockCount = 0;

                gamemanager.saveCountInmanager(4);
            }
            else
            {
                // 다음 단계로 설정함
                stage = nextStage;
            }

            UpdateVisuals();
            SaveCheckpoint();

            // 현재 상태를 파일에 저장함
            filemanager.SaveWordEaterInfo((int)stage, currentAnswer, gamemanager.HistoryLIne, gamemanager.RelevantResult, wordImgString, gamemanager.RelevantLine);
            NotifyNewWordAssigned();
        }

        /// <summary>
        /// Bit 단계(새 게임)로 모든 상태를 리셋함
        /// </summary>
        private void ResetToBitConfig()
        {
            stage = GrowthStage.Bit;
            isDead = false;
            enabled = true;

            // 배터리 가득 채움
            if (battery != null) battery.RefillToMax();

            // 새 단어 뽑고 이번 생애의 고유 ID 생성함
            currentEntry = wordService.PickInitialWord();
            currentAnswer = currentEntry.word;
            nameBit = currentAnswer; // 첫 번째 단어 저장
            pendingEvoId = $"evo_{System.DateTime.UtcNow.Ticks}";

            // 초기 썸네일 캡처함
            CaptureThumbnail($"thumb_{pendingEvoId}_s0");
            /*
                        // 튜토리얼 씬이 아니면 관련 단어 버튼 활성화함
                        if (SceneManager.GetActiveScene().name != "TutoScene")
                        {
                            submitmanager.OnRelevantButton();
                        }*/
        }

        /// <summary>
        /// 현재 단계에 맞는 스프라이트로 교체하고 썸네일을 캡처함
        /// </summary>
        private void UpdateVisuals(int type = 0) // 0-> 새로운 이미지 , 1 -> 이미지 불러오기
        {
            // 1. TargetImage가 없으면 중단
            if (TargetImage == null) return;

            // [디버깅] 데이터베이스 연결 확인
            if (wordimgdatabase == null)
            {
                Debug.LogError("❌ [치명적 오류] WordImgDatabase가 연결되지 않았습니다! 인스펙터를 확인하세요.");
                return;
            }
            if (wordimgdatabase.entries == null || wordimgdatabase.entries.Count == 0)
            {
                Debug.LogError("❌ [데이터 오류] WordImgDatabase는 연결됐지만, 내용물(Entries)이 비어있습니다!");
                return;
            }

            // 단계에 맞는 스프라이트 적용함
            int index = (int)stage;

            try // 안전 장치 추가
            {
                if (type == 0)
                {
                    // bit 단계면 랜덤 선택
                    if (index == 0)
                    {
                        // [수정] stageSprites 여부와 상관없이 DB가 있으면 무조건 수행
                        if (index >= 0)
                        {
                            int randomIndex = UnityEngine.Random.Range(0, wordimgdatabase.entries.Count);
                            // 여기서 entries 접근 시 에러가 났었습니다.
                            var entry = wordimgdatabase.entries[randomIndex];

                            wordImgString = entry.wordId;
                            TargetImage.sprite = entry.stage1;

                            var pet = GetComponent<WordEaterPet>();
                            if (pet != null) pet.SetAnimSprites(entry.stage1Anim?.ToArray());

                            Debug.Log($"[Visual Update] New Bit Image: {wordImgString}");
                        }
                    }
                    // bit 아니면 다음 단계로
                    else
                    {
                        if (stageSprites != null && index >= 0 && !string.IsNullOrEmpty(wordImgString))
                        {
                            WordStageImages cur = wordimgdatabase.entries.Find(e => e.wordId == wordImgString);

                            if (cur != null) // 찾는 ID가 없을 수도 있음
                            {
                                var pet = GetComponent<WordEaterPet>();

                                // byte
                                if (index == 1)
                                {
                                    TargetImage.sprite = cur.stage2;
                                    if (pet != null) pet.SetAnimSprites(cur.stage2Anim?.ToArray());
                                }
                                // word
                                if (index == 2)
                                {
                                    TargetImage.sprite = cur.stage3;
                                    if (pet != null) pet.SetAnimSprites(cur.stage3Anim?.ToArray());
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ ID '{wordImgString}'에 해당하는 이미지를 DB에서 찾을 수 없습니다.");
                            }
                        }
                    }

                    // 살아있을 때만 썸네일 저장함
                    if (!isDead)
                    {
                        string suffix = stage == GrowthStage.Bit ? "s0" : (stage == GrowthStage.Byte ? "s1" : "s2");
                        CaptureThumbnail($"thumb_{pendingEvoId}_{suffix}");
                    }
                }
                else // type == 1 (로드 시)
                {
                    if (stageSprites != null && index >= 0 && !string.IsNullOrEmpty(wordImgString))
                    {
                        WordStageImages cur = wordimgdatabase.entries.Find(e => e.wordId == wordImgString);

                        if (cur != null)
                        {

                            var pet = GetComponent<WordEaterPet>();

                            // 0: Bit, 1: Byte, 2: Word
                            if (index == 0)
                            {
                                TargetImage.sprite = cur.stage1;
                                if (pet != null) pet.SetAnimSprites(cur.stage1Anim?.ToArray());
                            }
                            if (index == 1)
                            {
                                TargetImage.sprite = cur.stage2;
                                if (pet != null) pet.SetAnimSprites(cur.stage2Anim?.ToArray());
                            }
                            if (index == 2)
                            {
                                TargetImage.sprite = cur.stage3;
                                if (pet != null) pet.SetAnimSprites(cur.stage3Anim?.ToArray());
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UpdateVisuals 내부 에러 발생: {e.Message}");
            }
        }
        #endregion

        #region [Game Loop]
        /// <summary>
        /// 제출 전 배터리 비용을 지불할 수 있는지 확인함
        /// UI 팝업 대기를 위해 성공 시 실행할 로직(onSuccess)을 인자로 받음
        /// </summary>
        public void TryPayForSubmit(Action onSuccess)
        {
            if (isDead) return;

            ActionType costType = GetSubmitAction();

            // 1. 배터리가 충분한 경우 (정상 진행)
            if (battery.TryConsume(costType))
            {
                // 즉시 성공 로직 실행 (서버 통신 시작)
                onSuccess?.Invoke();

            }
            // 배터리가 부족한 경우 (팝업 띄우기)
            else
            {
                adPopup.Configure("배터리 부족 경고", "강제 제출", "닫기");
                adPopup.SetAdMode(false);

                adPopup.YesNoPanelShow(
                      onAccept: () =>
                      {
                          battery.ForceEmpty();
                          onSuccess?.Invoke();
                          // 여기도 사망 체크 없음 (DoFeedData 안에서 처리됨)
                      },
                      onDecline: () =>
                      {
                          NoticeManager.Instance.ShowSticky("배터리가 부족합니다");
                      }
                );
            }
        }

        /// <summary>
        /// 소모 후 사망 체크 헬퍼 메서드
        /// </summary>
        private void CheckBatteryDeath()
        {
            if (battery.CurrentPercent <= 0)
            {
                Debug.Log("[결제 후 소진] 사망 처리");
                StartCoroutine(DieSequenceRoutine());
            }
        }

        /// <summary>
        /// 미니게임 시작 비용 지불 (콜백 방식)
        /// </summary>
        public void TryPayForMiniGame(Action onSuccess)
        {
            if (isDead) return;

            // 1. 배터리 충분함
            if (battery.TryConsume(ActionType.MinigameStart))
            {
                onSuccess?.Invoke();
            }
            // 2. 배터리 부족함 -> 팝업
            else
            {
                adPopup.Configure("배터리 부족", "강제 시작", "포기");
                adPopup.SetAdMode(false); // 광고 없이

                adPopup.YesNoPanelShow(
                    onAccept: () =>
                    {
                        // 강제 시작: 배터리 0으로 만들고 게임 시작
                        battery.ForceEmpty();

                        onSuccess?.Invoke();

                        // 미니게임 시작 비용도 없는데 했으므로 사망 처리
                        StartCoroutine(DieSequenceRoutine());
                    },
                    onDecline: () =>
                    {
                        NoticeManager.Instance.ShowSticky("배터리가 부족합니다");
                    }
                );
            }
        }

        /// <summary>
        /// 유저 입력을 받아 정답 여부를 판정하고 결과를 처리함
        /// </summary>
        public void DoFeedData(string userInput)
        {
            if (isDead) return;

            // 정답 판정 수행함
            bool isCorrect = CheckAnswer(userInput);
            GameEvents.OnFeedResult?.Invoke(userInput, isCorrect);

            if (isCorrect)
            {
                // 정답이면 진화 로직 실행함
                ProcessEvolution();
            }
            else
            {
                // 오답이면 피드백 줌
                HandleMistake();
                // 오답인 경우에만 배터리를 확인하여 사망 처리
                if (battery.CurrentPercent <= 0)
                {
                    StartCoroutine(DieSequenceRoutine());
                }
            }

        }

        /// <summary>
        /// 입력값과 정답을 비교함
        /// </summary>
        private bool CheckAnswer(string input)
        {
            return string.Equals(input.Trim(), currentAnswer.Trim(), StringComparison.Ordinal);
        }

        /// <summary>
        /// 오답 시 진동 등 피드백을 처리함
        /// </summary>
        private void HandleMistake()
        {
            GameEvents.RaiseMistakeHit();
            Handheld.Vibrate();
        }

        /// <summary>
        /// 정답을 맞췄을 때 다음 단계로 진화하거나 엔딩을 봄
        /// </summary>
        public void ProcessEvolution()
        {
            // 이미 최종 단계(Word)라면 엔딩 처리함
            if (stage == GrowthStage.Word)
            {
                nameWord = currentAnswer; // 최종 단어 저장
                HandleEnding();
                return;
            }

            // 다음 단계 연관 단어를 배정받음
            currentEntry = wordService.PickNextLinkedWord(currentEntry, stage);
            currentAnswer = currentEntry.word;

            // 단계 상승시키고 배터리 채워줌
            stage++;
            battery.RefillToMax();

            // 보상 아이템 지급함
            ItemDropManager.Instance.ObtainRandomItem();
            GameEvents.OnEvolved?.Invoke(stage);

            // 다음 단계 시작함
            BeginStage(stage);
            // 진화가 완료되었으므로, 새로운 단어에 대한 힌트(연관어)를 서버에 요청합니다.
            if (submitmanager != null)
            {
                submitmanager.OnRelevantButton();
            }
        }

        /// <summary>
        /// 게임 클리어 엔딩을 처리하고 도감에 등록함
        /// </summary>
        private void HandleEnding()
        {
            // 1. 진화 이벤트 발생
            GameEvents.OnEvolved?.Invoke(stage);

            // 2. 도감(데이터) 등록 - 백그라운드 작업
            RegisterToGallery();

            // 3. UI 갱신 (도감에 New 표시 등을 위함)
            galleryUIManager.Refresh();

            // [중요] 아이템 획득(ObtainRandomItem) 코드는 여기서 삭제합니다.
            // GameManager의 시퀀스 안에서 실행하도록 변경했기 때문입니다.

            // 4. 게임 매니저에게 클리어 시퀀스 시작 요청
            gamemanager.EndingController(2);
        }

        #endregion

        #region [Death & Revive]

        /// <summary>
        /// 사망 연출을 위해 약간의 딜레이를 줌
        /// </summary>
        private IEnumerator DieSequenceRoutine()
        {
            yield return new WaitForSeconds(0.35f);
            OnDeath();
        }

        /// <summary>
        /// 실제 사망 처리를 수행하고 부활 로직을 시작함
        /// </summary>
        private void OnDeath()
        {
            if (isDead) return;
            isDead = true;
            enabled = false;
            GameEvents.OnDied?.Invoke();

            // 부활권 보유 여부부터 체크함
            CheckReviveTicketAvailability();
        }

        /// <summary>
        /// 부활권 아이템이 있는지 확인함
        /// </summary>
        private void CheckReviveTicketAvailability()
        {
            int ticketCount = ItemManager.Instance.GetCount(ItemType.ReviveTicket);

            if (ticketCount > 0)
            {
                // 부활권이 있으면 팝업으로 물어봄
                UIManager.Instance.ShowConfirmPopup(
                    "부활권 사용",
                    $"부활권을 사용하여 이어하시겠습니까?\n(남은 개수: {ticketCount}개)",
                    onYes: TryUseReviveTicket,
                    onNo: ShowAnswerAndTriggerAdLogic,
                    itemIcon: reviveTicketSprite
                );
            }
            else
            {
                // 없으면 바로 정답 공개 후 광고 로직으로 넘어감
                ShowAnswerAndTriggerAdLogic();
            }
        }

        /// <summary>
        /// 부활권 사용을 시도함
        /// </summary>
        private void TryUseReviveTicket()
        {
            if (ItemManager.Instance.TryUseItem(ItemType.ReviveTicket))
            {
                RevivePlayer();
                UIManager.Instance.Show("부활권 사용!\n단어를 다시 맞춰보세요.");
            }
            else
            {
                // 사용 실패 시(혹시 모를 오류) 광고 로직으로 넘어감
                ShowAnswerAndTriggerAdLogic();
            }
        }

        /// <summary>
        /// 정답을 보여주고 확인 시 광고 부활 로직을 호출함
        /// </summary>
        private void ShowAnswerAndTriggerAdLogic()
        {
            UIManager.Instance.ShowEmergencyAlarm("정답단어", currentAnswer, 2.0f, CheckAdRevive);
        }

        /// <summary>
        /// 광고 시스템을 통해 부활 기회를 제공함
        /// </summary>
        private void CheckAdRevive()
        {
            if (GameReviveSystem.Instance != null)
            {
                // 광고 보고 부활할지 포기할지 결정함
                GameReviveSystem.Instance.OnPlayerDied(onGiveUp: () =>
                {
                    gamemanager.EndingController(1); // 포기하면 게임오버
                });
            }
            else
            {
                // 시스템 없으면 바로 게임오버
                gamemanager.EndingController(1);
            }
        }

        /// <summary>
        /// 플레이어를 다시 활성화하고 배터리를 채움
        /// </summary>
        public void RevivePlayer()
        {
            isDead = false;
            enabled = true;
            battery.RefillToMax();
        }

        #endregion

        #region [Helpers]

        // 새 단어가 할당되었음을 이벤트로 알림
        private void NotifyNewWordAssigned() => GameEvents.OnNewWordAssigned?.Invoke(currentAnswer);

        // 현재 상태를 체크포인트에 저장함
        private void SaveCheckpoint()
        {
            if (GameReviveSystem.Instance != null && battery != null)
            {
                GameReviveSystem.Instance.SaveCheckpoint(this, battery.CurrentPercent);
            }
        }

        // 현재 단계에 따른 제출 비용 타입을 반환함
        private ActionType GetSubmitAction()
        {
            switch (stage)
            {
                case GrowthStage.Byte: return ActionType.SubmitByte;
                case GrowthStage.Word: return ActionType.SubmitWord;
                default: return ActionType.SubmitBit;
            }
        }

        // 현재 이미지를 캡처해서 파일로 저장함
        private void CaptureThumbnail(string fileName)
        {
            if (TargetImage != null)
                GalleryCapture.SaveSpriteThumb(TargetImage, fileName, 256);
        }

        // 현재 워드이터를 도감(JSON)에 등록함
        private void RegisterToGallery()
        {
            string finalId = $"{currentEntry.stage}-{currentEntry.word.Trim().Replace(" ", "")}";
            string baseDir = Application.persistentDataPath;

            // 임시 썸네일들을 최종 ID 이름으로 변경함
            MoveThumbFile(baseDir, $"thumb_{pendingEvoId}_s0.png", $"thumb_{finalId}_s0.png");
            MoveThumbFile(baseDir, $"thumb_{pendingEvoId}_s1.png", $"thumb_{finalId}_s1.png");

            // 최종 단계 썸네일 저장함
            string finalS2Path = Path.Combine(baseDir, $"thumb_{finalId}_s2.png");
            CaptureThumbnail($"thumb_{finalId}_s2");

            // JSON 데이터 생성 및 업데이트함
            var item = new GalleryItem
            {
                id = finalId,
                displayNameBit = nameBit,
                displayNameByte = nameByte,
                displayNameWord = currentAnswer, // 현재(Word단계) 단어                desc = GetDisplayTopic(currentEntry),
                
                callCount = GameManager.Instance.callCount,
                msgCount = GameManager.Instance.msgCount,
                submitCount = GameManager.Instance.submitCount,
                lockCount = GameManager.Instance.lockCount,
                
                thumbPath = finalS2Path,
                dateCaught = System.DateTime.Now.ToString("yyyy-MM-dd"),
                spriteid = wordImgString
            };

            if (FileManager.Instance != null)
            {
                FileManager.Instance.UpsertGalleryItem(item);
            }
        }

        // 파일 이름을 변경함 (덮어쓰기 처리 포함)
        private void MoveThumbFile(string dir, string srcName, string dstName)
        {
            string src = Path.Combine(dir, srcName);
            string dst = Path.Combine(dir, dstName);
            try
            {
                if (File.Exists(src))
                {
                    if (File.Exists(dst)) File.Delete(dst);
                    File.Move(src, dst);
                }
            }
            catch { }
        }

        // 도감에 표시할 카테고리 주제를 가져옴
        private string GetDisplayTopic(WordEntry e)
        {
            if (e.stage == 2) return e.word;
            return (e.related != null && e.related.Length > 0) ? e.related[0] : "기타";
        }

        /// <summary>
        /// 데이터 복구용 메서드임 (ReviveSystem 등에서 사용)
        /// </summary>
        public void RestoreAnswer(string answer, GrowthStage s)
        {
            stage = s;
            currentAnswer = answer;
            NotifyNewWordAssigned();
            UpdateVisuals(1);
        }
        #endregion
    }
}