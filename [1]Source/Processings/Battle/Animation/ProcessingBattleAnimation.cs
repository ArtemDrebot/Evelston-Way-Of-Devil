using Pixeye.Actors;
using System.Collections.Generic;
using UnityEngine;

namespace Drebot
{
    public enum TypeBattleAnimation { None, Move}

    public class AnimationBattleElement
    {
        public bool play = false;
        public float time = 0;
        public float startTime = 0;
        public TypeBattleAnimation typeAnimation;
    }

    public class ProcessingBattleAnimation : Processor, ITick
    {
        public static int animationsBattleCount = 0;
        public static bool muteAnimations;

        static List<AnimationBattleElement> orderAnimations = new List<AnimationBattleElement>();

        static Transform parent = GameObject.Find("Dynamic").transform;

        public static void AddMoraleAnimation(ent entity, bool typeMorale)
        {
            if (muteAnimations) return;
            animationsBattleCount++;

            float timeForMoraleAnimation = 3;

            AnimationBattleElement animationBattleElement = new AnimationBattleElement();
            animationBattleElement.time = timeForMoraleAnimation;
            orderAnimations.Add(animationBattleElement);


            GameObject effect;
            if (typeMorale)
                effect = LayerBattle.Actor.Create(DataBasePrefabs.battlePrefabs.goodMorale).gameObject;
            else
                effect = LayerBattle.Actor.Create(DataBasePrefabs.battlePrefabs.badMorale).gameObject;

            effect.transform.position = new Vector3(entity.transform.position.x, entity.transform.position.y + 2, entity.transform.position.z);
            effect.transform.SetParent(parent);
        }

        public static void AddEnduranceAnimation(ent entity)
        {
            if (muteAnimations) return;
            animationsBattleCount++;

            float timeForEnduranceAnimation = 3;

            AnimationBattleElement animationBattleElement = new AnimationBattleElement();
            animationBattleElement.time = timeForEnduranceAnimation;
            orderAnimations.Add(animationBattleElement);

            GameObject effect = LayerBattle.Actor.Create(DataBasePrefabs.battlePrefabs.badMorale).gameObject;

            effect.transform.position = new Vector3(entity.transform.position.x, entity.transform.position.y + 2, entity.transform.position.z);
            effect.transform.SetParent(parent);
        }

        public void Tick(float delta)
        {
            if (orderAnimations.Count > 0)
            {
                if (!orderAnimations[0].play)
                {
                    orderAnimations[0].play = true;
                    orderAnimations[0].startTime = Pixeye.Actors.Time.Current;
                    
                }
                if (orderAnimations[0].play)
                {
                    if ((Pixeye.Actors.Time.Current - orderAnimations[0].startTime) >= orderAnimations[0].time)
                    {
                        DecreaseAnimationNumber();
                        orderAnimations.RemoveAt(0);
                    }
                }
            }
        }

        public static void AddUseAbilityToCharacterAnimation(ent caster, ent target, float time)
        {
            if (muteAnimations) return;
            animationsBattleCount++;

            AnimationBattleElement animationBattleElement = new AnimationBattleElement();
            animationBattleElement.time = time;
            orderAnimations.Add(animationBattleElement);
        }

        public static void AddAnimationDeath(float time)
        {
            if (muteAnimations) return;
            animationsBattleCount++;

            AnimationBattleElement animationBattleElement = new AnimationBattleElement();
            animationBattleElement.time = time;
            orderAnimations.Add(animationBattleElement);
        }

        public static void IncreaseAnimationNumber()
        {
            if (muteAnimations) return;
            animationsBattleCount++;
        }

        public static void DecreaseAnimationNumber()
        {
            if (muteAnimations) return;
            animationsBattleCount--;

            if (animationsBattleCount < 0)
                animationsBattleCount = 0;
        }

        public static void AddAnimationMove()
        {
            if (muteAnimations) return;
            animationsBattleCount++;

            AnimationBattleElement animationBattleElement = new AnimationBattleElement();
            animationBattleElement.time = 20;
            animationBattleElement.typeAnimation = TypeBattleAnimation.Move;
            orderAnimations.Add(animationBattleElement);
        }

        public static void RemoveAnimationMove()
        {
            if (muteAnimations) return;
            animationsBattleCount--;

            var animation = orderAnimations.Find(x => x.typeAnimation == TypeBattleAnimation.Move);
            if (animation != null)
                orderAnimations.Remove(animation);
        }

        public static bool ReadyToContinue()
        {
            if (animationsBattleCount > 0)
                return false;

            return true;
        }
    }
}