﻿using RimWorld;
using Vehicles.Defs;
using Vehicles.AI;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public class JobGiver_GotoTravelDestinationVehicle : ThinkNode_JobGiver
	{
		private LocomotionUrgency locomotionUrgency = LocomotionUrgency.Jog;

		private Danger maxDanger = Danger.Some;

		private int jobMaxDuration = 999999;

		private bool exactCell;

		private IntRange WaitTicks = new IntRange(30, 80);

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_GotoTravelDestinationVehicle job = (JobGiver_GotoTravelDestinationVehicle)base.DeepCopy(resolve);
			job.locomotionUrgency = locomotionUrgency;
			job.maxDanger = maxDanger;
			job.jobMaxDuration = jobMaxDuration;
			job.exactCell = exactCell;
			return job;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			pawn.drafter.Drafted = true;

			IntVec3 cell = pawn.mindState.duty.focus.Cell;
			if (pawn.IsBoat() && !ShipReachabilityUtility.CanReachShip(pawn, cell, PathEndMode.OnCell, PawnUtility.ResolveMaxDanger(pawn, maxDanger), false, TraverseMode.ByPawn))
			{
				return null;
			}
			else if (!pawn.IsBoat() && pawn is VehiclePawn vehicle && !ReachabilityUtility.CanReach(vehicle, cell, PathEndMode.OnCell, PawnUtility.ResolveMaxDanger(vehicle, maxDanger), false))
			{
				return null;
			}
			if (exactCell && pawn.Position == cell)
			{
				return null;
			}

			return new Job(JobDefOf.Goto, cell)
			{
				locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, locomotionUrgency),
				expiryInterval = jobMaxDuration
			};
		}
	}
}