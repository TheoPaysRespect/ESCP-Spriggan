﻿using RimWorld;
using Verse;
using System;
using System.Collections.Generic;

namespace ESCP_Spriggan
{
    class Comp_SpawnSprigganOnChop : ThingComp
    {
		public CompProperties_SpawnSprigganOnChop Props
		{
			get
			{
				return (CompProperties_SpawnSprigganOnChop)props;
			}
		}

        public bool takingAgeDamage = false;

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            takingAgeDamage = dinfo.Def == DamageDefOf.Rotting || dinfo.Def == DamageDefOf.Deterioration || (dinfo.Def == DamageDefOf.Flame && !ESCP_Spriggan_ModSettings.FireAttackChance);
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            takingAgeDamage = dinfo.Def == DamageDefOf.Rotting || dinfo.Def == DamageDefOf.Deterioration || (dinfo.Def == DamageDefOf.Flame && !ESCP_Spriggan_ModSettings.FireAttackChance);
            base.PostPreApplyDamage(ref dinfo, out absorbed);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (!takingAgeDamage && GenDate.DaysPassedSinceSettle > 1)
            {
                if (ESCP_Spriggan_ModSettings.ToxicAttackChance && previousMap.gameConditionManager.ActiveConditions.Any((GameCondition x) => x.def.conditionClass == typeof(GameCondition_ToxicFallout)))
                {
                    return;
                }
                //check smol attack
                if (ESCP_Spriggan_ModSettings.EnableChopAttack)
                {
                    Plant p = parent as Plant;
                    if(!ESCP_Spriggan_ModSettings.SownAttackChance && p.sown)
                    {
                        return;
                    }
                    if (Props.overrideList != null)
                    {
                        foreach (Overrides ov in Props.overrideList)
                        {
                            if (parent.def.ToString() == ov.treeDefName)
                            {
                                if (Rand.Chance(ov.overrideChance ? ov.overriddenChance : ESCP_Spriggan_ModSettings.EnableChopChance))
                                {
                                    SpawnAngrySpriggan(previousMap, parent, PawnKindDef.Named(ov.kindDefName));
                                    base.PostDestroy(mode, previousMap);
                                    return;
                                }
                                break;
                            }
                        }
                    }
                    if (Rand.Chance(ESCP_Spriggan_ModSettings.EnableChopChance))
                    {
                        SpawnAngrySpriggan(previousMap, parent);
                        base.PostDestroy(mode, previousMap);
                        return;
                    }
                }

                if (ESCP_Spriggan_ModSettings.EnableAttackChance && (!ESCP_Spriggan_ModSettings.RaidsCooldown || MapComponent_SprigganAttackTracker.CooldownReady()))
                {
                    MapComponent_SprigganAttackTracker.IncreaseChance();
                    if (Rand.Chance(MapComponent_SprigganAttackTracker.GetChance()))
                    {
                        TriggerAttack(previousMap);
                        if (ESCP_Spriggan_ModSettings.ResetAttackChance)
                        {
                            MapComponent_SprigganAttackTracker.ResetChance();
                        }
                        MapComponent_SprigganAttackTracker.ResetRaidCooldown();
                    }
                }
               
            }

            base.PostDestroy(mode, previousMap);
        }


        //for big boy attack
        public static void TriggerAttack(Map map)
        {
            //Log.Message("Attack triggered on map: " + map);
            IncidentDef def = IncidentDefOf.ESCP_Spriggan_SprigganAttack;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            parms.forced = true;
            def.Worker.TryExecute(parms);
        }

        //for smol attack
        public static void SpawnAngrySpriggan(Map map, Thing parent)
        {
            SpawnAngrySpriggan(map, parent, SprigganUtility.GetSprigganType(map));
        }

        public static void SpawnAngrySpriggan(Map map, Thing parent, PawnKindDef kindDef)
        {
            if(kindDef != null)
            {
                int avg = (int)kindDef.wildGroupSize.Average;   //basically just gets the wild group size, spawns that number in. This does mean you pretty much always want wild group size to be 1
                List<Pawn> pawns = new List<Pawn> { }; 
                for (int i = 0; i < avg; i++)
                {
                    Pawn newP = PawnGenerator.GeneratePawn(kindDef, null);
                    GenSpawn.Spawn(newP, parent.Position, map);
                    newP.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                    pawns.Add(newP);
                }
                if (!pawns.NullOrEmpty())
                {
                    Find.LetterStack.ReceiveLetter("ESCP_Spriggan_SprigganChopAttack_Label".Translate(), "ESCP_Spriggan_SprigganChopAttack_Description".Translate(), LetterDefOf.ThreatBig, pawns);

                }
            }
        }
    }
}
