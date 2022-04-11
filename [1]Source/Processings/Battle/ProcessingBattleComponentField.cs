using Pixeye.Actors;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Drebot
{
    public class ProcessingBattleComponentField : Processor,
        IReceive<SignalBattleChangeCurrentAbilityContainer>,
        IReceive<SignalBattleAbilityButtonMouseEnter>,
        IReceive<SignalBattleAbilityButtonMouseExit>
    {
        Group<ComponentPosition> positions;

        private int positionsCount = -1;

        #region INITIALIZE

        public void Initialize()
        {
            Clear();
            this.positionsCount = LayerBattle.Instance.battleFieldCounter.GetPositionsCount();
            LayerBattle.Run(WaitForAllPosition());

            ProcessingBattle.playerAction += PlayerAction;
            ProcessingBattle.clickableAll += ClickableAll;
            ProcessingBattle.battleCharacterClick += CharacterClick;
            ProcessingBattle.battlePositionClick += PositionClick;
            ProcessingBattleComponentATB.changeCharacter += ChangeCurrentCharacter;
        }

        private IEnumerator WaitForAllPosition()
        {
            while (positions.length != positionsCount)
                yield return new WaitForFixedUpdate();

            LayerBattle.Get<ProcessingBattleStatusLoading>().CheckLoad(true, typeof(ProcessingBattleComponentField));
        }

        #endregion

        public ent[] GetPositions(out int lenght)
        {
            lenght = positions.length;
            return positions.entities;
        }

        #region FINILIZE

        protected void Clear()
        {
            positionsCount = -1;

            ProcessingBattle.playerAction -= PlayerAction;
            ProcessingBattle.clickableAll -= ClickableAll;
            ProcessingBattle.battleCharacterClick -= CharacterClick;
            ProcessingBattle.battlePositionClick -= PositionClick;
            ProcessingBattleComponentATB.changeCharacter -= ChangeCurrentCharacter;
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

        #region CLICKABLE

        public void ClickableAll(bool toogle)
        {
            if (toogle == true)
            {
                foreach (var entity in positions)
                {
                    if (entity.ComponentUpstanding().entity < 0)
                        entity.ComponentClickable().value = true;
                    else
                        entity.ComponentClickable().value = false;
                }
            }
            else
            {
                foreach (var entity in positions)
                {
                    entity.ComponentClickable().value = false;
                }
            }
        }

        private void UpdateAbilityClickArea(BlueprintAbilityActive ability)
        {
            ClickableAll(false);
            ent currentEntity = ProcessingBattle.CurrentCharacterEntity;

            if (ability.castTarget == CastTarget.Position || ability.castTarget == CastTarget.Both)
                ClickableInRange(currentEntity.ComponentStandingOn().entity, currentEntity.ComponentSpeed().currentValue);
        }

        public Queue<Vector3> GetPath(ent end)
        {
            Queue<Vector3> path = new Queue<Vector3>();
            path.Enqueue(end.ComponentCharacterPositionPivot().positionPivot.position);

            ent currentEntity = end;

            int i = 0;
            int newValue;
            if (end.ComponentPositionRangeTo().value == 1)
            {
                newValue = 0;
            }
            else
            {
                newValue = currentEntity.ComponentMoveToSpeedCost().value;
            }
            while (newValue > 1)
            {
                i++;
                if (i >= 100)
                    return new Queue<Vector3>(end);

                ent tempEntyty = currentEntity;

                foreach (var item in currentEntity.ComponentPositionNeighbours().neighbourPositions)
                {
                    if (path.Contains(item.entity.ComponentCharacterPositionPivot().positionPivot.position))
                        continue;
                    if (item.entity.ComponentMoveToSpeedCost().deadEnd)
                        continue;

                    if (item.entity.ComponentPositionRangeTo().value == 1)
                    {
                        tempEntyty = item.entity;
                        newValue = 0;
                    }
                    else if (item.entity.ComponentMoveToSpeedCost().value < newValue)
                    {
                        tempEntyty = item.entity;
                        newValue = item.entity.ComponentMoveToSpeedCost().value;
                    }
                }
                currentEntity = tempEntyty;
                path.Enqueue(currentEntity.ComponentCharacterPositionPivot().positionPivot.position);
            }

            return new Queue<Vector3>(path.Reverse());
        }

        public void ClickableInRange(int start, int range)
        {
            foreach (var entity in positions)
            {
                if (entity.ComponentMoveToSpeedCost().value > 0 && entity.ComponentMoveToSpeedCost().value <= range)
                    entity.ComponentClickable().value = true;
                if (entity.ComponentUpstanding().entity >= 0)
                    entity.ComponentClickable().value = false;
                if (entity.ComponentMoveToSpeedCost().moveAanyway == true)
                    entity.ComponentClickable().value = true;
            }
        }

        #endregion

        #region IN ABILITY RANGE

        private void InAbilityRangeAll(bool toogle)
        {
            foreach (var entity in positions)
            {
                entity.ComponentInAbilityRange().value = toogle;
            }
        }

        private void UpdateAbilityOverArea(BlueprintAbilityActive ability)
        {
            InAbilityRangeAll(false);
            ent currentEntity = ProcessingBattle.CurrentCharacterEntity;
            FallsInAbilityRange(currentEntity.ComponentStandingOn().entity, ability);
        }

        public void FallsInAbilityRange(ent start, BlueprintAbilityActive ability)
        {
            if (ability.code != "SWAP")
            {
                foreach (var entity in positions)
                {
                    if (entity.ComponentPositionRangeTo().value > 0 && entity.ComponentPositionRangeTo().value <= ability.castRange)
                        entity.ComponentInAbilityRange().value = true;
                }
            }
            else
            {
                foreach (var entity in positions)
                {
                    if (entity.ComponentMoveToSpeedCost().value > 0 && entity.ComponentMoveToSpeedCost().value <= ability.castRange)
                        entity.ComponentInAbilityRange().value = true;
                    if (entity.ComponentMoveToSpeedCost().moveAanyway == true)
                        entity.ComponentInAbilityRange().value = true;
                }
            }
        }

        #endregion

        #region DEJKSTRA

        public void DejkstraSearch(ent start, int speed)
        {
            ClearPositionsInfo();
            DejkstraSearchRange(start);
            DejkstraSearchSpeed(start);
            DejkstraSearchEndurance(start, speed);
        }

        private void DejkstraSearchRange(ent start)
        {
            start.ComponentPositionRangeTo().value = 0;
            start.ComponentPositionCameFrom().entityRange = -1;

            Queue<ent> frontier = new Queue<ent>();
            frontier.Enqueue(start);

            ent currentEntity;
            ent nextEntity;
            int newValue;

            ComponentPositionRangeTo tMoveRange;
            while (frontier.Count > 0)
            {
                currentEntity = frontier.Dequeue();
                foreach (var item in currentEntity.ComponentPositionNeighbours().neighbourPositions)
                {
                    nextEntity = item.entity;

                    tMoveRange = nextEntity.ComponentPositionRangeTo();

                    newValue = currentEntity.ComponentPositionRangeTo().value + 1;
                    if (tMoveRange.value == -1 || newValue < tMoveRange.value)
                    {
                        tMoveRange.value = newValue;
                        frontier.Enqueue(nextEntity);
                        nextEntity.ComponentPositionCameFrom().entityRange = currentEntity;
                    }
                }
            }
        }

        /// <summary>
        /// FIRST path finding
        /// </summary>
        /// <param name="start"></param>
        private void DejkstraSearchSpeed(ent start)
        {
            start.ComponentPositionCameFrom().entitySpeed = -1;
            start.ComponentMoveToSpeedCost().value = 0;

            Queue<ent> frontier = new Queue<ent>();
            frontier.Enqueue(start);

            ent currentEntity;
            ent nextEntity;
            int newCost;

            ComponentMoveToSpeedCost tMoveToCost;
            while (frontier.Count > 0)
            {
                currentEntity = frontier.Dequeue();
                foreach (var item in currentEntity.ComponentPositionNeighbours().neighbourPositions)
                {
                    nextEntity = item.entity;

                    if (nextEntity.ComponentUpstanding().entity != -1)
                        nextEntity.ComponentMoveToSpeedCost().deadEnd = true;

                    tMoveToCost = nextEntity.ComponentMoveToSpeedCost();

                    newCost = currentEntity.ComponentMoveToSpeedCost().value + nextEntity.ComponentSpeedCost().Value;
                    if (tMoveToCost.value == -1 || newCost < tMoveToCost.value)
                    {
                        tMoveToCost.value = newCost;
                        if (nextEntity.ComponentMoveToSpeedCost().deadEnd == false)
                            frontier.Enqueue(nextEntity);
                        nextEntity.ComponentPositionCameFrom().entitySpeed = currentEntity;
                    }
                }
            }
            foreach (var item in start.ComponentPositionNeighbours().neighbourPositions)
            {
                if (item.entity.ComponentUpstanding().entity >= 0)
                    continue;

                if (ProcessingBattle.CurrentCharacterEntity.ComponentSpeed().currentValue > 0)
                    item.entity.ComponentMoveToSpeedCost().moveAanyway = true;
            }
        }

        /// <summary>
        /// SECOND path finding
        /// </summary>
        /// <param name="start"></param>
        private void DejkstraSearchEndurance(ent start, int range)
        {
            Queue<ent> frontier = new Queue<ent>();
            frontier.Enqueue(start);

            start.ComponentPositionCameFrom().entityEndurance = -1;
            start.ComponentMoveToEnduranceCost().value = 0;

            ent currentEntity;
            ent nextEntity;
            int newCostEndurance;
            ComponentMoveToEnduranceCost tMoveToCost;
            while (frontier.Count > 0)
            {
                currentEntity = frontier.Dequeue();
                foreach (var item in currentEntity.ComponentPositionNeighbours().neighbourPositions)
                {
                    nextEntity = item.entity;

                    if (nextEntity.ComponentUpstanding().entity != -1)
                        nextEntity.ComponentMoveToSpeedCost().deadEnd = true;

                    tMoveToCost = nextEntity.ComponentMoveToEnduranceCost();
                    newCostEndurance = currentEntity.ComponentMoveToEnduranceCost().value + nextEntity.ComponentEnduranceCost().Value;

                    if (tMoveToCost.value == -1 || newCostEndurance < tMoveToCost.value)
                    {
                        tMoveToCost.value = newCostEndurance;
                        if (nextEntity.ComponentMoveToSpeedCost().deadEnd == false)
                            frontier.Enqueue(nextEntity);
                        nextEntity.ComponentPositionCameFrom().entityEndurance = currentEntity;
                    }
                }
            }
        }

        private void ClearPositionsInfo()
        {
            foreach (var entity in positions)
            {
                entity.ComponentMoveToSpeedCost().deadEnd = false;
                entity.ComponentMoveToSpeedCost().moveAanyway = false;

                entity.ComponentPositionRangeTo().value = -1;
                entity.ComponentMoveToEnduranceCost().value = -1;
                entity.ComponentMoveToSpeedCost().value = -1;

                entity.ComponentPositionCameFrom().entityRange = -1;
                entity.ComponentPositionCameFrom().entitySpeed = -1;
                entity.ComponentPositionCameFrom().entityEndurance = -1;
            }
        }

        #endregion

        #region SIGNALS

        public void HandleSignal(in SignalBattleChangeCurrentAbilityContainer arg)
        {
            if (arg.abilityToPosition == null)
                return;

            if (arg.isAI == true)
                return;

            if (arg.abilityToPosition.GetAbility().isInstant == true)
                return;

            UpdateAbilityClickArea(arg.abilityToPosition.GetAbility());
        }
        
        public void HandleSignal(in SignalBattleAbilityButtonMouseEnter arg)
        {
            UpdateAbilityOverArea(arg.ability);
        }

        public void HandleSignal(in SignalBattleAbilityButtonMouseExit arg)
        {
            InAbilityRangeAll(false);
        }

        public void PlayerAction(ent entity)
        {
            DejkstraSearch(entity.ComponentStandingOn().entity, entity.ComponentSpeed().currentValue);
        }

        public void ChangeCurrentCharacter(ent entity)
        {
            DejkstraSearch(entity.ComponentStandingOn().entity, entity.ComponentSpeed().currentValue);
        }

        public void CharacterClick(ent entity)
        {
            ClickableAll(false);
        }

        public void PositionClick(ent entity)
        {
            ClickableAll(false);
        }
        #endregion
    }
}