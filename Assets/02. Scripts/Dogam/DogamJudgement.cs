using UnityEngine;

public class DogamJudgement : MonoBehaviour
{
    public string judStr = "";
    public FileManager filemanager;
    public RectTransform content;
    public void OpenDogam() {
        judStr = filemanager.LoadGallerySpriteId();
        Checkid();
    }

    // contetn의 모든 자식 오브젝트를 돌며 id를 조회 하며 확인
    public void Checkid() {
        foreach (Transform child in content) {
            DogamitselfInfo info = child.GetComponent<DogamitselfInfo>();

            if (info == null)
                continue;

            string childId = info.sprid;

            if (judStr.Contains(childId)) {
                info.isOn = true;
                info.DogamOn();
            }
        }
    }
}
