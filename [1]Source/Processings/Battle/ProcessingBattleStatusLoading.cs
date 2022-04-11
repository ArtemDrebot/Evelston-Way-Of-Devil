using Pixeye.Actors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Drebot
{
    public class ProcessingBattleStatusLoading : Processor
    {
        private Dictionary<Type, bool> loadingChecklist;

        protected override void OnDispose()
        {
            if (loadingChecklist != null)
                loadingChecklist.Clear();
        }

        public void CheckLoad(bool result, Type type)
        {

            if (loadingChecklist.Count == 0)
            {
                DrebotDebug.DebugError("Can't find BattleComponents! Count = 0 ");
                BreakLoadingBattle();
                return;
            }
            if (result == false)
            {
                DrebotDebug.DebugError("Can't load BattleComponent with type " + type);
                BreakLoadingBattle();
                return;
            }
            loadingChecklist[type] = result;

            Debug.LogError("-----------------------------------------------");
            Debug.LogError(string.Format("<color='blue'>Current load -> {0}</color>", type));
            foreach (var item in loadingChecklist)
            {
                if (item.Value == true)
                    Debug.LogError(string.Format("<color='green'>{0}</color>", item.Key + " " + item.Value));
                else
                    Debug.LogError(string.Format("<color='red'>{0}</color>", item.Key + " " + item.Value));
            }
            Debug.LogError("-----------------------------------------------");

            if (loadingChecklist.Any(x => x.Value == false))
                return;

            LayerBattle.Get<ProcessingBattle>().Initialize();
        }

        private void BreakLoadingBattle()
        {
            DrebotDebug.DebugError("BreakLoadingBattle does not realized");
            OnDispose();
        }

        public ProcessingBattleStatusLoading()
        {
            loadingChecklist = new Dictionary<Type, bool>()
            {
                {typeof(ProcessingBattleComponentCharacters), false },
                {typeof(ProcessingBattleComponentQueue), false },
                {typeof(ProcessingBattleComponentField), false },
                {typeof(ProcessingBattleComponentAbilities), false },
                {typeof(ProcessingBattleComponentATB), false },
                {typeof(ProcessingBattleComponentAssotiateCharactersAndPositions), false },
                {typeof(ProcessingBattleCharacterMoveNavMesh), false }
            };
        }
    }
}