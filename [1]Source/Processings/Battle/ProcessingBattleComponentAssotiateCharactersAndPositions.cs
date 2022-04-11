using Pixeye.Actors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Drebot
{
    public class ProcessingBattleComponentAssotiateCharactersAndPositions : Processor
    {
        Group<ComponentCharacter> group_characters;
        Group<ComponentIndexForSetup, ComponentUpstanding> group_positions;

        private int positionsCount = -1;
        private bool positionsLoaded = false;

        private int charactersCount = -1;
        private bool charactersLoaded = false;

        public override void HandleEcsEvents()
        {
            if (group_characters.added.length > 0)
            {           
                if (group_characters.length < charactersCount)
                    return;

                if (group_characters.length == charactersCount)
                {
                    charactersLoaded = true;
                    OnBattleLoaded();
                }

            }

            if (group_positions.added.length > 0)
            {
                if (group_positions.length < positionsCount)
                    return;

                if (group_positions.length == positionsCount)
                {
                    positionsLoaded = true;
                    OnBattleLoaded();
                }
            }
        }

        #region INITIALIZE

        public void Initialize(int charactersCount)
        {
            this.charactersCount = charactersCount;
            if (group_characters.length == charactersCount)
            {
                charactersLoaded = true;
                OnBattleLoaded();
            }
            //Debug.Log(this.charactersCount);
            this.positionsCount = LayerBattle.Instance.battleFieldCounter.GetPositionsCount();
            //LayerBattle.Run(WaitForAllPositionAndCharacters());
            if (group_positions.length == positionsCount)
            {
                positionsLoaded = true;
                OnBattleLoaded();
            }
        }

        private IEnumerator WaitForAllPositionAndCharacters()
        {
            while (group_characters.length != charactersCount && group_positions.length != positionsCount)
            {
                Debug.Log(group_characters.length != charactersCount);
                Debug.Log(group_characters.length);
                Debug.Log(charactersCount);
                yield return new WaitForFixedUpdate();
            }
            Debug.Log(group_characters.length != charactersCount);
            Debug.Log(group_characters.length);
            Debug.Log(charactersCount);
            LayerBattle.Get<ProcessingBattleStatusLoading>().CheckLoad(AssociateCharactersAndPositions(), typeof(ProcessingBattleComponentAssotiateCharactersAndPositions));
        }
        private void OnBattleLoaded()
        {
            if (charactersLoaded == true && positionsLoaded == true)
            {
                LayerBattle.Get<ProcessingBattleStatusLoading>().CheckLoad(AssociateCharactersAndPositions(), typeof(ProcessingBattleComponentAssotiateCharactersAndPositions));
            }
        }

        #endregion

        #region FINILIZE

        protected void Clear()
        {
            charactersCount = -1;
            positionsCount = -1;
        }

        protected void OnBattleEnded()
        {
            Clear();
        }

        protected override void OnDispose()
        {
            Clear();
        }

        #endregion

        private int ComparePositions(ent entity1, ent entity2)
        {
            if (entity1.ComponentIndexForSetup().index <= entity2.ComponentIndexForSetup().index) return 1;
            else return -1;
        }

        private bool AssociateCharactersAndPositions()
        {
            List<ent> left = new List<ent>();
            List<ent> right = new List<ent>();
            List<ent> neutral = new List<ent>();

            ent positionEntity;
            ComponentIndexForSetup tPositionSetup;
            for (int i = 0; i < group_positions.length; i++)
            {
                positionEntity = group_positions[i];
                tPositionSetup = positionEntity.ComponentIndexForSetup();
                if (tPositionSetup.side == Side.Left && tPositionSetup.index > 0)
                    left.Add(positionEntity);
                if (tPositionSetup.side == Side.Right && tPositionSetup.index > 0)
                    right.Add(positionEntity);
                if (tPositionSetup.side == Side.Neutral && tPositionSetup.index > 0)
                    neutral.Add(positionEntity);
            }

            if (left.Count == 0 || right.Count == 0)
                return false;

            left.Sort(ComparePositions);
            right.Sort(ComparePositions);
            neutral.Sort(ComparePositions);

            int l = 0;
            int r = 0;
            int n = 0;

            ent characterEntity;
            ComponentSide tCharacterSide;
            for (int i = 0; i < group_characters.length; i++)
            {
                characterEntity = group_characters[i];
                tCharacterSide = characterEntity.ComponentSide();
                if (tCharacterSide.side == Side.Left)
                {
                    if (l >= left.Count)
                    {
                        return false;
                    }
                    characterEntity.ComponentStandingOn().entity = left[l];
                    left[l].ComponentUpstanding().entity = characterEntity;
                    l++;
                }
                if (tCharacterSide.side == Side.Right)
                {
                    if (r >= right.Count)
                    {
                        return false;
                    }
                    characterEntity.ComponentStandingOn().entity = right[r];
                    right[r].ComponentUpstanding().entity = characterEntity;
                    r++;
                }
                if (tCharacterSide.side == Side.Neutral)
                {
                    if (n >= neutral.Count)
                    {
                        return false;
                    }
                    characterEntity.ComponentStandingOn().entity = neutral[n];
                    neutral[n].ComponentUpstanding().entity = characterEntity;
                    n++;
                }
            }
            return true;
        }
    }
}