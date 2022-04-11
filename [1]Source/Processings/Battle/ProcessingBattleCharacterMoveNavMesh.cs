using Pixeye.Actors;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Drebot
{
    /// <summary>
    /// Processing for move characters on battle scene. 
    /// if character have MonoMoveNavMeshInBattle, play animation.
    /// </summary>
    public sealed class ProcessingBattleCharacterMoveNavMesh : Processor, ITick
    {
        Group<ComponentCharacter> group_characters;

        private int charactersCount = -1;
        ent characterCurrentEnt;
        MonoMoveNavMeshInBattle characterCurrent;
        bool moveCharacter = false;


        #region INITIALIZE

        public void Initialize(int charactersCount)
        {
            ProcessingBattle.battleCharacterMove += CharacterMove;
            this.charactersCount = charactersCount;
            LayerBattle.Run(WaitForAllPositionAndCharacters());
        }

        private IEnumerator WaitForAllPositionAndCharacters()
        {
            while (group_characters.length != charactersCount)
                yield return new WaitForFixedUpdate();

            SetUpCharactersOnPositions();
            LayerBattle.Get<ProcessingBattleStatusLoading>().CheckLoad(true, typeof(ProcessingBattleCharacterMoveNavMesh));
        }

        #endregion

        /// <summary>
        /// Set the correct position for the character.
        /// </summary>
        public void SetUpCharactersOnPositions()
        {
            Vector3 position;
            for (int i = 0; i < group_characters.length; i++)
            {
                position = group_characters[i].ComponentStandingOn().entity.ComponentCharacterPositionPivot().positionPivot.position;
                
                NavMeshAgent agent = group_characters[i].transform.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    NavMeshHit closestHit;
                    if (NavMesh.SamplePosition(position, out closestHit, 500, 1))
                    {
                        agent.Warp(closestHit.position);
                        agent.enabled = true;
                    }
                }
                else
                {
                    group_characters[i].transform.position = position;
                }
            }
        }

        /// <summary>
        /// We move the body of the character along the path passed to us, point by point
        /// </summary>
        public void ChangeCharacterBody(ent character, Queue<Vector3> path)
        {
            characterCurrentEnt = character;
            characterCurrent = characterCurrentEnt.transform.GetComponent<MonoMoveNavMeshInBattle>();

            if (characterCurrent != null)
            {
                characterCurrent.SetTransforms(path);
                characterCurrent.move = true;
                moveCharacter = true;
            }
            else
            {
                while (path.Count > 0)
                {
                    Vector3 temp = path.Dequeue();
                    if (path.Count == 0)
                    {
                        // move character to last position
                        character.transform.position = temp;
                        ProcessingBattleAnimation.RemoveAnimationMove();
                    }
                }
            }
        }

        public void Tick(float delta)
        {
            if (moveCharacter)
            {
                if (characterCurrent == null)
                    return;
                if (!characterCurrent.agent.isOnNavMesh)
                    return;

                if (characterCurrent != null)
                {
                    if (characterCurrent.TakeLastPosition(delta))
                    {
                        ProcessingBattleAnimation.RemoveAnimationMove();
                        characterCurrent = null;
                        characterCurrentEnt = -1;
                        moveCharacter = false;
                    }
                }
            }
        }

        protected override void OnDispose()
        {
            group_characters = null;
            ProcessingBattle.battleCharacterMove -= CharacterMove;
        }

        public void CharacterMove(ent caster, ent oldPosition, ent newPosition)
        {
            caster.ComponentStandingOn().entity = newPosition;
            ChangeCharacterBody(caster, LayerBattle.Get<ProcessingBattleComponentField>().GetPath(newPosition));
        }
    }
}
