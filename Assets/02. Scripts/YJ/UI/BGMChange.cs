using UnityEngine;
using UnityEngine.UI;
public class BGMChange : MonoBehaviour
{
    public Button FirstBtn;
    public Button SecondBtn;
    public Button ThridBtn;

    public int CurBGM = 1;

    public SoundManager soundmanager;

    public void SetUI(int inputCurBGM){
       switch (inputCurBGM)  
       {
           case 1:
               FirstBtn.interactable = false;
               SecondBtn.interactable = true;
               ThridBtn.interactable = true;
               break;
           case 2:
               FirstBtn.interactable = true;
               SecondBtn.interactable = false;
               ThridBtn.interactable = true;
               break;
           case 3:
               FirstBtn.interactable = true;
               SecondBtn.interactable = true;
               ThridBtn.interactable = false;
               break;
       }


        soundmanager.BGMStart(inputCurBGM);
    }



    public void BGMChanger(int type) {
        if (type == 1)// 첫 번째 버튼
        {
            FirstBtn.interactable = false;
            SecondBtn.interactable = true;
            ThridBtn.interactable = true;

            CurBGM = 1;
            soundmanager.CurBGM = 1;
            soundmanager.BGMStart(CurBGM);

        }
        else if (type == 2)
        {// 두번째 버튼
            FirstBtn.interactable = true;
            SecondBtn.interactable = false;
            ThridBtn.interactable = true;

            CurBGM = 2;
            soundmanager.CurBGM = 2;
            soundmanager.BGMStart(CurBGM);
        }
        else {// 세 번째 버튼
            FirstBtn.interactable = true;
            SecondBtn.interactable = true;
            ThridBtn.interactable = false;

            CurBGM = 3;
            soundmanager.CurBGM = 3;
            soundmanager.BGMStart(CurBGM);
        }

        //파일에 저장
        soundmanager.SaveCurBgm();
    }
}
