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
        public const float MinDifference = 0.00001f;

        public static Harmony harmonyInstance;
        static ExtraPowerInfoMod()
        {
            harmonyInstance = new Harmony("arl85.MorePowerInfo");
            harmonyInstance.PatchAll();
        }

        [HarmonyPatch(typeof(CompPower), "CompInspectStringExtra")]
        public static class CompPower_CompInspectStringExtra
        {
            static void Postfix(ref string __result, CompPower __instance)
            {

                PowerNet powerNet = __instance.PowerNet;
                if (powerNet != null)
                {
                    float energyStored = powerNet.CurrentStoredEnergy();

                    float energyUsageTick = 0f;
                    float batterySelfDischargeUsageTick = 0f;
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
                    energyBalanceTick = Math.Abs(energyProductionTick - energyUsageTick) < MinDifference ? 0 : energyProductionTick - energyUsageTick; //dirty trick to avoid floating points rounding errors


                    Utility.LogEveryNTicks("energyProductionTick: " + energyProductionTick);
                    Utility.LogEveryNTicks("energyUsageTick: " + energyUsageTick);
                    Utility.LogEveryNTicks("energyBalanceTick: " + energyBalanceTick);
                    Utility.LogEveryNTicks("energyStored: " + energyStored);

                    int currentAutonomyTicks = 0;
                    int storageOnlyAutonomyTicks = 0;

                    string currentAutonomyStr = string.Empty;
                    string storageOnlyAutonomyStr = string.Empty;

                    if (energyStored > 0) /* no power stored --> 0 autonomy */
                    {
                        for (int i = 0; i < powerNet.batteryComps.Count; i++) //self-discharging batteries
                        {
                            if (powerNet.batteryComps[i].StoredEnergy > 0)
                            {
                                batterySelfDischargeUsageTick += Mathf.Min(5f * CompPower.WattsToWattDaysPerTick, powerNet.batteryComps[i].StoredEnergy);

                            }
                        }

                        Utility.LogEveryNTicks("batterySelfDischargeUsageTick: " + batterySelfDischargeUsageTick);

                        //battery flow => (half the EXTRA energy OR negative energy) - 5Wd per battery
                        float batteryFlowTick = (energyBalanceTick >= 0
                                            ? energyBalanceTick / 2  //maybe use CompProperties_Battery Props.efficiency (0.5) instead of hardcoding it here
                                            : energyBalanceTick)
                                            - batterySelfDischargeUsageTick;

                        if (Math.Abs(batteryFlowTick) < MinDifference) batteryFlowTick = 0;

                        Utility.LogEveryNTicks("batteryFlow: " + batteryFlowTick);

                        if (batteryFlowTick >= 0) /* positive battery flow -> infinite autonomy */
                        {
                            currentAutonomyStr = "MorePowerInfo.Forever".Translate();
                        }
                        else
                        {
                            currentAutonomyTicks = (int)(energyStored / -batteryFlowTick);
                        }
                        // all energy usage + the self discharge energy of batteries
                        storageOnlyAutonomyTicks = (int)(energyStored / (energyUsageTick + batterySelfDischargeUsageTick));

                    }

                    /* string conversion */
                    if (currentAutonomyStr == string.Empty) currentAutonomyStr = currentAutonomyTicks.ToStringTicksToPeriod();
                    if (storageOnlyAutonomyStr == string.Empty) storageOnlyAutonomyStr = storageOnlyAutonomyTicks.ToStringTicksToPeriod();

                    string energyStoredStr = energyStored.ToString("F0");
                    string energyProductionStr = (energyProductionTick / CompPower.WattsToWattDaysPerTick).ToString("F0");
                    string energyUsageStr = (energyUsageTick / CompPower.WattsToWattDaysPerTick).ToString("F0");
                    string energyBalanceStr = (energyBalanceTick / CompPower.WattsToWattDaysPerTick).ToString("F0");


                    __result = "MorePowerInfo.Production".Translate(energyProductionStr, energyUsageStr, energyBalanceStr) + "\n" +
                               "MorePowerInfo.Storage".Translate(energyStoredStr, currentAutonomyStr) + "\n" +
                               "MorePowerInfo.BatteryOnly".Translate(storageOnlyAutonomyStr);

                    Utility.LogEveryNTicks("----");

                }

            }
        }

    }

}

static class Utility
{
    public static void LogEveryNTicks(string msg, int N = 60)
    {
#if DEBUG
        if (GenTicks.TicksGame % N == 0) Log.Message(msg);
#endif
    }
}