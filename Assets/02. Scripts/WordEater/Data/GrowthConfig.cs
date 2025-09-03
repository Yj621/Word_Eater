using UnityEngine;
using WordEater.Core;
using WordEater.Data;

namespace WordEater.Data
{
    [CreateAssetMenu(fileName = "GrowthConfig", menuName = "WordEater/GrowthConfig")]
    public class GrowthConfig : ScriptableObject
    {
        // Bit의 턴 수, 오답횟수, 정답횟수
        public StageConfig bit = new StageConfig { turnsPerStage = 4, maxMistakes = 2, requiredCorrectToAdvance = 2 };
        // Byte의 턴 수, 오답횟수, 정답횟수
        public StageConfig byt = new StageConfig { turnsPerStage = 8, maxMistakes = 4, requiredCorrectToAdvance = 4 };
        // Word의 턴 수, 오답횟수, 정답횟수
        public StageConfig wod = new StageConfig { turnsPerStage = 12, maxMistakes = 6, requiredCorrectToAdvance = 6 };

        public StageConfig Get(GrowthStage stage)
        {
            switch(stage)
            {
                case GrowthStage.Bit: return bit;
                case GrowthStage.Byte: return byt;
                case GrowthStage.Word: return wod;
                default: return bit;
            }
        }
    }

}
