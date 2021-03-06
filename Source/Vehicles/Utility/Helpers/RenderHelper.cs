﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class RenderHelper
	{
		private static List<int> cachedEdgeTiles = new List<int>();
		private static int cachedEdgeTilesForCenter = -1;
		private static int cachedEdgeTilesForRadius = -1;
		private static int cachedEdgeTilesForWorldSeed = -1;

		/// <summary>
		/// Calculate VehicleTurret draw offset
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="xOffset"></param>
		/// <param name="yOffset"></param>
		/// <param name="rotationOffset"></param>
		/// <param name="turretRotation"></param>
		/// <param name="attachedTo"></param>
		public static Pair<float,float> TurretDrawOffset(float angle, float xOffset, float yOffset, out Pair<float, float> rotationOffset, float turretRotation = 0, VehicleTurret attachedTo = null)
		{
			rotationOffset = new Pair<float, float>(0, 0);
			if (attachedTo != null)
			{
				return Ext_Math.RotatePointClockwise(attachedTo.turretRenderLocation.x + xOffset, attachedTo.turretRenderLocation.y + yOffset, turretRotation);
			}
			return Ext_Math.RotatePointClockwise(xOffset, yOffset, angle);
		}

		/// <summary>
		/// Calculate VehicleTurret draw offset given <paramref name="rot"/>
		/// </summary>
		/// <param name="rot"></param>
		/// <param name="xOffset"></param>
		/// <param name="yOffset"></param>
		/// <param name="rotationOffset"></param>
		/// <param name="turretRotation"></param>
		/// <param name="attachedTo"></param>
		/// <returns></returns>
		public static Pair<float,float> ShipDrawOffset(Rot8 rot, float xOffset, float yOffset, out Pair<float, float> rotationOffset, float turretRotation = 0, VehicleTurret attachedTo = null)
		{
			rotationOffset = new Pair<float, float>(0, 0);
			if(attachedTo != null)
			{
				return Ext_Math.RotatePointClockwise(attachedTo.turretRenderLocation.x + xOffset, attachedTo.turretRenderLocation.y + yOffset, turretRotation);
			}

			return rot.AsInt switch
			{
				//North
				0 => new Pair<float, float>(xOffset, yOffset),
				//East
				1 => new Pair<float, float>(yOffset, -xOffset),
				//South
				2 => new Pair<float, float>(-xOffset, -yOffset),
				//West
				3 => new Pair<float, float>(-yOffset, xOffset),
				//NorthEast
				4 => Ext_Math.RotatePointClockwise(yOffset, -xOffset, 45f),
				//SouthEast
				5 => Ext_Math.RotatePointCounterClockwise(yOffset, -xOffset, 45f),
				//SouthWest
				6 => Ext_Math.RotatePointClockwise(-yOffset, xOffset, 45f),
				//NorthWest
				7 => Ext_Math.RotatePointCounterClockwise(-yOffset, xOffset, 45f),
				//Default
				_ => throw new ArgumentOutOfRangeException("VehicleRotation is not within bounds. RotationInt must be between 0 and 7 for each lateral, longitudinal, and diagonal direction.")
			};
		}

		/// <summary>
		/// Draw VehicleTurret on vehicle
		/// </summary>
		/// <param name="turret"></param>
		public static void DrawAttachedThing(VehicleTurret turret)
		{
			try
			{
				Vector3 topVectorRotation = new Vector3(turret.turretRenderOffset.x, 1f, turret.turretRenderOffset.y).RotatedBy(turret.TurretRotation);
				float locationRotation = 0f;
				if(turret.attachedTo != null)
				{
					locationRotation = turret.attachedTo.TurretRotation;
				}
				Pair<float, float> drawOffset = TurretDrawOffset(turret.vehicle.Rotation.AsAngle + turret.vehicle.Angle, turret.turretRenderLocation.x, turret.turretRenderLocation.y, out Pair<float, float> rotOffset1, locationRotation, turret.attachedTo);
					
				Vector3 topVectorLocation = new Vector3(turret.vehicle.DrawPos.x + drawOffset.First + rotOffset1.First, turret.vehicle.DrawPos.y + turret.drawLayer, turret.vehicle.DrawPos.z + drawOffset.Second + rotOffset1.Second);
				if (turret.rTracker.Recoil > 0f)
				{
					topVectorLocation = Ext_Math.PointFromAngle(topVectorLocation, turret.rTracker.Recoil, turret.rTracker.Angle);
				}
				if (turret.attachedTo != null && turret.attachedTo.rTracker.Recoil > 0f)
				{
					topVectorLocation = Ext_Math.PointFromAngle(topVectorLocation, turret.attachedTo.rTracker.Recoil, turret.attachedTo.rTracker.Angle);
				}
				Mesh cannonMesh = turret.CannonGraphic.MeshAt(Rot4.North);
				Graphics.DrawMesh(cannonMesh, topVectorLocation, turret.TurretRotation.ToQuat(), turret.CannonMaterial, 0);
			}
			catch(Exception ex)
			{
				Log.Error(string.Format("Error occurred during rendering of attached thing on {0}. Exception: {1}", turret.vehicle.Label, ex.Message));
			}
		}

		/// <summary>
		/// Draw cannon textures on GUI given collection of cannons and vehicle GUI is being drawn for
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="displayRect"></param>
		/// <param name="cannons"></param>
		/// <param name="vehicleMaskName"></param>
		/// <param name="resolveGraphics"></param>
		/// <param name="manualColorOne"></param>
		/// <param name="manualColorTwo"></param>
		/// <remarks>Might possibly want to throw into separate threads</remarks>
		public static void DrawCannonTextures(this VehiclePawn vehicle, Rect displayRect, IEnumerable<VehicleTurret> cannons, PatternDef pattern, bool resolveGraphics = false, Color? manualColorOne = null, Color? manualColorTwo = null, Color? manualColorThree = null)
		{
			foreach (VehicleTurret cannon in cannons)
			{
				if (cannon.NoGraphic)
				{
					continue;
				}
				GraphicDataRGB graphicData = vehicle.VehicleDef.graphicData;
				if (resolveGraphics)
				{
					cannon.ResolveCannonGraphics(vehicle);
				}

				float cannonWidth = (displayRect.width / graphicData.drawSize.x) * cannon.CannonGraphicData.drawSize.x;
				float cannonHeight = (displayRect.height / graphicData.drawSize.y) * cannon.CannonGraphicData.drawSize.y;

				/// ( center point of vehicle) + (UI size / drawSize) * cannonPos
				/// y axis inverted as UI goes top to bottom, but DrawPos goes bottom to top
				float xCannon = (displayRect.x + (displayRect.width / 2) - (cannonWidth / 2)) + ((vehicle.VehicleDef.drawProperties.upgradeUISize.x / graphicData.drawSize.x) * cannon.turretRenderLocation.x);
				float yCannon = (displayRect.y + (displayRect.height / 2) - (cannonHeight / 2)) - ((vehicle.VehicleDef.drawProperties.upgradeUISize.y / graphicData.drawSize.y) * cannon.turretRenderLocation.y);

				Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
				Material cannonMat = new Material(cannon.CannonGraphic.MatAt(Rot4.North, vehicle));
				
				if (cannon.CannonGraphic.Shader.SupportsRGBMaskTex() && (manualColorOne != null || manualColorTwo != null || manualColorThree != null) && cannon.CannonGraphic.GetType().IsAssignableFrom(typeof(Graphic_Cannon)))
				{
					MaterialRequestRGB matReq = new MaterialRequestRGB()
					{
						mainTex = cannon.CannonTexture,
						shader = cannon.CannonGraphic.Shader,
						color = manualColorOne != null ? manualColorOne.Value : vehicle.DrawColor,
						colorTwo = manualColorTwo != null ? manualColorTwo.Value : vehicle.DrawColorTwo,
						colorThree = manualColorThree != null ? manualColorThree.Value : vehicle.DrawColorThree,
						tiles = vehicle.tiles,
						properties = pattern.properties,
						isSkin = pattern is SkinDef,
						maskTex = cannon.CannonGraphic.masks[0],
						patternTex = pattern[Rot8.North]
					};
					cannonMat = MaterialPoolExpanded.MatFrom(matReq);
				}
				GenUI.DrawTextureWithMaterial(cannonDrawnRect, cannon.CannonTexture, cannonMat);

				if (VehicleMod.settings.debug.debugDrawCannonGrid)
				{
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
					Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
					Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
				}
			}
		}

		/// <summary>
		/// Draw cannon textures on GUI given collection of cannons and vehicle GUI is being drawn for with additional tiling
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="displayRect"></param>
		/// <param name="cannons"></param>
		/// <param name="vehicleMaskName"></param>
		/// <param name="resolveGraphics"></param>
		/// <param name="manualColorOne"></param>
		/// <param name="manualColorTwo"></param>
		/// <remarks>Might possibly want to throw into separate threads</remarks>
		public static void DrawCannonTexturesTiled(this VehiclePawn vehicle, Rect displayRect, IEnumerable<VehicleTurret> cannons, PatternDef pattern, bool resolveGraphics = false, 
			Color? manualColorOne = null, Color? manualColorTwo = null, Color? manualColorThree = null, float tiles = 1, float displacementX = 0, float displacementY = 0)
		{
			foreach (VehicleTurret cannon in cannons)
			{
				if (cannon.NoGraphic)
				{
					continue;
				}
				GraphicDataRGB graphicData = vehicle.VehicleDef.graphicData;
				if (resolveGraphics)
				{
					cannon.ResolveCannonGraphics(vehicle);
				}

				float cannonWidth = (displayRect.width / graphicData.drawSize.x) * cannon.CannonGraphicData.drawSize.x;
				float cannonHeight = (displayRect.height / graphicData.drawSize.y) * cannon.CannonGraphicData.drawSize.y;

				/// ( center point of vehicle) + (UI size / drawSize) * cannonPos
				/// y axis inverted as UI goes top to bottom, but DrawPos goes bottom to top
				float xCannon = (displayRect.x + (displayRect.width / 2) - (cannonWidth / 2)) + ((vehicle.VehicleDef.drawProperties.upgradeUISize.x / graphicData.drawSize.x) * cannon.turretRenderLocation.x);
				float yCannon = (displayRect.y + (displayRect.height / 2) - (cannonHeight / 2)) - ((vehicle.VehicleDef.drawProperties.upgradeUISize.y / graphicData.drawSize.y) * cannon.turretRenderLocation.y);

				Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
				Material cannonMat = new Material(cannon.CannonGraphic.MatAt(Rot4.North, vehicle));

				if (cannon.CannonGraphic.Shader.SupportsRGBMaskTex() && (manualColorOne != null || manualColorTwo != null || manualColorThree != null) && cannon.CannonGraphic.GetType().IsAssignableFrom(typeof(Graphic_Cannon)))
				{
					MaterialRequestRGB matReq = new MaterialRequestRGB()
					{
						mainTex = cannon.CannonTexture,
						shader = cannon.CannonGraphic.Shader,
						color = manualColorOne != null ? manualColorOne.Value : vehicle.DrawColor,
						colorTwo = manualColorTwo != null ? manualColorTwo.Value : vehicle.DrawColorTwo,
						colorThree = manualColorThree != null ? manualColorThree.Value : vehicle.DrawColorThree,
						tiles = tiles,
						displacement = new Vector2(displacementX, displacementY),
						properties = pattern.properties,
						isSkin = pattern is SkinDef,
						maskTex = cannon.CannonGraphic.masks[0],
						patternTex = pattern[Rot8.North]
					};
					cannonMat = MaterialPoolExpanded.MatFrom(matReq);
				}
				GenUI.DrawTextureWithMaterial(cannonDrawnRect, cannon.CannonTexture, cannonMat);

				if (VehicleMod.settings.debug.debugDrawCannonGrid)
				{
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
					Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
					Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
				}
			}
		}

		/// <summary>
		/// Draw Vehicle texture with option to manually apply colors to Material
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="vehicleTex"></param>
		/// <param name="vehicle"></param>
		/// <param name="vehicleMaskName"></param>
		/// <param name="resolveGraphics"></param>
		/// <param name="manualColorOne"></param>
		/// <param name="manualColorTwo"></param>
		public static void DrawVehicleTex(Rect rect, Texture2D vehicleTex, VehiclePawn vehicle, PatternDef pattern = null, bool resolveGraphics = false, Color? manualColorOne = null, Color? manualColorTwo = null, Color? manualColorThree = null)
		{
			Material mat = new Material(vehicle.VehicleGraphic.MatAt(Rot4.North, vehicle));
			
			if (vehicle.VehicleGraphic.Shader.SupportsRGBMaskTex() && (manualColorOne != null || manualColorTwo != null || manualColorThree != null))
			{
				MaterialRequestRGB matReq = new MaterialRequestRGB()
				{
					mainTex = vehicleTex,
					shader = vehicle.VehicleGraphic.Shader,
					color = manualColorOne != null ? manualColorOne.Value : vehicle.DrawColor,
					colorTwo = manualColorTwo != null ? manualColorTwo.Value : vehicle.DrawColorTwo,
					colorThree = manualColorThree != null ? manualColorThree.Value : vehicle.DrawColorThree,
					tiles = vehicle.tiles,
					properties = pattern.properties,
					isSkin = pattern is SkinDef,
					maskTex = vehicle.VehicleGraphic.masks[0],
					patternTex = pattern?[Rot8.North]
				};
				mat = MaterialPoolExpanded.MatFrom(matReq);
			}

			GenUI.DrawTextureWithMaterial(rect, vehicleTex, mat);

			if (vehicle.CompCannons != null)
			{
				vehicle.DrawCannonTextures(rect, vehicle.CompCannons.Cannons.OrderBy(x => x.drawLayer), pattern, resolveGraphics, manualColorOne, manualColorTwo, manualColorThree);
			}
		}

		/// <summary>
		/// Draw Vehicle texture dynamically allowing for tiling and rescaling of patterns
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="vehicleTex"></param>
		/// <param name="vehicle"></param>
		/// <param name="pattern"></param>
		/// <param name="resolveGraphics"></param>
		/// <param name="manualColorOne"></param>
		/// <param name="manualColorTwo"></param>
		/// <param name="manualColorThree"></param>
		/// <param name="tiles"></param>
		public static void DrawVehicleTexTiled(Rect rect, Texture2D vehicleTex, VehiclePawn vehicle, PatternDef pattern = null, bool resolveGraphics = false, 
			Color? manualColorOne = null, Color? manualColorTwo = null, Color? manualColorThree = null, float tiles = 1, float displacementX = 0, float displacementY = 0)
		{
			Material mat = new Material(vehicle.VehicleGraphic.MatAt(Rot4.North, vehicle));

			if (vehicle.VehicleGraphic.Shader.SupportsRGBMaskTex() && (manualColorOne != null || manualColorTwo != null || manualColorThree != null))
			{
				MaterialRequestRGB matReq = new MaterialRequestRGB()
				{
					mainTex = vehicleTex,
					shader = vehicle.VehicleGraphic.Shader,
					color = manualColorOne != null ? manualColorOne.Value : vehicle.DrawColor,
					colorTwo = manualColorTwo != null ? manualColorTwo.Value : vehicle.DrawColorTwo,
					colorThree = manualColorThree != null ? manualColorThree.Value : vehicle.DrawColorThree,
					tiles = tiles,
					displacement = new Vector2(displacementX, displacementY),
					properties = pattern.properties,
					isSkin = pattern is SkinDef,
					maskTex = vehicle.VehicleGraphic.masks[0],
					patternTex = pattern?[Rot8.North]
				};
				mat = MaterialPoolExpanded.MatFrom(matReq);
			}

			GenUI.DrawTextureWithMaterial(rect, vehicleTex, mat);

			if (vehicle.CompCannons != null)
			{
				vehicle.DrawCannonTexturesTiled(rect, vehicle.CompCannons.Cannons.OrderBy(x => x.drawLayer), pattern, resolveGraphics, manualColorOne, manualColorTwo, manualColorThree, tiles, displacementX, displacementY);
			}
		}

		/// <summary>
		/// Draw Vehicle texture from mod settings
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="graphic">cached Vehicle_Graphic for proper rendering of texture</param>
		/// <param name="vehicleTex">cached graphic being passed in is faster than ContentFinder every tick</param>
		public static void DrawVehicleTexInSettings(Rect rect, VehicleDef vehicleDef, Graphic_Vehicle graphic, Texture2D vehicleTex, PatternDef pattern, Rot8 rot)
		{
			float UISizeX = vehicleDef.drawProperties.settingsUISize.x;
			float UISizeY = vehicleDef.drawProperties.settingsUISize.y;
			float UIMoveX = vehicleDef.drawProperties.settingsUICoord.x;
			float UIMoveY = vehicleDef.drawProperties.settingsUICoord.y;

			float centeredX = rect.x + (rect.width / 2) - (UISizeX / 2);
			float centeredY = rect.y + (rect.height / 2) - (UISizeY / 2);

			Rect displayRect = new Rect(centeredX + UIMoveX, centeredY + UIMoveY, UISizeX, UISizeY);
			Material mat = new Material(graphic.MatAt(rot, pattern));
			GenUI.DrawTextureWithMaterial(displayRect, vehicleTex, mat);

			if (vehicleDef.GetSortedCompProperties<CompProperties_Cannons>() is CompProperties_Cannons props)
			{
				DrawCannonTexturesInSettings(displayRect, vehicleDef, props.turrets.OrderBy(x => x.drawLayer), pattern, rot);
			}
		}

		/// <summary>
		/// Draw VehicleTurrets in ModSettings
		/// </summary>
		/// <remarks>This method variant is necessary. Preset UI size and offset will be used from the vehicle's draw properties</remarks>
		/// <param name="displayRect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="cannons"></param>
		/// <param name="pattern"></param>
		/// <param name="rot"></param>
		public static void DrawCannonTexturesInSettings(Rect displayRect, VehicleDef vehicleDef, IEnumerable<VehicleTurret> cannons, PatternDef pattern, Rot8 rot)
		{
			foreach (VehicleTurret cannon in cannons)
			{
				if (cannon.NoGraphic)
				{
					continue;
				}

				GraphicDataRGB vehicleGraphicData = vehicleDef.graphicData;

				cannon.ResolveCannonGraphics(vehicleDef);

				float cannonWidth = (displayRect.width / vehicleGraphicData.drawSize.x) * cannon.CannonGraphicData.drawSize.x;
				float cannonHeight = (displayRect.height / vehicleGraphicData.drawSize.y) * cannon.CannonGraphicData.drawSize.y;

				/// ( center point of vehicle) + (UI size / drawSize) * cannonPos
				/// y axis inverted as UI goes top to bottom, but DrawPos goes bottom to top
				float xCannon = (displayRect.x + (displayRect.width / 2) - (cannonWidth / 2)) + ((vehicleDef.drawProperties.settingsUISize.x / vehicleGraphicData.drawSize.x) * cannon.turretRenderLocation.x);
				float yCannon = (displayRect.y + (displayRect.height / 2) - (cannonHeight / 2)) - ((vehicleDef.drawProperties.settingsUISize.y / vehicleGraphicData.drawSize.y) * cannon.turretRenderLocation.y);

				Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);

				Material cannonMat = cannon.CannonGraphic.Shader.SupportsRGBMaskTex() ? new Material(cannon.CannonGraphic.MatAt(pattern)) : cannon.CannonGraphic.MatSingle;

				GenUI.DrawTextureWithMaterial(cannonDrawnRect, cannon.CannonTexture, cannonMat);

				if (VehicleMod.settings.debug.debugDrawCannonGrid)
				{
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
					Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
					Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
				}
			}
		}

		/// <summary>
		/// Draw <paramref name="vehicleDef"/>
		/// </summary>
		/// <remarks><paramref name="material"/> may overwrite material used for vehicle</remarks>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="material"></param>
		public static void DrawVehicleDef(Rect rect, VehicleDef vehicleDef, Material material = null)
		{
			if (VehicleMod.settings.vehicles.defaultMasks.TryGetValue(vehicleDef.defName, out var maskName))
			{
				var graphic = VehicleTex.CachedGraphics[vehicleDef];
				PatternDef pattern = DefDatabase<PatternDef>.GetNamed(maskName);
				if (material is null)
				{
					material = new Material(graphic.MatAt(Rot8.North, pattern));
				}
				GenUI.DrawTextureWithMaterial(rect, VehicleTex.VehicleTexture(vehicleDef, Rot8.North), material);
				if (vehicleDef.GetSortedCompProperties<CompProperties_Cannons>() is CompProperties_Cannons props)
				{
					DrawCannonTexturesInSettings(rect, vehicleDef, props.turrets.OrderBy(x => x.drawLayer), pattern, Rot8.North);
				}
			}
		}

		public static void DrawLinesBetweenTargets(VehiclePawn pawn, Job curJob, JobQueue jobQueue)
		{
			Vector3 a = pawn.Position.ToVector3Shifted();
			if (pawn.vPather.curPath != null)
			{
				a = pawn.vPather.Destination.CenterVector3;
			}
			else if (curJob != null && curJob.targetA.IsValid && (!curJob.targetA.HasThing || (curJob.targetA.Thing.Spawned && curJob.targetA.Thing.Map == pawn.Map)))
			{
				GenDraw.DrawLineBetween(a, curJob.targetA.CenterVector3, AltitudeLayer.Item.AltitudeFor());
				a = curJob.targetA.CenterVector3;
			}
			for (int i = 0; i < jobQueue.Count; i++)
			{
				if (jobQueue[i].job.targetA.IsValid)
				{
					if (!jobQueue[i].job.targetA.HasThing || (jobQueue[i].job.targetA.Thing.Spawned && jobQueue[i].job.targetA.Thing.Map == pawn.Map))
					{
						Vector3 centerVector = jobQueue[i].job.targetA.CenterVector3;
						GenDraw.DrawLineBetween(a, centerVector, AltitudeLayer.Item.AltitudeFor());
						a = centerVector;
					}
				}
				else
				{
					List<LocalTargetInfo> targetQueueA = jobQueue[i].job.targetQueueA;
					if (targetQueueA != null)
					{
						for (int j = 0; j < targetQueueA.Count; j++)
						{
							if (!targetQueueA[j].HasThing || (targetQueueA[j].Thing.Spawned && targetQueueA[j].Thing.Map == pawn.Map))
							{
								Vector3 centerVector2 = targetQueueA[j].CenterVector3;
								GenDraw.DrawLineBetween(a, centerVector2, AltitudeLayer.Item.AltitudeFor());
								a = centerVector2;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Render lines from <paramref name="cannonPos"/> given angle and ranges
		/// </summary>
		/// <param name="cannonPos"></param>
		/// <param name="restrictedAngle"></param>
		/// <param name="minRange"></param>
		/// <param name="maxRange"></param>
		/// <param name="theta"></param>
		/// <param name="additionalAngle"></param>
		public static void DrawAngleLines(Vector3 cannonPos, Vector2 restrictedAngle, float minRange, float maxRange, float theta, float additionalAngle = 0f)
		{
			Vector3 minTargetPos1 = cannonPos.PointFromAngle(minRange, restrictedAngle.x + additionalAngle);
			Vector3 minTargetPos2 = cannonPos.PointFromAngle(minRange, restrictedAngle.y + additionalAngle);

			Vector3 maxTargetPos1 = cannonPos.PointFromAngle(maxRange, restrictedAngle.x + additionalAngle);
			Vector3 maxTargetPos2 = cannonPos.PointFromAngle(maxRange, restrictedAngle.y + additionalAngle);

			GenDraw.DrawLineBetween(minTargetPos1, maxTargetPos1);
			GenDraw.DrawLineBetween(minTargetPos2, maxTargetPos2);
			if (minRange > 0)
			{
				GenDraw.DrawLineBetween(cannonPos, minTargetPos1, SimpleColor.Red);
				GenDraw.DrawLineBetween(cannonPos, minTargetPos2, SimpleColor.Red);
			}

			float angleStart = restrictedAngle.x;

			Vector3 lastPointMin = minTargetPos1;
			Vector3 lastPointMax = maxTargetPos1;
			for (int angle = 0; angle < theta + 1; angle++)
			{
				Vector3 targetPointMax = cannonPos.PointFromAngle(maxRange, angleStart + angle + additionalAngle);
				GenDraw.DrawLineBetween(lastPointMax, targetPointMax);
				lastPointMax = targetPointMax;

				if (minRange > 0)
				{
					Vector3 targetPointMin = cannonPos.PointFromAngle(minRange, angleStart + angle + additionalAngle);
					GenDraw.DrawLineBetween(lastPointMin, targetPointMin, SimpleColor.Red);
					lastPointMin = targetPointMin;
				}
			}
		}
		
		/// <summary>
		/// Allow for optional overriding of mote saturation on map while being able to throw any MoteThrown <paramref name="mote"/>
		/// </summary>
		/// <seealso cref="MoteThrown"/>
		/// <param name="loc"></param>
		/// <param name="map"></param>
		/// <param name="mote"></param>
		/// <param name="overrideSaturation"></param>
		public static Mote ThrowMoteEnhanced(Vector3 loc, Map map, MoteThrown mote, bool overrideSaturation = false)
		{
			if(!loc.ShouldSpawnMotesAt(map) || (overrideSaturation && map.moteCounter.Saturated))
			{
				return null;
			}

			GenSpawn.Spawn(mote, loc.ToIntVec3(), map, WipeMode.Vanish);
			return mote;
		}

		/// <summary>
		/// Draw ColorPicker and HuePicker
		/// </summary>
		/// <param name="fullRect"></param>
		public static Rect DrawColorPicker(Rect fullRect)
		{
			Rect rect = fullRect.ContractedBy(10f);
			rect.width = 15f;
			if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !Dialog_ColorPicker.draggingHue)
			{
				Dialog_ColorPicker.draggingHue = true;
			}
			if (Dialog_ColorPicker.draggingHue && Event.current.isMouse)
			{
				float num = Dialog_ColorPicker.hue;
				Dialog_ColorPicker.hue = Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y);
				if (Dialog_ColorPicker.hue != num)
				{
					Dialog_ColorPicker.Instance.SetColor(Dialog_ColorPicker.hue, Dialog_ColorPicker.saturation, Dialog_ColorPicker.value);
				}
			}
			if (Input.GetMouseButtonUp(0))
			{
				Dialog_ColorPicker.draggingHue = false;
			}
			Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
			Widgets.DrawTexturePart(rect, new Rect(0f, 0f, 1f, 1f), Dialog_ColorPicker.HueChart);
			Rect rect2 = new Rect(0f, 0f, 16f, 16f)
			{
				center = new Vector2(rect.center.x, rect.height * (1f - Dialog_ColorPicker.hue) + rect.y).Rounded()
			};

			Widgets.DrawTextureRotated(rect2, VehicleTex.ColorHue, 0f);
			rect = fullRect.ContractedBy(10f);
			rect.x = rect.xMax - rect.height;
			rect.width = rect.height;
			if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !Dialog_ColorPicker.draggingCP)
			{
				Dialog_ColorPicker.draggingCP = true;
			}
			if (Dialog_ColorPicker.draggingCP)
			{
				Dialog_ColorPicker.saturation = Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x);
				Dialog_ColorPicker.value = Mathf.InverseLerp(rect.width, 0f, Event.current.mousePosition.y - rect.y);
				Dialog_ColorPicker.Instance.SetColor(Dialog_ColorPicker.hue, Dialog_ColorPicker.saturation, Dialog_ColorPicker.value);
			}
			if (Input.GetMouseButtonUp(0))
			{
				Dialog_ColorPicker.draggingCP = false;
			}
			Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
			Widgets.DrawBoxSolid(rect, Color.white);
			GUI.color = Color.HSVToRGB(Dialog_ColorPicker.hue, 1f, 1f);
			Widgets.DrawTextureFitted(rect, Dialog_ColorPicker.ColorChart, 1f);
			GUI.color = Color.white;
			GUI.BeginClip(rect);
			rect2.center = new Vector2(rect.width * Dialog_ColorPicker.saturation, rect.width * (1f - Dialog_ColorPicker.value));
			if (Dialog_ColorPicker.value >= 0.4f && (Dialog_ColorPicker.hue <= 0.5f || Dialog_ColorPicker.saturation <= 0.5f))
			{
				GUI.color = Dialog_ColorPicker.Blackist;
			}
			Widgets.DrawTextureFitted(rect2, VehicleTex.ColorPicker, 1f);
			GUI.color = Color.white;
			GUI.EndClip();
			return rect;
		}

		/// <summary>
		/// Draw <paramref name="buildDef"/> with proper vehicle material
		/// </summary>
		/// <param name="command"></param>
		/// <param name="rect"></param>
		/// <param name="buildDef"></param>
		public static GizmoResult GizmoOnGUIWithMaterial(Command command, Rect rect, VehicleBuildDef buildDef)
		{
			VehicleDef vehicleDef = buildDef.thingToSpawn;
			Text.Font = GameFont.Tiny;
			bool flag = false;
			if (Mouse.IsOver(rect))
			{
				flag = true;
				if (!command.disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
			Material material = command.disabled ? TexUI.GrayscaleGUI : null;
			GenUI.DrawTextureWithMaterial(rect, command.BGTexture, material);

			Vector2 rectSize = vehicleDef.ScaleDrawRatio(new Vector2(rect.width * 0.95f, rect.height * 0.95f));
			float newX = (rect.width / 2) - (rectSize.x / 2);
			float newY = (rect.height / 2) - (rectSize.y / 2);
			DrawVehicleDef(new Rect(rect.x + newX, rect.y + newY, rectSize.x, rectSize.y), vehicleDef, material);

			bool flag2 = false;
			KeyCode keyCode = (command.hotKey == null) ? KeyCode.None : command.hotKey.MainKey;
			if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
			{
				Vector2 vector = new Vector2(5f, 3f);
				Widgets.Label(new Rect(rect.x + vector.x, rect.y + vector.y, rect.width - 10f, 18f), keyCode.ToStringReadable());
				GizmoGridDrawer.drawnHotKeys.Add(keyCode);
				if (command.hotKey.KeyDownEvent)
				{
					flag2 = true;
					Event.current.Use();
				}
			}
			if (Widgets.ButtonInvisible(rect, true))
			{
				flag2 = true;
			}
			string topRightLabel = command.TopRightLabel;
			if (!topRightLabel.NullOrEmpty())
			{
				Vector2 vector2 = Text.CalcSize(topRightLabel);
				Rect position;
				Rect rectBase = position = new Rect(rect.xMax - vector2.x - 2f, rect.y + 3f, vector2.x, vector2.y);
				position.x -= 2f;
				position.width += 3f;
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperRight;
				GUI.DrawTexture(position, TexUI.GrayTextBG);
				Widgets.Label(rectBase, topRightLabel);
				Text.Anchor = TextAnchor.UpperLeft;
			}
			string labelCap = command.LabelCap;
			if (!labelCap.NullOrEmpty())
			{
				float num = Text.CalcHeight(labelCap, rect.width);
				Rect rect2 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
				GUI.DrawTexture(rect2, TexUI.GrayTextBG);
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperCenter;
				Widgets.Label(rect2, labelCap);
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
			}
			GUI.color = Color.white;
			if (Mouse.IsOver(rect) /*&& command.DoTooltip*/)
			{
				TipSignal tip = command.Desc;
				if (command.disabled && !command.disabledReason.NullOrEmpty())
				{
					tip.text += "\n\n" + "DisabledCommand".Translate() + ": " + command.disabledReason;
				}
				TooltipHandler.TipRegion(rect, tip);
			}
			if (!command.HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
			{
				UIHighlighter.HighlightOpportunity(rect, command.HighlightTag);
			}
			Text.Font = GameFont.Small;
			if (flag2)
			{
				if (command.disabled)
				{
					if (!command.disabledReason.NullOrEmpty())
					{
						Messages.Message(command.disabledReason, MessageTypeDefOf.RejectInput, false);
					}
					return new GizmoResult(GizmoState.Mouseover, null);
				}
				GizmoResult result;
				if (Event.current.button == 1)
				{
					result = new GizmoResult(GizmoState.OpenedFloatMenu, Event.current);
				}
				else
				{
					if (!TutorSystem.AllowAction(command.TutorTagSelect))
					{
						return new GizmoResult(GizmoState.Mouseover, null);
					}
					result = new GizmoResult(GizmoState.Interacted, Event.current);
					TutorSystem.Notify_Event(command.TutorTagSelect);
				}
				return result;
			}
			else
			{
				if (flag)
				{
					return new GizmoResult(GizmoState.Mouseover, null);
				}
				return new GizmoResult(GizmoState.Clear, null);
			}
		}

		/// <summary>
		/// Create rotated Mesh where <paramref name="rot"/> [1:3] indicates number of 90 degree rotations
		/// </summary>
		/// <param name="size"></param>
		/// <param name="rot"></param>
		public static Mesh NewPlaneMesh(Vector2 size, int rot)
		{
			Vector3[] vertices = new Vector3[4];
			Vector2[] uv = new Vector2[4];
			int[] triangles = new int[6];
			vertices[0] = new Vector3(-0.5f * size.x, 0f, -0.5f * size.y);
			vertices[1] = new Vector3(-0.5f * size.x, 0f, 0.5f * size.y);
			vertices[2] = new Vector3(0.5f * size.x, 0f, 0.5f * size.y);
			vertices[3] = new Vector3(0.5f * size.x, 0f, -0.5f * size.y);
			switch (rot)
			{
				case 1:
					uv[0] = new Vector2(1f, 0f);
					uv[1] = new Vector2(0f, 0f);
					uv[2] = new Vector2(0f, 1f);
					uv[3] = new Vector2(1f, 1f);
					break;
				case 2:
					uv[0] = new Vector2(1f, 1f);
					uv[1] = new Vector2(1f, 0f);
					uv[2] = new Vector2(0f, 0f);
					uv[3] = new Vector2(0f, 1f);
					break;
				case 3:
					uv[0] = new Vector2(0f, 1f);
					uv[1] = new Vector2(1f, 1f);
					uv[2] = new Vector2(1f, 0f);
					uv[3] = new Vector2(0f, 0f);
					break;
				default:
					uv[0] = new Vector2(0f, 0f);
					uv[1] = new Vector2(0f, 1f);
					uv[2] = new Vector2(1f, 1f);
					uv[3] = new Vector2(1f, 0f);
					break;
			}
			triangles[0] = 0;
			triangles[1] = 1;
			triangles[2] = 2;
			triangles[3] = 0;
			triangles[4] = 2;
			triangles[5] = 3;
			Mesh mesh = new Mesh();
			mesh.name = "NewPlaneMesh()";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>
		/// Create mesh with varying length of vertices rather than being restricted to 4
		/// </summary>
		/// <param name="size"></param>
		public static Mesh NewTriangleMesh(Vector2 size)
		{
			Vector3[] vertices = new Vector3[3];
			Vector2[] uv = new Vector2[3];
			int[] triangles = new int[3];

			vertices[0] = new Vector3(-0.5f * size.x, 0, 1 * size.y);
			vertices[1] = new Vector3(0.5f * size.x, 0, 1 * size.y);
			vertices[2] = new Vector3(0, 0, 0);

			uv[0] = vertices[0];
			uv[1] = vertices[1];
			uv[2] = vertices[2];

			triangles[0] = 0;
			triangles[1] = 1;
			triangles[2] = 2;

			Mesh mesh = new Mesh();
			mesh.name = "TriangleMesh";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}
		
		/// <summary>
		/// Create triangle mesh with a cone like arc for an FOV effect
		/// </summary>
		/// <remarks><paramref name="arc"/> should be within [0:360]</remarks>
		/// <param name="size"></param>
		/// <param name="arc"></param>
		public static Mesh NewConeMesh(float distance, int arc)
		{
			float currentAngle = arc / -2f;
			Vector3[] vertices = new Vector3[arc + 2];
			Vector2[] uv = new Vector2[vertices.Length];
			int[] triangles = new int[arc * 3];

			vertices[0] = Vector3.zero;
			uv[0] = Vector3.zero;
			int t = 0;
			for (int i = 1; i <= arc; i++)
			{
				vertices[i] = vertices[0].PointFromAngle(distance, currentAngle);
				uv[i] = vertices[i];
				currentAngle += 1;

				triangles[t] = 0;
				triangles[t + 1] = i;
				triangles[t + 2] = i + 1;
				t += 3;
			}

			Mesh mesh = new Mesh();
			mesh.name = "ConeMesh";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>
		/// Reroute Draw method call to dynamic object's Draw method
		/// </summary>
		/// <param name="worldObject"></param>
		public static bool RenderDynamicWorldObjects(WorldObject worldObject)
		{
			if (VehicleMod.settings.main.dynamicWorldDrawing && worldObject is DynamicDrawnWorldObject dynamicObject)
			{
				dynamicObject.Draw();
				return true;
			}
			return false;
		}

		public static void DrawWorldRadiusRing(int center, int radius, Material material)
		{
			if (radius < 0)
			{
				return;
			}
			if (cachedEdgeTilesForCenter != center || cachedEdgeTilesForRadius != radius || cachedEdgeTilesForWorldSeed != Find.World.info.Seed)
			{
				cachedEdgeTilesForCenter = center;
				cachedEdgeTilesForRadius = radius;
				cachedEdgeTilesForWorldSeed = Find.World.info.Seed;
				cachedEdgeTiles.Clear();
				Find.WorldFloodFiller.FloodFill(center, (int tile) => true, delegate (int tile, int dist)
				{
					if (dist > radius + 1)
					{
						return true;
					}
					if (dist == radius + 1)
					{
						cachedEdgeTiles.Add(tile);
					}
					return false;
				}, int.MaxValue, null);
				WorldGrid worldGrid = Find.WorldGrid;
				Vector3 c = worldGrid.GetTileCenter(center);
				Vector3 n = c.normalized;
				cachedEdgeTiles.Sort(delegate (int a, int b)
				{
					float num = Vector3.Dot(n, Vector3.Cross(worldGrid.GetTileCenter(a) - c, worldGrid.GetTileCenter(b) - c));
					if (Mathf.Abs(num) < 0.0001f)
					{
						return 0;
					}
					if (num < 0f)
					{
						return -1;
					}
					return 1;
				});
			}
			GenDraw.DrawWorldLineStrip(cachedEdgeTiles, material, 5f);
		}
	}
}
