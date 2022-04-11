using Pixeye.Actors;
using UnityEngine;

namespace Drebot
{
    public class ProcessingBattleEnd : Processor
    {
        public void Initialize(ScriptBattleEndLogicBase script)
        {
            this.script = script;
        }

        ScriptBattleEndLogicBase script;

        public int CheckEndOfBattle()
        {
            return script.CheckEndOfBattle();
        }
    }
}
