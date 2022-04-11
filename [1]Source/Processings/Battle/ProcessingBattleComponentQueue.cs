using Pixeye.Actors;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Drebot
{
    public class ProcessingBattleComponentQueue : Processor, IReceive<SignalBattleQueueCreated>
    {
        Group<ComponentCharacter> characters;

        private int charactersCount = -1;

        private Transform queueParent;
        private Object prefab;
        private LinkedList<ent> queue;


        #region INITIALIZE

        public void Initialize(int charactersCount)
        {
            this.charactersCount = charactersCount;
            LayerBattle.Run(WaitForAllCharacters());

            queue = new LinkedList<ent>();
        }

        private IEnumerator WaitForAllCharacters()
        {
            while (characters.length != charactersCount)
                yield return new WaitForFixedUpdate();

            LayerBattle.Get<ProcessingBattleStatusLoading>().CheckLoad(true, typeof(ProcessingBattleComponentQueue));
        }

        #endregion

        #region FINILIZE

        protected void Clear()
        {
            charactersCount = -1;
            if (queue != null)
            {
                foreach (var item in queue)
                {
                    item.Release();
                }
                queue.Clear();
            }
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

        #region BATTLE

        public void HandleSignal(in SignalBattleQueueCreated arg)
        {
            UIManager.Instance.UpdateQueue(arg.queue);
        }

        #endregion

    }
}