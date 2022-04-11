using Pixeye.Actors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Drebot
{
    public class ProcessingBattleComponentCharacters : Processor
        ,IReceive<SignalBattleChangeCurrentAbilityContainer>
    {
        Group<ComponentCharacter> characters;
        private int charactersCount = -1;

        public LinkedList<ent> CharactersAlive { get; private set; }
        public LinkedList<ent> CharactersDead { get; private set; }
        public LinkedList<ent> CharactersLeft { get; private set; }
        public LinkedList<ent> CharactersRight { get; private set; }
        public LinkedList<ent> CharactersNeutral { get; private set; }

        #region INITIALIZE

        public ProcessingBattleComponentCharacters()
        {
            CharactersAlive = new LinkedList<ent>();
            CharactersDead = new LinkedList<ent>();
            CharactersLeft = new LinkedList<ent>();
            CharactersRight = new LinkedList<ent>();
            CharactersNeutral = new LinkedList<ent>();
        }

        public void Initialize(int charactersCount)
        {
            this.charactersCount = charactersCount;
            LayerBattle.Run(WaitForAllCharacters());
            ProcessingBattle.clickableAll += ClickableAll;
            ProcessingBattle.checkEndOfBattle += UpdateCharactersList;
        }

        private IEnumerator WaitForAllCharacters()
        {
            while (characters.length != charactersCount)
                yield return new WaitForFixedUpdate();

            UpdateCharactersList();
            LayerBattle.Get<ProcessingBattleStatusLoading>().CheckLoad(true, typeof(ProcessingBattleComponentCharacters));
        }

        #endregion

        #region FINILIZE

        protected void Clear()
        {
            charactersCount = -1;

            CharactersAlive.Clear();
            CharactersDead.Clear();
            CharactersLeft.Clear();
            CharactersRight.Clear();
            CharactersNeutral.Clear();

            ProcessingBattle.clickableAll -= ClickableAll;
            ProcessingBattle.checkEndOfBattle -= UpdateCharactersList;
        }

        protected void OnBattleEnded()
        {
            foreach (ent entity in characters)
            {
                if (entity.exist)
                {
                    if (entity.ComponentSide().side != Side.Left)
                        entity.Release();
                }
            }
            Clear();
        }

        protected override void OnDispose()
        {
            Clear();
        }

        #endregion

        #region BATTLE LOGIC

        public void UpdateCharactersList()
        {
            CharactersLeft.Clear();
            CharactersRight.Clear();
            CharactersNeutral.Clear();

            CharactersAlive.Clear();
            CharactersDead.Clear();

            foreach (var entity in characters)
            {
                if (entity.Has(Tag.CharacterDead))
                {
                    if (entity.ComponentSide().side != Side.Neutral)
                        CharactersDead.AddLast(entity);
                    continue;
                }
                CharactersAlive.AddLast(entity);

                if (entity.ComponentSide().side == Side.Left)
                {
                    CharactersLeft.AddLast(entity);
                    continue;
                }
                if (entity.ComponentSide().side == Side.Right)
                {
                    CharactersRight.AddLast(entity);
                    continue;
                }
                if (entity.ComponentSide().side == Side.Neutral)
                {
                    CharactersNeutral.AddLast(entity);
                    continue;
                }
            }
        }

        public void MarkWinners(ICollection<ent> winners)
        {
            foreach (var item in winners)
            {
                item.Set(Tag.CharacterWinner);
            }
        }

        public void ClickableAll(bool toogle)
        {
            if (toogle == true)
            {
                foreach (var entity in characters)
                {
                    entity.ComponentClickable().value = true;
                }
            }
            else
            {
                foreach (var entity in characters)
                {
                    entity.ComponentClickable().value = false;
                }
            }
        }

        private void UpdateAbilityArea(Side currentCharacterSide, BlueprintAbilityActive ability)
        {
            ClickableAll(false);

            if (ability.castTarget == CastTarget.Character || ability.castTarget == CastTarget.Both)
                ClickableInRange(ProcessingBattle.CurrentCharacterEntity, currentCharacterSide, ability);
        }

        private void ClickableInRange(ent start, Side currentCharacterSide, BlueprintAbilityActive ability)
        {            
            if (currentCharacterSide == Side.None)
            {
                DrebotDebug.DebugError("ComponentSide not set");
                ClickableAll(true);
                return;
            }
            if (currentCharacterSide == Side.Neutral)
            {
                DrebotDebug.DebugError("ComponentSide equal Side.Neutral. Something wrong");
                ClickableAll(true);
                return;
            }

            Dictionary<ent, int> alreadyVisited = new Dictionary<ent, int>();
            Queue<ent> frontier = new Queue<ent>();
            frontier.Enqueue(start.ComponentStandingOn().entity);
            alreadyVisited.Add(start.ComponentStandingOn().entity, 0);
            if (ability.castSelf == true)
                start.ComponentClickable().value = true;

            ent currentEntity;
            ent neighbourEntity;
            while (frontier.Count > 0)
            {
                currentEntity = frontier.Dequeue();
                foreach (var item in currentEntity.ComponentPositionNeighbours().neighbourPositions)
                {
                    neighbourEntity = item.entity;

                    if (alreadyVisited.ContainsKey(neighbourEntity) == false)
                        alreadyVisited.Add(neighbourEntity, 100);

                    if (alreadyVisited[currentEntity] + 1 < alreadyVisited[neighbourEntity])
                    {
                        frontier.Enqueue(neighbourEntity);
                        alreadyVisited[neighbourEntity] = alreadyVisited[currentEntity] + 1;
                        if (alreadyVisited[neighbourEntity] > ability.castRange)
                            continue;
                        if (neighbourEntity.ComponentUpstanding().entity == -1)
                            continue;

                        switch (ability.castTargetSide)
                        {
                            case CastTargetSide.All:
                                neighbourEntity.ComponentUpstanding().entity.ComponentClickable().value = true;
                                break;
                            case CastTargetSide.Ally:
                                if (neighbourEntity.ComponentUpstanding().entity.ComponentSide().side == currentCharacterSide)
                                    neighbourEntity.ComponentUpstanding().entity.ComponentClickable().value = true;
                                break;
                            case CastTargetSide.Enemy:
                                if (currentCharacterSide == Side.Left)
                                {
                                    if (neighbourEntity.ComponentUpstanding().entity.ComponentSide().side == Side.Right || neighbourEntity.ComponentUpstanding().entity.ComponentSide().side == Side.Neutral)
                                        neighbourEntity.ComponentUpstanding().entity.ComponentClickable().value = true;
                                }
                                else if (currentCharacterSide == Side.Right)
                                {
                                    if (neighbourEntity.ComponentUpstanding().entity.ComponentSide().side == Side.Left || neighbourEntity.ComponentUpstanding().entity.ComponentSide().side == Side.Neutral)
                                        neighbourEntity.ComponentUpstanding().entity.ComponentClickable().value = true;
                                }
                                break;
                        }
                    }
                }
            }
            return;
        }

        #endregion

        #region SIGNALS

        public void HandleSignal(in SignalBattleChangeCurrentAbilityContainer arg)
        {
            if (arg.abilityToCharacter == null)
                return;

            if (arg.abilityToCharacter.GetAbility().isInstant == true)
                return;

            if (arg.isAI == true)
                return;

            UpdateAbilityArea(ProcessingBattle.CurrentCharacterSide, arg.abilityToCharacter.GetAbility(ProcessingBattle.CurrentCharacterEntity.ComponentEndurance().CurrentValue));
        }
            
        #endregion
    }
}