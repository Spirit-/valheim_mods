﻿using BepInEx;
using HarmonyLib;
using Pipakin.SkillInjectorMod;
using System;
using UnityEngine;

namespace SailingSkill
{

    [BepInPlugin("gaijinx.mod.sailing_skill", "SailingSkill", "1.0.2")]
    [BepInDependency("pfhoenix.modconfigenforcer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.pipakin.SkillInjectorMod")]
    public class SailingSkillsPlugin : BaseUnityPlugin
    {
        public const String MOD_ID = "gaijinx.mod.sailing_skill";
        public const int SKILL_TYPE = 1339;

        private static SailingConfig sailingConfig = new SailingConfig();

        void Awake()
        {

            sailingConfig.InitConfig(MOD_ID, Config);

            var harmony = new Harmony(MOD_ID);
            harmony.PatchAll();

            SkillInjector.RegisterNewSkill(SKILL_TYPE, "Sailing", "Describes sailing ability", 1.0f, null, Skills.SkillType.Run);
        }

        public static bool IsPlayerControlling(Ship ship)
        {
            return ship.HaveControllingPlayer() && ship.m_shipControlls.IsLocalUser();
        }


        public static float GetSkillFactorMultiplier(float max)
        {
            return 1.0f + (Player.m_localPlayer.GetSkillFactor((Skills.SkillType)SKILL_TYPE) * max);
        }

        [HarmonyPatch(typeof(Ship), "GetSailForce")]
        static class GetSailForce_Patch
        {
            static void Postfix(Ship __instance, ref Vector3 __result)
            {
                if(IsPlayerControlling(__instance))
                {
                    float degrees = Vector3.Angle(EnvMan.instance.GetWindDir(), __instance.transform.forward);
                    if (degrees < 135f)
                    {
                        __result *= GetSkillFactorMultiplier(sailingConfig.MaxTailwindBoost);  // Maximum tailwind speed boost, up to 50%
                    }
                    else
                    {
                        __result *= GetSkillFactorMultiplier(sailingConfig.MaxForewindDampener);  // Maximum forewind speed dampening, up to -50%
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WearNTear), "Damage")]
        static class Damage_Patch
        {
            static void Prefix(WearNTear __instance, ref HitData hit)
            {
                if (__instance.gameObject.GetComponent<Ship>() == null)
                    return;
                if (IsPlayerControlling(__instance.gameObject.GetComponent<Ship>()))
                {
                    MultiplyDamage(ref hit, GetSkillFactorMultiplier(sailingConfig.MaxDamageReduction));  // up to 50% dmg reduction

                }
            }
        }

        private static void MultiplyDamage(ref HitData hit, float value)
        {
            value = Math.Max(0, value);
            hit.m_damage.m_damage *= value;
            hit.m_damage.m_blunt *= value;
            hit.m_damage.m_slash *= value;
            hit.m_damage.m_pierce *= value;
            hit.m_damage.m_chop *= value;
            hit.m_damage.m_pickaxe *= value;
            hit.m_damage.m_fire *= value;
            hit.m_damage.m_frost *= value;
            hit.m_damage.m_lightning *= value;
            hit.m_damage.m_poison *= value;
            hit.m_damage.m_spirit *= value;
        }


        [HarmonyPatch(typeof(Ship), "FixedUpdate")]
        public static class FixedUpdate_Patch
        {
            private static void Postfix(ref Ship __instance)
            {

                if (IsPlayerControlling(__instance)) {
                    Ship.Speed shipSpeed = __instance.GetSpeedSetting();
                    if (shipSpeed != Ship.Speed.Stop)
                    {
                        Player.m_localPlayer.RaiseSkill((Skills.SkillType) SKILL_TYPE, sailingConfig.SkillIncrease);
                    }
                }
            }
        }
    }

}
