using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutoMamager : MonoBehaviour
{
    public Image TouchEffect;
    public Button[] TutoButtons;

    public GameObject ExplainImg;
    public TMP_Text ExplainText;

    public TMP_Text OutputText;

    private int stepIndex = 0;
    private Button currentButton;


    private bool clicked = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(RunTutorial());
    }

    void Update()
    {
        // 현재 버튼이 있으면 손가락 위치 따라가기
        if (currentButton != null)
            TouchEffect.transform.position = currentButton.transform.position;
    }

    IEnumerator RunTutorial()
    {
        while (stepIndex < TutoButtons.Length)
        {
            //첫 번째 전화 끄기와 submit 스킵

            if (stepIndex == 1 || stepIndex == 4) {
                stepIndex++;
                continue;
            }

            //각 index마다 설명 대사 추가
            if (stepIndex == 0 || stepIndex == 2 || stepIndex==5 || stepIndex ==8 || stepIndex ==10 || stepIndex == 11)
            {
                 string[] messages = null;
                ExplainImg.SetActive(true);
                TouchEffect.gameObject.SetActive(false);

                switch (stepIndex)
                {
                    case 0:
                        messages = new string[]
                            {
                            "워드 이터를\n다운받아 주셔서\n감사합니다!",
                            "기본적인 튜토리얼을\n진행하겠습니다.",
                            "게임이 시작하면\n무작위 단어가\n정답 단어로 선정 됩니다.",
                            "여러분들은\n그 단어가 무엇인지\n맞춰야 합니다."
                            };
                        break;

                    case 2:
                        messages = new string[]
                            {
                                "자판에서\n자음과 모을을 끌어서\n보드로 가져올 수 있습니다.",
                                "자음과 모음은\n아이템 형식으로\n사용하고 나면\n개수가 차감됩니다.",
                                "보드에 있는\n자음과 모음을 결합하여\n단어를 완성할 수 있습니다.",
                                "'제출' 버튼을 누르면\n입력 단어의\n의미적인 유사도를\n알려줍니다.",
                                "정답을 맞추면\n워드이터가 성장하고\n정답을 틀리면\n베터리가 줄어드니\n신중하게 입력하세요!"
                            };
                        break;

                    case 5:
                        messages = new string[]
                            {
                                "전화에서는\n정답 단어와 관련된\n단어의 힌트를\n받을 수 있습니다."
                            };
                        break;

                    case 8:
                        messages = new string[]
                            {
                                "메세지에서는\n목숨을 소모하지 않고\n단어의 유사도를\n확인할 수 있습니다."
                            };
                        break;

                    case 10:
                        messages = new string[]
                            {
                                "갤러리에서는\n성장을 마친\n워드이터들을\n볼 수 있습니다."
                            };

                        break;

                    case 11:
                        messages = new string[]
                                {
                                "베터리는 워드이터의\n목숨 입니다!",
                                "베터리가 0이 되면\n워드이터가 죽고\n새로운 워드이터가\n태어납니다.",
                                "베터리는 시간이 지나면\n천천히 다시 차오릅니다."
                                };

                        break;
                }


                // 모든 버튼 비활성화
                foreach (var btn in TutoButtons)
                    btn.GetComponent<Image>().raycastTarget = false;

                clicked = false;
                Button stepBtn = ExplainImg.GetComponent<Button>();
                stepBtn.onClick.RemoveAllListeners();

                // 여러 문장을 순차적으로 보여줌
                for (int i = 0; i < messages.Length; i++)
                {
                    ExplainText.text = messages[i];
                    clicked = false;
                    stepBtn.onClick.RemoveAllListeners();
                    stepBtn.onClick.AddListener(() => clicked = true);

                    yield return new WaitUntil(() => clicked);
                }

                ExplainImg.SetActive(false);
            }

            TouchEffect.gameObject.SetActive(true);
            currentButton = TutoButtons[stepIndex];

            //전화 끄기는 텍스트가 나온 뒤에
            if (stepIndex == 6)
            {
                TouchEffect.gameObject.SetActive(false);

                yield return new WaitUntil(() => OutputText.text.Contains(":"));

                TouchEffect.gameObject.SetActive(true);
                currentButton.GetComponent<Image>().raycastTarget = true;

            }
            // 현재 단계 버튼만 활성화
            else currentButton.GetComponent<Image>().raycastTarget = true;


            // 이미지 위치 이동
            TouchEffect.transform.position = currentButton.transform.position;

            clicked = false;
            currentButton.onClick.RemoveAllListeners();
            currentButton.onClick.AddListener(() => clicked = true);

            // 버튼 클릭될 때까지 대기
            yield return new WaitUntil(() => clicked);

            stepIndex++;
            currentButton = null;
        }

        // 튜토리얼 종료
        TouchEffect.gameObject.SetActive(false);
        Debug.Log("튜토리얼 완료!");
    }
}
