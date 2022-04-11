using Pixeye.Actors;
using System;
using UnityEngine;

namespace Drebot
{
    public sealed class ProcessingBattleCharacterRotation : Processor
    {
        public void Rotate(ent entityCaster, ent entityTarget)
        {
            if (entityCaster == entityTarget)
                return;

            float x, y, res = 0;
            x = entityTarget.transform.position.x - entityCaster.transform.position.x;
            y = entityTarget.transform.position.z - entityCaster.transform.position.z;
            if (x < 0)
                res = 90f - (180f + (float)((Math.Atan(y / x) * 180) / Math.PI));
            else
                res = 90f - (float)((Math.Atan(y / x) * 180) / Math.PI);
            entityCaster.transform.eulerAngles = new Vector3(0f, res, 0f);
        }
    }
}
