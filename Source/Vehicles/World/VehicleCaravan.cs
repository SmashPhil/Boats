﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    public class VehicleCaravan : Caravan
    {
        public VehicleCaravan() : base()
        {
            vPather = new VehicleCaravan_PathFollower(this);
            vTweener = new VehicleCaravan_Tweener(this);
        }

        //REDO : Implement custom caravan icons
        public override void Draw()
        {
            float averageTileSize = Find.WorldGrid.averageTileSize;
            float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;
            if (def.expandingIcon && transitionPct > 0f)
            {
                Color color = Material.color;
                float num = 1f - transitionPct;
                propertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(color.r, color.g, color.b, color.a * num));
                DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, 0.015f, Material, false, false, propertyBlock);
                return;
            }
            DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, 0.015f, Material, false, false, null);
        }

        public void DrawQuadTangentialToPlanet(Vector3 pos, float size, float altOffset, Material material, bool counterClockwise = false, bool useSkyboxLayer = false, MaterialPropertyBlock propertyBlock = null)
		{
			if (material == null)
			{
				Log.Warning("Tried to draw quad with null material.", false);
				return;
			}
			Vector3 normalized = pos.normalized;
			Vector3 vector;
			if (counterClockwise)
			{
				vector = -normalized;
			}
			else
			{
				vector = normalized;
			}
			Quaternion q = Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), vector) * Quaternion.Euler(0, -90f, 0);
			Vector3 s = new Vector3(size, 1f, size);
			Matrix4x4 matrix = default;
			matrix.SetTRS(pos + normalized * altOffset, q, s);
			int layer = useSkyboxLayer ? WorldCameraManager.WorldSkyboxLayer : WorldCameraManager.WorldLayer;
			if (propertyBlock != null)
			{
				Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer, null, 0, propertyBlock);
                //Graphics.DrawMesh(MeshPool.plane10, matrix, LeadVehicle.VehicleGraphic.MatAt(Rot4.West, LeadVehicle), layer, null, 0, propertyBlock);
				return;
			}
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer);
            //Graphics.DrawMesh(MeshPool.plane10, matrix, LeadVehicle.VehicleGraphic.MatAt(Rot4.West, LeadVehicle), layer);
            //if (LeadVehicle.GetCachedComp<CompCannons>() != null)
            //{
            //    foreach (CannonHandler cannon in LeadVehicle.GetCachedComp<CompCannons>().Cannons)
            //    {
            //        HelperMethods.DrawDefaultCannonMesh(cannon, pos, layer);
            //    }
            //}
		}

        private VehiclePawn leadVehicle;
        public VehiclePawn LeadVehicle
        {
            get
            {
                if (leadVehicle is null)
                    leadVehicle = PawnsListForReading.First(v => v is VehiclePawn) as VehiclePawn;
                return leadVehicle;
            }
        }

        public override Material Material
        {
            get
            {
                ThingDef leadVehicleDef = PawnsListForReading.First(v => v is VehiclePawn).def;
                
                if(!materials.ContainsKey(leadVehicleDef))
                {
                    var texture = VehicleTex.CachedTextureIcons[leadVehicleDef];
                    var material = MaterialPool.MatFrom(texture, ShaderDatabase.WorldOverlayTransparentLit, Color.white, WorldMaterials.WorldObjectRenderQueue);
                    materials.Add(leadVehicleDef, material);
                }
                return materials[leadVehicleDef];
            }
        }

        public override Vector3 DrawPos => vTweener.TweenedPos;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (Find.WorldSelector.SingleSelectedObject == this)
	        {
		        yield return new Gizmo_CaravanInfo(this);
	        }
	        foreach (Gizmo gizmo in base.GetGizmos().Where(g => g is Command_Action && (g as Command_Action).defaultLabel != "Dev: Mental break" 
                && (g as Command_Action).defaultLabel != "Dev: Make random pawn hungry" && (g as Command_Action).defaultLabel != "Dev: Kill random pawn" 
                && (g as Command_Action).defaultLabel != "Dev: Harm random pawn" && (g as Command_Action).defaultLabel != "Dev: Down random pawn"
                && (g as Command_Action).defaultLabel != "Dev: Plague on random pawn" && (g as Command_Action).defaultLabel != "Dev: Teleport to destination"))
	        {
		        yield return gizmo;
	        }

	        if (IsPlayerControlled)
	        {
		        if (vPather.Moving)
		        {
			        yield return new Command_Toggle
			        {
				        hotKey = KeyBindingDefOf.Misc1,
				        isActive = (() => vPather.Paused),
				        toggleAction = delegate()
				        {
					        if (!vPather.Moving)
					        {
						        return;
					        }
					        vPather.Paused = !vPather.Paused;
				        },
				        defaultDesc = "CommandToggleCaravanPauseDesc".Translate(2f.ToString("0.#"), 0.3f.ToStringPercent()),
				        icon = TexCommand.PauseCaravan,
				        defaultLabel = "CommandPauseCaravan".Translate()
			        };
		        }
		        if (CaravanMergeUtility.ShouldShowMergeCommand)
		        {
			        yield return CaravanMergeUtility.MergeCommand(this);
		        }
		        foreach (Gizmo gizmo2 in this.forage.GetGizmos())
		        {
			        yield return gizmo2;
		        }

		        foreach (WorldObject worldObject in Find.WorldObjects.ObjectsAt(base.Tile))
		        {
			        foreach (Gizmo gizmo3 in worldObject.GetCaravanGizmos(this))
			        {
				        yield return gizmo3;
			        }
		        }
	        }
	        if (Prefs.DevMode)
	        {
		        yield return new Command_Action
		        {
			        defaultLabel = "Vehicle Dev: Teleport to destination",
			        action = delegate()
			        {
				        base.Tile = vPather.Destination;
				        vPather.StopDead();
			        }
		        };
	        }
            if(HelperMethods.HasBoat(this) && (Find.World.CoastDirectionAt(Tile).IsValid || HelperMethods.RiverIsValid(Tile, PawnsListForReading.Where(x => HelperMethods.IsBoat(x)).ToList())))
            {
                if(!vPather.Moving && !PawnsListForReading.AnyNullified(x => !HelperMethods.IsBoat(x)))
                {
                    Command_Action dock = new Command_Action();
                    dock.icon = VehicleTex.Anchor;
                    dock.defaultLabel = Find.WorldObjects.AnySettlementBaseAt(Tile) ? "CommandDockShip".Translate() : "CommandDockShipDisembark".Translate();
                    dock.defaultDesc = Find.WorldObjects.AnySettlementBaseAt(Tile) ? "CommandDockShipDesc".Translate(Find.WorldObjects.SettlementBaseAt(Tile)) : "CommandDockShipObjectDesc".Translate();
                    dock.action = delegate ()
                    {
                        List<WorldObject> objects = Find.WorldObjects.ObjectsAt(Tile).ToList();
                        if(!objects.All(x => x is Caravan))
                            HelperMethods.ToggleDocking(this, true);
                        else
                            HelperMethods.SpawnDockedBoatObject(this);
                    };

                    yield return dock;
                }
                else if (!vPather.Moving && PawnsListForReading.AnyNullified(x => !HelperMethods.IsBoat(x)))
                {
                    Command_Action undock = new Command_Action();
                    undock.icon = VehicleTex.UnloadAll;
                    undock.defaultLabel = "CommandUndockShip".Translate();
                    undock.defaultDesc = "CommandUndockShipDesc".Translate(Label);
                    undock.action = delegate ()
                    {
                        HelperMethods.ToggleDocking(this, false);
                    };

                    yield return undock;
                }
            }
        }

        public void Notify_VehicleTeleported()
        {
            vTweener.ResetTweenedPosToRoot();
            vPather.Notify_Teleported_Int();
        }

        public override void Notify_MemberDied(Pawn member)
        {
            if (!Spawned)
            {
                Log.Error("Caravan member died in an unspawned caravan. Unspawned caravans shouldn't be kept for more than a single frame.", false);
            }
            if (!PawnsListForReading.AnyNullified(x => x is VehiclePawn vehicle && !vehicle.Dead && vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard.AnyNullified((Pawn y) => y != member && IsOwner(y))))
            {
                RemovePawn(member);
                if (Faction == Faction.OfPlayer)
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelAllCaravanColonistsDied".Translate(), "LetterAllCaravanColonistsDied".Translate(Name).CapitalizeFirst(), LetterDefOf.NegativeEvent, new GlobalTargetInfo(Tile), null, null);
                }
                pawns.Clear();
                Find.WorldObjects.Remove(this);
            }
            else
            {
                member.Strip();
                RemovePawn(member);
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (stringBuilder.Length != 0)
                stringBuilder.AppendLine();
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int num5 = 0;
            int numS = 0;
            foreach(VehiclePawn vehicle in PawnsListForReading.Where(x => x is VehiclePawn).Cast<VehiclePawn>())
            {
                numS++;
                foreach(Pawn p in vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard)
                {
                    if(p.IsColonist)
                        num++;
                    if(p.RaceProps.Animal)
                        num2++;
                    if(p.IsPrisoner)
                        num3++;
                    if(p.Downed)
                        num4++;
                    if(p.InMentalState)
                        num5++;
                }
            }
            foreach(Pawn p in PawnsListForReading.Where(x => !(x is VehiclePawn)))
            {
                if (p.IsColonist)
                    num++;
                if (p.RaceProps.Animal)
                    num2++;
                if (p.IsPrisoner)
                    num3++;
                if (p.Downed)
                    num4++;
                if (p.InMentalState)
                    num5++;
            }

            if (numS > 1)
            {
                Dictionary<Thing, int> vehicleCounts = new Dictionary<Thing, int>();
                foreach(Pawn p in PawnsListForReading.Where(x => x is VehiclePawn))
                {
                    if(vehicleCounts.ContainsKey(p))
                    {
                        vehicleCounts[p]++;
                    }
                    else
                    {
                        vehicleCounts.Add(p, 1);
                    }
                }

                foreach(KeyValuePair<Thing, int> vehicles in vehicleCounts)
                {
                    stringBuilder.Append($"{vehicles.Value} {vehicles.Key.LabelCap}");
                }
            }
            stringBuilder.Append(", " + "CaravanColonistsCount".Translate(num, (num != 1) ? Faction.OfPlayer.def.pawnsPlural : Faction.OfPlayer.def.pawnSingular));
            if (num2 == 1)
                stringBuilder.Append(", " + "CaravanAnimal".Translate());
            else if (num2 > 1)
                stringBuilder.Append(", " + "CaravanAnimalsCount".Translate(num2));
            if (num3 == 1)
                stringBuilder.Append(", " + "CaravanPrisoner".Translate());
            else if (num3 > 1)
                stringBuilder.Append(", " + "CaravanPrisonersCount".Translate(num3));
            stringBuilder.AppendLine();
            if (num5 > 0)
                stringBuilder.Append("CaravanPawnsInMentalState".Translate(num5));
            if (num4 > 0)
            {
                if (num5 > 0)
                {
                    stringBuilder.Append(", ");
                }
                stringBuilder.Append("CaravanPawnsDowned".Translate(num4));
            }
            if (num5 > 0 || num4 > 0)
            {
                stringBuilder.AppendLine();
            }

            if(vPather.Moving)
            {
                if (vPather.ArrivalAction != null)
                    stringBuilder.Append(vPather.ArrivalAction.ReportString);
                else if (HelperMethods.HasBoat(this))
                    stringBuilder.Append("CaravanSailing".Translate());
                else
                    stringBuilder.Append("CaravanTraveling".Translate());
            }
            else
            {
                Settlement settlementBase = CaravanVisitUtility.SettlementVisitedNow(this);
                if (!(settlementBase is null))
                    stringBuilder.Append("CaravanVisiting".Translate(settlementBase.Label));
                else
                    stringBuilder.Append("CaravanWaiting".Translate());
            }
            if (vPather.Moving)
            {
                float num6 = (float)CaravanArrivalTimeEstimator.EstimatedTicksToArrive(this, true) / 60000f;
                stringBuilder.AppendLine();
                stringBuilder.Append("CaravanEstimatedTimeToDestination".Translate(num6.ToString("0.#")));
            }
            if (AllOwnersDowned)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("AllCaravanMembersDowned".Translate());
            }
            else if (AllOwnersHaveMentalBreak)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("AllCaravanMembersMentalBreak".Translate());
            }
            else if (ImmobilizedByMass)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("CaravanImmobilizedByMass".Translate());
            }
            if (needs.AnyPawnOutOfFood(out string text))
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("CaravanOutOfFood".Translate());
                if (!text.NullOrEmpty())
                {
                    stringBuilder.Append(" ");
                    stringBuilder.Append(text);
                    stringBuilder.Append(".");
                }
            }
            if (!vPather.MovingNow)
            {
                int usedBedCount = beds.GetUsedBedCount();
                stringBuilder.AppendLine();
                stringBuilder.Append(CaravanBedUtility.AppendUsingBedsLabel("CaravanResting".Translate(), usedBedCount));
            }
            else
            {
                string inspectStringLine = carryTracker.GetInspectStringLine();
                if (!inspectStringLine.NullOrEmpty())
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append(inspectStringLine);
                }
                string inBedForMedicalReasonsInspectStringLine = beds.GetInBedForMedicalReasonsInspectStringLine();
                if (!inBedForMedicalReasonsInspectStringLine.NullOrEmpty())
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append(inBedForMedicalReasonsInspectStringLine);
                }
            }
            return stringBuilder.ToString();
        }

        public override void DrawExtraSelectionOverlays()
		{
			if (IsPlayerControlled && vPather.curPath != null)
			{
				vPather.curPath.DrawPath(this);
			}
            gotoMote.RenderMote();
		}

        public override void PostRemove()
        {
            base.PostRemove();
            vPather.StopDead();
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            vTweener.ResetTweenedPosToRoot();
        }

        public override void Tick()
        {
            base.Tick();
            vPather.PatherTick();
            vTweener.TweenerTick();
        }

        public VehicleCaravan_PathFollower vPather;
        public VehicleCaravan_Tweener vTweener;

        private static MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private static readonly Texture2D SplitCommand = ContentFinder<Texture2D>.Get("UI/Commands/SplitCaravan", true);

        private static Dictionary<ThingDef, Material> materials = new Dictionary<ThingDef, Material>();
    }
}
