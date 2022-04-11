using Pixeye.Actors;
using System;
using System.Collections;
using UnityEngine;

namespace Drebot
{
    public delegate void battleStep();
    public delegate void battleStepEntityOne(ent T1);
    public delegate void battleStepEntityThree(ent T1, ent T2, ent T3);
    public delegate void battleUseAbility(ent entityCaster, ent entityTarget, BlueprintAbilityActive ability, bool isEndOfAction);
    public delegate void battleUseAbilityContrainer(ent entityCaster, ent entityTarget, BlueprintAbilityContainer abilityContrainer, bool isEndOfAction);
    public delegate void boolDelegate(bool B1);

    public sealed class ProcessingBattle : Processor
    {
        private ProcessingBattleComponentCharacters processingCharacters;
        private ProcessingBattleComponentAbilities processingAbilities;

        public static battleStepEntityOne characterAwake;
        public static battleStepEntityOne playerAction;
        public static battleStepEntityOne characterAsleep;
        public static battleStep battleStart;
        public static battleStep checkEndOfBattle;
        public static battleStep nextCharacter;

        public static boolDelegate clickableAll;

        public static battleStepEntityOne battleCharacterClick;
        public static battleStepEntityOne battlePositionClick;
        public static battleUseAbilityContrainer battleUseAbilityContrainer;

        public static battleStepEntityThree battleCharacterStartMove;
        public static battleStepEntityThree battleCharacterMove;

        public static Side CurrentCharacterSide
        {
            get { return CurrentCharacterEntity.ComponentSide().side; }
        }
        public static ent CurrentCharacterEntity
        {
            get
            {
                return LayerBattle.Get<ProcessingBattleComponentATB>().CurrentEntity;
            }
        }

        #region Initizlize

        public void Initialize()
        {
            ProcessingBattleComponentATB.changeCharacter += StepCharacterAwake;
            battleUseAbilityContrainer += UseAbility;
            battleCharacterClick += CharacterClick;
            battleCharacterStartMove += CharacterMove;

            processingCharacters = LayerBattle.Get<ProcessingBattleComponentCharacters>();
            processingAbilities = LayerBattle.Get<ProcessingBattleComponentAbilities>();
            
            battleStart?.Invoke();

            MoraleFromLocationToBattle();
            EnduranceFromLocationToBattle();
            WaitForAnimation(StepNextCharacter);
        }

        public void CharacterMove(ent caster, ent oldPosition, ent newPosition)
        {
            ProcessingBattleAnimation.AddAnimationMove();
            battleCharacterMove(caster, oldPosition, newPosition);
        }

        /// <summary>
        /// Setup start morale for characters based on location morale 
        /// </summary>
        public void MoraleFromLocationToBattle()
        {
            ComponentMorale tMorale;
            foreach (var item in processingCharacters.CharactersLeft)
            {
                tMorale = item.ComponentMorale();
                tMorale.value = LayerLocation.Get<ProcessingLocationMorale>().GetMoraleForBattle();
            }
            foreach (var item in processingCharacters.CharactersRight)
            {
                tMorale = item.ComponentMorale();
                tMorale.value = 0;
            }
        }

        /// <summary>
        /// Setup start endurance for characters based on location endurance 
        /// </summary>
        public void EnduranceFromLocationToBattle()
        {
            ComponentEndurance tEndurance;
            foreach (var item in processingCharacters.CharactersLeft)
            {
                tEndurance = item.ComponentEndurance();
                tEndurance.CurrentValue = Mathf.RoundToInt((float)tEndurance.baseValue * (1f - ((50f - (float)LayerLocation.Get<ProcessingEndurance>().Current) / 100f)));
            }
            foreach (var item in processingCharacters.CharactersRight)
            {
                tEndurance = item.ComponentEndurance();
                tEndurance.CurrentValue = tEndurance.baseValue;
            }
        }

        #endregion

        #region BATTLE LOOP

        public void StepCharacterAwake()
        {
            characterAwake(CurrentCharacterEntity);
            WaitForAnimation(StepUseEffectsAtStart);
        }

        /// <summary>
        /// Checking effects before starting a character's turn.
        /// </summary>
        private void StepUseEffectsAtStart()
        {
            CurrentCharacterEntity.ComponentEffects().TryUseEffectsAtStart(CurrentCharacterEntity);
            StepRemoveDeadCharacters(StepCheckMoraleAtStart);
        }

        /// <summary>
        /// Checking morale before starting a character's turn.
        /// </summary>
        private void StepCheckMoraleAtStart()
        {
            if (CheckMoraleAtStart())
            {
                ProcessingBattleAnimation.AddMoraleAnimation(CurrentCharacterEntity, false);
                WaitForAnimation(StepCharacterAsleep);
                return;
            }

            if (CurrentCharacterEntity.ComponentAiLogic().isEnable == true)
            {
                WaitForAnimation(StepAiMove);
                return;
            }
            else
            {
                WaitForAnimation(StepPlayerAction);
            }
        }

        /// <summary>
        /// Moral check. Will the character skip a turn or not.
        /// </summary>
        /// <returns> true - skip (bad morale), false - not skip </returns>
        private bool CheckMoraleAtStart()
        {
            float tRandom = UnityEngine.Random.Range(0f, 100f);
            switch (CurrentCharacterEntity.ComponentMorale().value)
            {
                case -1:
                    if (tRandom < 14.2f)
                    {
                        CurrentCharacterEntity.ComponentMorale().startMoraleBad = true;
                        return true;
                    }
                    else break;
                case -2:
                    if (tRandom < 18.3f)
                    {
                        CurrentCharacterEntity.ComponentMorale().startMoraleBad = true;
                        return true;
                    }
                    else break;
                case -3:
                    if (tRandom < 22.5f)
                    {
                        CurrentCharacterEntity.ComponentMorale().startMoraleBad = true;
                        return true;
                    }
                    else break;
            }
            CurrentCharacterEntity.ComponentMorale().startMoraleBad = false;
            return false;
        }

        /// <summary>
        /// Moral check. Should the character get extra ATB points? 
        /// </summary>
        /// <returns> true - should get extra ATB points, false - should NOT get extra ATB points </returns>
        private bool CheckMoraleAtEnd()
        {
            float tRandom = UnityEngine.Random.Range(0f, 100f);
            switch (CurrentCharacterEntity.ComponentMorale().value)
            {
                case 1:
                    if (tRandom < 14.2f)
                    {
                        return true;
                    }
                    else break;
                case 2:
                    if (tRandom < 18.3f)
                    {
                        return true;
                    }
                    else break;
                case 3:
                    if (tRandom < 22.5f)
                    {
                        return true;
                    }
                    else break;
            }

            return false;
        }

        private void StepAiMove()
        {
            CurrentCharacterEntity.ComponentAiLogic().ScriptAiLogic.StepMove(CurrentCharacterEntity);
        }

        private void StepPlayerAction()
        {
            playerAction(CurrentCharacterEntity);
        }

        /// <summary>
        /// Checking endurance after character's action. If low endurance and bad luck, character end turn.
        /// </summary>
        private void StepCheckEndurance()
        {
            if (CurrentCharacterEntity.ComponentEndurance().CurrentValue == 0)
            {
                if (UnityEngine.Random.Range(0f, 100f) < 40f)
                {
                    ProcessingBattleAnimation.AddEnduranceAnimation(CurrentCharacterEntity);
                    WaitForAnimation(StepCharacterAsleep);
                    return;
                }
            }

            if (CurrentCharacterEntity.ComponentAiLogic().isEnable == true)
            {
                WaitForAnimation(StepAiAction);
                return;
            }
            WaitForAnimation(StepPlayerAction);
        }

        private void StepAiAction()
        {
            CurrentCharacterEntity.ComponentAiLogic().ScriptAiLogic.StepAction(CurrentCharacterEntity);
        }

        private void StepCharacterAsleep()
        {
            characterAsleep?.Invoke(CurrentCharacterEntity);
            clickableAll(false);

            if (!CurrentCharacterEntity.ComponentMorale().startMoraleBad && CheckMoraleAtEnd())
            {
                CurrentCharacterEntity.ComponentATB().currentATB = 50f;
                ProcessingBattleAnimation.AddMoraleAnimation(CurrentCharacterEntity, true);
            }
            else
                CurrentCharacterEntity.ComponentATB().currentATB = 0f;

            WaitForAnimation(StepUseEffectsAtEnd);
        }

        /// <summary>
        /// Apply effects on the end character's turn.
        /// Применяем эффекты в конце хода
        /// </summary>
        private void StepUseEffectsAtEnd()
        {
            CurrentCharacterEntity.ComponentEffects().TryUseEffectsAtEnd(CurrentCharacterEntity);
            StepRemoveDeadCharacters(StepCheckEndOfBattle);
        }

        /// <summary>
        /// RemoveDeadCharacters
        /// </summary>
        /// <param name="next"></param>
        public void StepRemoveDeadCharacters(Action next)
        {
            ProcessingBattleCharacterDeath tProcessingDeath = LayerBattle.Get<ProcessingBattleCharacterDeath>();
            if (tProcessingDeath == null)
            {
                WaitForAnimation(next);
                return;
            }
            tProcessingDeath.RemoveDead();
            if (CurrentCharacterEntity.Has(Tag.CharacterDead))
                WaitForAnimation(StepNextCharacter);
            else
                WaitForAnimation(next);
        }

        /// <summary>
        /// Checking if the fight is over
        /// </summary>
        public void StepCheckEndOfBattle()
        {
            checkEndOfBattle();

            int result = LayerBattle.Get<ProcessingBattleEnd>().CheckEndOfBattle();
            if (result != 0)
            {
                StepEndBattle(result == 1 ? true : false);
                return;
            }

            WaitForAnimation(StepNextCharacter);
        }

        /// <summary>
        /// Show end battle UI
        /// </summary>
        /// <param name="isPlayerWin"></param>
        private void StepEndBattle(bool isPlayerWin)
        {
            UIManager.Instance.EndOfBattle(isPlayerWin);
        }

        /// <summary>
        /// Move to next character
        /// </summary>
        private void StepNextCharacter()
        {
            nextCharacter?.Invoke();
        }

        #endregion

        private void WaitForAnimation(Action Method)
        {
            LayerBattle.Run(WaitForUnlock(Method));
        }

        private IEnumerator WaitForUnlock(Action Method)
        {
            while (!ProcessingBattleAnimation.ReadyToContinue())
            {
                yield return new WaitForFixedUpdate();
            }

            Method();
        }

        #region FINILIZE

        protected override void OnDispose()
        {
            ProcessingBattleComponentATB.changeCharacter -= StepCharacterAwake;

            battleUseAbilityContrainer -= UseAbility;
            battleCharacterClick -= CharacterClick;
            battleCharacterStartMove -= CharacterMove;
        }

        #endregion


        /// <summary>
        /// Вызов применения способности на персонажа
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="target"></param>
        private void StepCharacterUseAbility(ent caster, ent target)
        {
            processingAbilities.UseAbilityToCharacterAnimation(caster, target);
        }

        public void CharacterClick(ent entity)
        {
            StepCharacterUseAbility(CurrentCharacterEntity, entity);
        }

        #region SIGNALS

        public void StepCharacterAwake(ent entity)
        {
            StepCharacterAwake();
        }

        //public void UseAbility(ent entityCaster, ent entityTarget, BlueprintAbilityActive ability, bool isEndOfAction)
        //{
        //    // Если способность не заканчивает ход персонажа, проверяем его на выносливость.
        //    if (isEndOfAction == false)
        //        WaitForAnimation(StepCheckEndurance);
        //    else
        //        WaitForAnimation(StepCharacterAsleep);
        //}

        public void UseAbility(ent entityCaster, ent entityTarget, BlueprintAbilityContainer ability, bool isEndOfAction)
        {
            // Если способность не заканчивает ход персонажа, проверяем его на выносливость.
            if (isEndOfAction == false)
                WaitForAnimation(StepCheckEndurance);
            else
                WaitForAnimation(StepCharacterAsleep);
        }
        #endregion
    }
}