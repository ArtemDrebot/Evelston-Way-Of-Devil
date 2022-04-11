using Pixeye.Actors;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Drebot
{
    public class ProcessingBattleCharacterUiHp : Processor,
        IReceive<SignalBattleCharacterTakeDamage>,
        IReceive<SignalBattleCharacterTakeHeal>
    {
        Group<ComponentHealth> group_characters;

        public ProcessingBattleCharacterUiHp()
        {
            Initialize();
        }

        public void Initialize()
        {
            ProcessingBattle.battleStart += BattleStart;
            ProcessingBattle.characterAwake += UpdateHealth;
            ProcessingBattle.characterAsleep += UpdateHealth;
        }

        void BattleStart()
        {
            for (int i = 0; i < group_characters.length; i++)
            {
                group_characters[i].ComponentHealthBar().healthBar?.SetHealth(group_characters[i].ComponentHealth().baseValue, group_characters[i].ComponentHealth().currentValue);
            }
        }

        public void UpdateHealth(ent entity)
        {
            if (entity.ComponentAiLogic().isEnable == false)
            {
                float baseHp = entity.ComponentHealth().baseValue;
                float currentHp = Storage<ComponentHealth>.Instance.TryGet(entity).currentValue;

                UIManager.Instance.UpdateHpCurrentCharacter(baseHp, currentHp);
            }
        }

        public void HandleSignal(in SignalBattleCharacterTakeDamage arg)
        {
            arg.entity.ComponentHealthBar().healthBar?.ChangeHealth(arg.entity.ComponentHealth().currentValue);

            if (arg.entity == ProcessingBattle.CurrentCharacterEntity && arg.entity.ComponentAiLogic().isEnable == false)
            {
                float baseHp = arg.entity.ComponentHealth().baseValue;
                float currentHp = Storage<ComponentHealth>.Instance.TryGet(arg.entity).currentValue;

                UIManager.Instance.UpdateHpCurrentCharacter(baseHp, currentHp);
            }
        }

        public void HandleSignal(in SignalBattleCharacterTakeHeal arg)
        {
            UpdateHealth(arg.entity);
        }
    }
}