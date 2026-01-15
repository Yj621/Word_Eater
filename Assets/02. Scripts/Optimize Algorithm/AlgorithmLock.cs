using System.Collections.Generic;
using UnityEngine;

// 힌트 제공 방식을 정의하는 열거형임
public enum LockHintMode
{
    LengthOnly,      // ● ● ● ● (글자수만 보여줌)
    FirstChosung,    // ㄱ ● ● ● (첫 글자 초성 공개)
    LastChosung      // ● ● ● ㄱ (마지막 글자 초성 공개)
}

public class AlgorithmLock : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject root;           // 전체 UI 패널
    [SerializeField] private Transform slotsParent;     // 슬롯들이 생성될 부모 Transform
    [SerializeField] private LockPassword slotPrefab;   // 생성할 슬롯 프리팹

    // 생성된 슬롯들을 재사용하기 위해 리스트로 관리함 (오브젝트 풀링 개념)
    private readonly List<LockPassword> spawned = new();

    /// <summary>
    /// 외부에서 정답 단어와 힌트 모드를 받아 UI를 갱신하는 메인 함수
    /// </summary>
    public void ShowHint(string answerWord, LockHintMode mode)
    {
        // UI가 꺼져있으면 켬
        if (root != null) root.SetActive(true);

        // 정답이 비어있으면 예외 처리로 '?' 할당함
        if (string.IsNullOrEmpty(answerWord))
            answerWord = "?";

        // 너무 길어지는 것 방지 (1~50글자 제한)
        int length = Mathf.Clamp(answerWord.Length, 1, 50);

        // 필요한 만큼 슬롯(LockPassword)을 확보함
        EnsureSlots(length);

        // 정답 길이만큼만 슬롯을 켜고, 나머지는 끔
        for (int i = 0; i < spawned.Count; i++)
        {
            bool visible = (i < length);
            spawned[i].gameObject.SetActive(visible);

            // 안 보이는 슬롯은 설정할 필요 없으니 건너뜀
            if (!visible) continue;

            // 일단 모든 슬롯을 '점(●)' 상태로 초기화함
            spawned[i].SetDot(active: true);
        }

        // 1글자는 힌트 주면 바로 정답이라 초성 공개 안 함
        if (length == 1) return;

        // 길이만 보여주는 모드면 여기서 끝냄
        if (mode == LockHintMode.LengthOnly) return;

        // 정답에서 해당 위치의 초성을 추출함
        char chosungChar = GetSingleChosung(answerWord, mode);

        // 모드에 따라 첫 번째 혹은 마지막 슬롯에 초성을 박아줌
        if (mode == LockHintMode.FirstChosung)
            spawned[0].SetChar(chosungChar.ToString());
        else if (mode == LockHintMode.LastChosung)
            spawned[length - 1].SetChar(chosungChar.ToString());
    }

    // UI 숨기는 함수
    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    // 필요한 개수만큼 슬롯이 있는지 확인하고, 부족하면 더 생성
    private void EnsureSlots(int required)
    {
        while (spawned.Count < required)
        {
            var slot = Instantiate(slotPrefab, slotsParent);
            spawned.Add(slot);
        }
    }

    /// <summary>
    /// 단어에서 모드에 맞는 초성을 추출하는 로직
    /// </summary>
    private char GetSingleChosung(string word, LockHintMode mode)
    {
        if (string.IsNullOrEmpty(word)) return '?';

        // 첫 글자냐 끝 글자냐 타겟 문자 결정함
        char target = (mode == LockHintMode.FirstChosung) ? word[0] : word[word.Length - 1];

        // 타겟 문자가 한글 범위(0xAC00 ~ 0xD7A3) 안에 있는지 체크함
        if (target >= 0xAC00 && target <= 0xD7A3)
        {
            int uniVal = target - 0xAC00;
            int choIndex = uniVal / (21 * 28); // 초성 인덱스 계산 공식
            return KoreanUtils_OneChar.GetCho(choIndex);
        }

        // 한글 아니면(영어, 숫자 등) 그냥 그대로 반환
        return target;
    }
}