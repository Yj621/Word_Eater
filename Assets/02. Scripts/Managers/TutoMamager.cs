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
            if (stepIndex == 0 || stepIndex == 2 || stepIndex==5 || stepIndex ==8 || stepIndex ==10)
            {
                ExplainImg.SetActive(true);
                TouchEffect.gameObject.SetActive(false);
                
                switch (stepIndex) {
                    case 0:
                        ExplainText.text = "배경 설정 설명문";
                        break;

                    case 2:
                        ExplainText.text = "단어 생성과 Submit 설명문";
                        break;

                    case 5:
                        ExplainText.text = "전화 버튼에 대한 설명";
                        break;

                    case 8:
                        ExplainText.text = "메세지 버튼에 대한 설명";
                        break;

                    case 10:
                        ExplainText.text = "갤러리 버튼에 대한 설명";
                        break;
                }


                clicked = false;
                Button stepBtn = ExplainImg.GetComponent<Button>();
                stepBtn.onClick.RemoveAllListeners();
                stepBtn.onClick.AddListener(() => clicked = true);

                yield return new WaitUntil(() => clicked);
                ExplainImg.SetActive(false);
            }

            TouchEffect.gameObject.SetActive(true);
            currentButton = TutoButtons[stepIndex];

            // 모든 버튼 비활성화
            foreach (var btn in TutoButtons)
                btn.GetComponent<Image>().raycastTarget = false;

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
