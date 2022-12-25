using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Reflection;
using HarmonyLib;
using System;

namespace ExtraPowerInfo
{
    [StaticConstructorOnStartup]
    public static class ExtraPowerInfoMod
    {
        public static Harmony harmonyInstance;
        static ExtraPowerInfoMod()
        {
            harmonyInstance = new Harmony("arl85.MorePowerInfo");
            harmonyInstance.PatchAll();
        }

        [HarmonyPatch(typeof(CompPower), "CompInspectStringExtra")]
        public static class CompPower_CompInspectStringExtra
        {
            static void Postfix(ref string __result, CompPower __instance, float ___WattsToWattDaysPerTick)
            {

                PowerNet powerNet = __instance.PowerNet;
                if (powerNet != null)
                {
                    float energyStored = powerNet.CurrentStoredEnergy();

                    float energyUsageTick = 0f;
                    float energyProductionTick = 0f;
                    float energyBalanceTick;

                    for (int i = 0; i < powerNet.powerComps.Count; i++)
                    {
                        if (powerNet.powerComps[i].PowerOn && powerNet.powerComps[i].EnergyOutputPerTick != 0f)
                        {

                            if (powerNet.powerComps[i].EnergyOutputPerTick < 0f)
                            {
                                energyUsageTick -= powerNet.powerComps[i].EnergyOutputPerTick;
                            }
                            else
                            {
                                energyProductionTick += powerNet.powerComps[i].EnergyOutputPerTick;
                            }
                        }
                    }
                    energyBalanceTick = energyProductionTick - energyUsageTick;


                    if (Math.Abs(energyBalanceTick) < 0.00001) energyBalanceTick = 0; //dirty trick to avoid floating points rounding errors
#if DEBUG
                    Log.Message("ept: " + energyProductionTick + " eut: " + energyUsageTick + " ebt: " + energyBalanceTick + "es: " + energyStored);
#endif
                    int currentAutonomyTicks = 0;
                    int storageOnlyAutonomyTicks = 0;

                    string currentAutonomyStr = string.Empty;
                    string storageOnlyAutonomyStr = string.Empty;

                    if (energyStored > 0) /* no power stored --> 0 autonomy */
                    {

                        if (energyBalanceTick >= 0) /* positive energy balance -> infinite autonomy */
                        {
                            currentAutonomyStr = "MorePowerInfo.Forever".Translate();
                        }
                        else
                        {
                            currentAutonomyTicks = (int)(energyStored / -energyBalanceTick);
                        }

                        if (energyUsageTick == 0) /* no energy usage (ie: disconnected battery) -> infinite autonomy */
                        {
                            storageOnlyAutonomyStr = "MorePowerInfo.Forever".Translate();
                        }
                        else
                        {
                            storageOnlyAutonomyTicks = (int)(energyStored / energyUsageTick);
                        }

                    }

                    /* string conversion */
                    if (currentAutonomyStr == string.Empty) currentAutonomyStr = currentAutonomyTicks.ToStringTicksToPeriod();
                    if (storageOnlyAutonomyStr == string.Empty)  storageOnlyAutonomyStr = storageOnlyAutonomyTicks.ToStringTicksToPeriod();
                    string energyStoredStr = energyStored.ToString("F0");
                    string energyProductionStr = (energyProductionTick / ___WattsToWattDaysPerTick).ToString("F0");
                    string energyUsageStr = (energyUsageTick / ___WattsToWattDaysPerTick).ToString("F0");
                    string energyBalanceStr = (energyBalanceTick / ___WattsToWattDaysPerTick).ToString("F0");


                    __result = "MorePowerInfo.Production".Translate(energyProductionStr, energyUsageStr, energyBalanceStr) + "\n" +
                               //"MorePowerInfo.Storage".Translate(energyStoredStr, currentAutonomy, worstCaseAutonomy);
                               "MorePowerInfo.Storage".Translate(energyStoredStr, currentAutonomyStr) + "\n" +
                               "MorePowerInfo.BatteryOnly".Translate(storageOnlyAutonomyStr);


                }

            }
        }

    }

}

