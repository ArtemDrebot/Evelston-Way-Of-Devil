using Pixeye.Actors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Drebot
{
    public struct AtbPosition : IFormattable
    {
        private float _value;
        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value < 0)
                    _value = 0;
                else
                    _value = value.Round(1);
            }
        }

        public AtbPosition(float d)
        {
            _value = 0f;
            Value = d;
        }

        public static AtbPosition operator +(AtbPosition a, float b)
        {
            AtbPosition r = new AtbPosition();
            r.Value = a.Value + b;
            return r;
        }
        public static AtbPosition operator +(AtbPosition a, AtbPosition b)
        {
            float t = a.Value + b.Value;
            return new AtbPosition(t);
        }

        public static bool operator <(AtbPosition a, float d)
        {
            if (a.Value < d)
                return true;
            else return false;
        }
        public static bool operator >(AtbPosition a, float d)
        {
            if (a.Value > d)
                return true;
            else return false;
        }
        public static bool operator <=(AtbPosition a, float d)
        {
            if (a.Value <= d)
                return true;
            else return false;
        }
        public static bool operator >=(AtbPosition a, float d)
        {
            if (a.Value >= d)
                return true;
            else return false;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
        public string ToString(IFormatProvider provider) { return ToString(); }
        public string ToString(string format) { return ToString(); }
        public string ToString(string format, IFormatProvider formatProvider) { return ToString(); }
    }

    class AtbCharacter
    {
        public ent entity;
        public AtbPosition atbPosition;

        /// <summary>
        /// Calculates the next position on the discrete ATV scale
        /// </summary>
        /// <returns> true - if the final position is greater than or equal to 100, false - if the final position is less than 100</returns>
        public bool Tick()
        {
            if (entity.ComponentInitiative().currentValue < 1f)
                entity.ComponentInitiative().currentValue = 1f;

            atbPosition += entity.ComponentInitiative().currentValue;
            if (atbPosition >= 100f)
                return true;
            else
                return false;
        }
        public void Reset()
        {
            atbPosition.Value = 0f;
        }
    }

    public class ProcessingBattleComponentATB : Processor
    {
        Group<ComponentCharacter> characters;

        private ent _currentEntity = -1;
        public ent CurrentEntity
        {
            get
            {
                return _currentEntity;
            }
        }
        private List<ent> _atbQueue;
        private int _atbQueueLength = 6;
        private List<AtbCharacter> tempCharacterList;
        private int charactersCount = -1;

        public static battleStepEntityOne changeCharacter;

        #region INITIALIZE

        public ProcessingBattleComponentATB()
        {
            tempCharacterList = new List<AtbCharacter>();
        }

        public void Initialize(int charactersCount)
        {
            Clear();
            this.charactersCount = charactersCount;
            LayerBattle.Run(WaitForAllCharacters());
            ProcessingBattle.nextCharacter += NextCharacter;
        }

        /// <summary>
        /// Queue loading complete
        /// </summary>
        private void LoadComplete()
        {
            foreach (var entity in characters)
            {
                entity.ComponentATB().currentATB = UnityEngine.Random.Range(0f, 10f);
                tempCharacterList.Add(new AtbCharacter
                {
                    entity = entity,
                    atbPosition = new AtbPosition()
                });
            }

            _atbQueue = new List<ent>();
        }

        private IEnumerator WaitForAllCharacters()
        {
            while (characters.length != charactersCount)
                yield return new WaitForFixedUpdate();

            LoadComplete();
            LayerBattle.Get<ProcessingBattleStatusLoading>().CheckLoad(true, typeof(ProcessingBattleComponentATB));
        }

        #endregion

        #region FINILIZE

        protected void Clear()
        {
            ProcessingBattle.nextCharacter -= NextCharacter;
            charactersCount = -1;
            tempCharacterList.Clear();
        }

        protected override void OnDispose()
        {
            Clear();
        }

        #endregion

        /// <summary>
        /// Move to the next character
        /// </summary>
        public void NextCharacter()
        {
            if (_currentEntity != -1)
            {
                _currentEntity.Remove(Tag.CharacterCurrent);
            }

            CalculateAtbQueue();
            _currentEntity = _atbQueue.First();
            changeCharacter(_currentEntity);
            //new SignalBattleChangeCurrentCharacter(_currentEntity).SendGlobal();
            _currentEntity.Set(Tag.CharacterCurrent);
            //HelperTags.Add(_currentEntity, Tag.CharacterCurrent);
            // return 0;
        }

        private static int Compare(AtbCharacter x, AtbCharacter y)
        {
            if (x.atbPosition.Value >= y.atbPosition.Value)
                return -1;
            else
                return 1;
        }

        private void RememberAtb()
        {
            foreach (var item in tempCharacterList)
            {
                item.entity.ComponentATB().currentATB = item.atbPosition.Value;
            }
        }

        private void CalculateAtbQueue()
        {
            for (int i = 0; i < tempCharacterList.Count; i++)
            {
                tempCharacterList.ElementAt(i).atbPosition.Value = tempCharacterList.ElementAt(i).entity.ComponentATB().currentATB;
            }

            _atbQueue.Clear();
            bool flag1;
            bool flag2 = false;
            int iteration = 0;
            while (_atbQueue.Count != _atbQueueLength)
            {
                flag1 = false;
                while (flag1 == false)
                {
                    for (int i = 0; i < tempCharacterList.Count; i++)
                    {
                        flag1 |= tempCharacterList.ElementAt(i).Tick();
                    }
                }

                List<AtbCharacter> s = tempCharacterList.Where(a => a.atbPosition >= 100f).ToList();
                s.Sort(Compare);
                if (flag2 == false)
                {
                    RememberAtb();
                    flag2 = true;
                }
                foreach (var item in s)
                {
                    _atbQueue.Add(item.entity);
                    item.Reset();
                    if (_atbQueue.Count >= _atbQueueLength)
                        break;
                }

                iteration++;
                if (iteration > 100)
                {
                    DrebotDebug.DebugError("The number of iterations has exceeded the allowable values.");
                    return;
                }
            }

            var signal = new SignalBattleQueueCreated(_atbQueue);
            LayerBattle.Send(signal);
        }
    }
}