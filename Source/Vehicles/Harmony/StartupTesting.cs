﻿using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class StartupTesting
	{
		[UnitTest(Active = false)]
		private static void UnitTestStrafing()
		{
			Map sourceMap = null;
			Map targetMap = null;
			VehiclePawn vehicle = null;
			foreach (Settlement settlement in Find.WorldObjects.Settlements.Where(s => s.Faction == Faction.OfPlayer))
			{
				Map map = GetOrGenerateMapUtility.GetOrGenerateMap(settlement.Tile, null);
				if (map.spawnedThings.FirstOrDefault(t => t is VehiclePawn vehicleCheck && vehicleCheck.CompVehicleLauncher != null) is VehiclePawn vehicleTarget)
				{
					sourceMap = map;
					vehicle = vehicleTarget;
				}
				else
				{
					targetMap = map;
				}
			}
			if (sourceMap is null || targetMap is null)
			{
				SmashLog.Error($"Unable to execute unit test <method>UnitTestStrafing</method> post load.");
				return;
			}
			Current.Game.CurrentMap = targetMap;
			LaunchTargeter.Instance.RegisterActionOnTile(targetMap.Tile, new AerialVehicleArrivalAction_StrafeMap(vehicle, targetMap.Parent));
		}

		[UnitTest(Active = false)]
		private static void UnitTestColorDialog()
		{
			Map map = Current.Game.CurrentMap;
			if (map is null)
			{
				SmashLog.Error($"Unable to execute unit test <method>UnitTestColorDialog</method> post load.");
				return;
			}
			VehicleDef vehicleDef = DefDatabase<VehicleDef>.GetNamed("Tank");
			VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
			GenSpawn.Spawn(vehicle, CellFinderExtended.RandomCenterCell(map,
				(IntVec3 c) => !c.Roofed(map) && c.InBounds(map) && vehicleDef.CellRectStandable(map, c)), map);
			vehicle.ChangeColor();
		}
	}
}
