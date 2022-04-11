using Pixeye.Actors;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Drebot
{
    public class ProcessingBattleComponentAbilities : Processor, ITick , IReceive<SignalBattleAbilityButtonClick>
    {
        private BlueprintAbilityContainer AbilitySwap;

        #region INITIALIZE

        public void Initialize(int charactersCount)
        {
            ProcessingBattle.characterAwake += CharacterAwake;
            ProcessingBattle.playerAction += PlayerAction;
            ProcessingBattle.characterAsleep += CharacterAsleep;
            ProcessingBattle.battlePositionClick += PositionClick;

            LayerBattle.Get<ProcessingBattleStatusLoading>().CheckLoad(true, typeof(ProcessingBattleComponentAbilities));
        }

        #endregion

        public void CharacterAwake(ent entity)
        {
            isSwapAvaliable = true;

            if (entity.ComponentAiLogic().isEnable == false)
                CreateAbilityButtons(entity);
        }
        public void PlayerAction(ent entity)
        {
            if (entity.ComponentAiLogic().isEnable == true)
                return;

            ChangeCurrentAbility(null, entity.ComponentAbilitySwap().abilityContainer);

            if (entity.ComponentAiLogic().isEnable == false)
                CreateAbilityButtons(entity);
        }

        private void CreateAbilityButtons(ent entityCharacter)
        {
            AbilitySwap = entityCharacter.ComponentAbilitySwap().abilityContainer;

            UIManager.Instance.CreateAbilityButtons(entityCharacter);
        }

        private BlueprintAbilityContainer AbilityContainerSelectedToCharacter = null;
        private BlueprintAbilityContainer AbilityContainerSelectedToPosition = null;

        private bool isSwapAvaliable;


        #region FINILIZE

        public void CharacterAsleep(ent entity)
        {
            AbilityContainerSelectedToCharacter = null;
            AbilityContainerSelectedToPosition = null;
        }


        private void UseAbilityToCharacter(ent caster, ent target)
        {
            bool isEndOfAction;

            if (AbilityContainerSelectedToCharacter.GetAbility(caster.ComponentEndurance().CurrentValue).isSpendSwap == true)
                isSwapAvaliable = false;

            if (AbilityContainerSelectedToCharacter.GetAbility(caster.ComponentEndurance().CurrentValue).isExtra == false)
            {
                isEndOfAction = true;
            }
            else
                isEndOfAction = false;

            if (AbilityContainerSelectedToCharacter.GetAbility(caster.ComponentEndurance().CurrentValue).UseAbility(caster, target) == false)
                DrebotDebug.DebugError("Try to use ability without effects!");
            ProcessingBattle.battleUseAbilityContrainer(caster, target, AbilityContainerSelectedToCharacter, isEndOfAction);
        }

        private void UseAbilityToPosition(ent caster, ent target)
        {
            if (AbilityContainerSelectedToPosition.GetAbility(caster.ComponentEndurance().CurrentValue).isSpendSwap == true)
            {
                isSwapAvaliable = false;
            }

            if (AbilityContainerSelectedToPosition.GetAbility(caster.ComponentEndurance().CurrentValue).UseAbility(caster, target) == false)
                DrebotDebug.DebugError("Try to use ability without effects!");

            ProcessingBattle.battleUseAbilityContrainer(caster, target, AbilityContainerSelectedToPosition, false);
        }

        public void ChangeCurrentAbility(BlueprintAbilityContainer abilityToCharacterContainer, BlueprintAbilityContainer abilityToPositionContainer)
        {
            if (abilityToCharacterContainer != null)
            {
                AbilityContainerSelectedToCharacter = abilityToCharacterContainer;
                if (abilityToCharacterContainer.GetAbility(ProcessingBattle.CurrentCharacterEntity.ComponentEndurance().CurrentValue).isInstant == true)
                {
                    UseAbilityToCharacter(ProcessingBattle.CurrentCharacterEntity, ProcessingBattle.CurrentCharacterEntity);
                    return;
                }
            }

            if (abilityToPositionContainer != null)
            {
                AbilityContainerSelectedToPosition = abilityToPositionContainer;
                if (abilityToPositionContainer.GetAbility(ProcessingBattle.CurrentCharacterEntity.ComponentEndurance().CurrentValue).isInstant == true)
                {
                    UseAbilityToCharacter(ProcessingBattle.CurrentCharacterEntity, ProcessingBattle.CurrentCharacterEntity);
                    return;
                }
            }
            else
            {
                AbilityContainerSelectedToPosition = AbilitySwap;
            }

            var signal = new SignalBattleChangeCurrentAbilityContainer(AbilityContainerSelectedToCharacter, isSwapAvaliable ? AbilityContainerSelectedToPosition : null, ProcessingBattle.CurrentCharacterEntity.ComponentAiLogic().isEnable);
            LayerBattle.Send(signal);
        }

        ent casterability;
        ent target;
        bool play = false;
        float time = 0;
        public void UseAbilityToCharacterAnimation(ent caster, ent target)
        {
            var animatorCaster = caster.transform.GetComponent<Animator>();
            var animController = animatorCaster.runtimeAnimatorController;
            var clip = animController.animationClips.First(a => a.name == AbilityContainerSelectedToCharacter.GetAbility(ProcessingBattle.CurrentCharacterEntity.ComponentEndurance().CurrentValue).animation);
            if (animatorCaster == null || animController == null || clip == null)
            { UseAbilityToCharacter(caster, target); }
            else
            {
                animatorCaster.SetTrigger(AbilityContainerSelectedToCharacter.GetAbility(ProcessingBattle.CurrentCharacterEntity.ComponentEndurance().CurrentValue).animation);

                float timeSpeedAnimation = animatorCaster.GetFloat("SpeedAnimation");
                if (timeSpeedAnimation == 0) timeSpeedAnimation = 1;

                time = clip.events.First(x => x.functionName == "AnimationUseAbility").time / timeSpeedAnimation;
                play = true;

                ProcessingBattleAnimation.AddUseAbilityToCharacterAnimation(caster, target, clip.length / timeSpeedAnimation);

                LayerBattle.Get<ProcessingBattleCharacterRotation>().Rotate(caster, target);

                casterability = caster;
                this.target = target;
            }
        }

        public void Tick(float delta)
        {
            if (play)
            {
                time -= Time.deltaTime;
                if (time < 0)
                {
                    UseAbilityToCharacter(casterability, target);
                    play = false;
                }
            }
        }

        protected void Clear()
        {
            ProcessingBattle.characterAwake -= CharacterAwake;
            ProcessingBattle.playerAction -= PlayerAction;
            ProcessingBattle.characterAsleep -= CharacterAsleep;
            ProcessingBattle.battlePositionClick -= PositionClick;
        }

        protected override void OnDispose()
        {
            Clear();
        }

        public void HandleSignal(in SignalBattleAbilityButtonClick arg)
        {
            if (arg.abilityContainer == null)
            {
                DrebotDebug.DebugError("Ability is not set!");
                return;
            }
            if (arg.abilityContainer.GetAbility(ProcessingBattle.CurrentCharacterEntity.ComponentEndurance().CurrentValue).isSpendSwap == false)
                ChangeCurrentAbility(arg.abilityContainer, null);
            else
                ChangeCurrentAbility(arg.abilityContainer, arg.abilityContainer);
        }
        public void PositionClick(ent entity)
        {
            UseAbilityToPosition(ProcessingBattle.CurrentCharacterEntity, entity);
        }
        #endregion
    }
}