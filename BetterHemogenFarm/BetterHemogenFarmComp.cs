﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BetterHemogenFarm
{
    public class BetterHemoFarmComp : ThingComp
    {
        private Pawn Pawn => (Pawn)this.parent;
        private bool shouldFarmHemogen;
        const string defaultGizmoHoverText = "Automatically place the 'Extract Hemogen Pack' bill on this pawn whenever they meet the following conditions:\n\n- Pawn Already Resting\n- Rest Need Below 40%\n- No Blood Loss condition\n\nIf the bill is not completed by 60% rest, it will be removed, try again the next night. This ensures we only take it when pawns can sleep off the worst of it.";
        const string ignoreRestConditionGizmoHoverText = "Automatically place the 'Extract Hemogen Pack' bill on this pawn whenever they meet the following conditions:\n\n- Pawn Already Resting\n- No Blood Loss condition";

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref shouldFarmHemogen, "shouldFarmHemogen", false);
        }

        public override void CompTick()
        {
            Pawn pawn = Pawn;
            if (shouldFarmHemogen && ModsConfig.BiotechActive && pawn.Spawned && pawn.IsHashIntervalTick(750))
            {
                Need rest = pawn.needs.rest;
                if (rest != null
                    && (rest.CurLevel <= 0.4f || BetterHemoFarmSettings.settings.ignoreRestCondition)
                    && rest.GUIChangeArrow > 0
                    && !pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss)
                    && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) > 0.41f
                    && pawn.BillStack != null
                    && !pawn.BillStack.Bills.Any((Bill x) => x.recipe == RecipeDefOf.ExtractHemogenPack)
                    && RecipeDefOf.ExtractHemogenPack.Worker.AvailableOnNow(pawn))
                {
                    HealthCardUtility.CreateSurgeryBill(pawn, RecipeDefOf.ExtractHemogenPack, null, null, sendMessages: false);
                }
                else if (pawn.health.hediffSet.HasHediff(HediffDefOf.BloodLoss)
                         || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) <= 0.41f
                         || (rest != null && (rest.GUIChangeArrow <= 0 || rest.CurLevel >= 0.6f && !BetterHemoFarmSettings.settings.ignoreRestCondition))
                        )
                {
                    List<Bill> billsToRemove = new List<Bill>();
                    foreach (Bill b in pawn.BillStack.Bills)
                    {
                        if (b.recipe == RecipeDefOf.ExtractHemogenPack)
                        {
                            billsToRemove.Add(b);
                        }
                    }
                    foreach (Bill b in billsToRemove)
                    {
                        b.billStack.Delete(b);
                    }
                }
            }
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref shouldFarmHemogen, "shouldFarmHemogen");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            Pawn pawn = Pawn;
            if (RecipeDefOf.ExtractHemogenPack.Worker.AvailableOnNow(pawn) && (pawn.IsColonist || pawn.IsPrisonerOfColony))
            {
                Command_Toggle command_Toggle2 = new Command_Toggle();
                command_Toggle2.defaultLabel = "Automatically Extract Hemogen";
                command_Toggle2.defaultDesc = (BetterHemoFarmSettings.settings.ignoreRestCondition ? ignoreRestConditionGizmoHoverText : defaultGizmoHoverText);
                command_Toggle2.hotKey = null;
                command_Toggle2.icon = DefDatabase<ThingDef>.GetNamed("HemogenPack").uiIcon;
                command_Toggle2.isActive = (() => shouldFarmHemogen);
                command_Toggle2.toggleAction = delegate
                {
                    shouldFarmHemogen = !shouldFarmHemogen;
                };
                yield return command_Toggle2;
            }
        }
    }

    public class BetterHemoFarmMod : Mod
    {

        public readonly BetterHemoFarmSettings settings;

        public BetterHemoFarmMod(ModContentPack content) : base(content)
        {
            this.settings = this.GetSettings<BetterHemoFarmSettings>();
            BetterHemoFarmSettings.settings = this.settings;
        }

        public override string SettingsCategory() => "Better Hemogen Farm (Continued)";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Ignore Rest Condition", ref this.settings.ignoreRestCondition, "If checked, the rest condition will be ignored when deciding to extract hemogen.");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
    }

    public class BetterHemoFarmSettings : ModSettings
    {
        public static BetterHemoFarmSettings settings;
        public bool ignoreRestCondition = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ignoreRestCondition, "ignoreRestCondition", false);
        }
    }
}