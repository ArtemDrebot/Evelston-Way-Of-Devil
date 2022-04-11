using Pixeye.Actors;

namespace Drebot
{
    /// <summary>
    /// Change endurance for characters in battle
    /// </summary>
    public class ProcessingBattleCharacterReplenishEndurance : Processor,
        IReceive<SignalBattleCharacterSpendEndurance>
    {
        private int replenishValue = 1;

        public ProcessingBattleCharacterReplenishEndurance()
        {
            Initialize();
        }

        public void Initialize()
        {
            ProcessingBattle.characterAwake += ReplenishEndurance;
            ProcessingBattle.characterAsleep += ReplenishEndurance;
        }

        public void ReplenishEndurance(ent entity)
        {
            if (entity.ComponentAiLogic().isEnable == false)
            {
                float previewEndurance = entity.ComponentEndurance().CurrentValue;
                entity.ComponentEndurance().CurrentValue += replenishValue;
                float baseEndurance = entity.ComponentEndurance().baseValue;
                float currentEndurance = entity.ComponentEndurance().CurrentValue;
                UIManager.Instance.UpdateEnduranceCurrentCharacter(baseEndurance, previewEndurance, currentEndurance);
            }
        }

        public void HandleSignal(in SignalBattleCharacterSpendEndurance arg)
        {
            if (arg.entity.ComponentAiLogic().isEnable == false)
            {
                float previewEndurance = arg.entity.ComponentEndurance().CurrentValue;
                arg.entity.ComponentEndurance().CurrentValue -= arg.value;
                float baseEndurance = arg.entity.ComponentEndurance().baseValue;
                float currentEndurance = arg.entity.ComponentEndurance().CurrentValue;
                UIManager.Instance.UpdateEnduranceCurrentCharacter(baseEndurance, previewEndurance, currentEndurance);
            }
        }
    }
}