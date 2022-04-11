using Pixeye.Actors;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Drebot
{
    public class ProcessingBattleLoading : Processor
    {
        public void Initialize(ComponentEnemyGroup enemyGroup, ScriptBattleEndLogicBase battleEndLogic)
        {
            foreach (var item in enemyGroup.enemies)
            {
                LoadCharacter(item.enemy.ToString("g"), item.side);
            }

            AddCharactersOfGroup();

            int charactersCount = enemyGroup.enemies.Count + 1;//DataGameGroupCharacters.GetActiveGroup().Count;

            LayerBattle.Get<ProcessingBattleComponentCharacters>().Initialize(charactersCount);
            LayerBattle.Get<ProcessingBattleComponentATB>().Initialize(charactersCount);
            LayerBattle.Get<ProcessingBattleComponentQueue>().Initialize(charactersCount);
            LayerBattle.Get<ProcessingBattleComponentAssotiateCharactersAndPositions>().Initialize(charactersCount);
            LayerBattle.Get<ProcessingBattleComponentField>().Initialize();
            LayerBattle.Get<ProcessingBattleComponentAbilities>().Initialize(charactersCount);
            LayerBattle.Get<ProcessingBattleCharacterMoveNavMesh>().Initialize(charactersCount);
            LayerBattle.Get<ProcessingBattleEnd>().Initialize(battleEndLogic);
        }

        private void LoadCharacter(string name, Side side)
        {
            ref var entity = ref Actor.Create(Path.Combine("Prefabs", "Battle", "Characters", name)).entity;
            entity.transform.SetParent(LayerBattle.Instance.dynamic);

            entity.Set<ComponentCharacter>();
            entity.Set<ComponentEffects>();
            entity.ComponentSide().side = side;
            entity.Set(Tag.CharacterInBattle);

            if (side == Side.Left)
                entity.transform.rotation = Quaternion.Euler(0, 90, 0);
            else if (side == Side.Right)
                entity.transform.rotation = Quaternion.Euler(0, -90, 0);
        }

        private void AddCharactersOfGroup()
        {
            if (DataGameGroupCharacters.charactersOfGroup != null && DataGameGroupCharacters.charactersOfGroup.Count > 0)
            {
                foreach (var character in DataGameGroupCharacters.charactersOfGroup)
                {
                    ref var entity = ref Actor.Create(Path.Combine("Prefabs", "Battle", "Characters", character.nameCharacter)).entity;
                    entity.transform.SetParent(LayerBattle.Instance.dynamic);

                    entity.ComponentHealth().SetValuesComponentHealth(character.GetComponentHealth());
                    entity.ComponentAbilitiesActive().blueprints = character.GetComponentAbilitiesActive().blueprints;
                    entity.ComponentWeapon() = character.GetComponentWeapon();

                    entity.Set<ComponentCharacter>();
                    entity.Set<ComponentEffects>();
                    entity.ComponentSide().side = Side.Left;

                    entity.Set(Tag.CharacterInBattle);
                    entity.transform.rotation = Quaternion.Euler(0, 90, 0);
                }
            }
            else
            {
                if (DataGameGroupCharacters.defaultCharactersOfGroup.Count == 0)
                {
                    LayerLocation.Get<ProcessingPlayerGroup>().LoadDefaultGroup();
                }

                foreach (var character in DataGameGroupCharacters.defaultCharactersOfGroup)
                {
                    ref var entity = ref Actor.Create(Path.Combine("Prefabs", "Battle", "Characters", character.nameCharacter)).entity;
                    entity.transform.SetParent(LayerBattle.Instance.dynamic);

                    entity.Set<ComponentCharacter>();
                    entity.Set<ComponentEffects>();
                    entity.ComponentSide().side = Side.Left;

                    entity.Set(Tag.CharacterInBattle);
                    entity.transform.rotation = Quaternion.Euler(0, 90, 0);
                }
            }
        }
    }
}