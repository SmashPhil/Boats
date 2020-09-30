﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using Vehicles.AI;

namespace Vehicles
{
    public static class SPMultiCell
    {
        /// <summary>
        /// Clamp value of type T between max and min
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        /// <summary>
        /// Clamp value between a min and max but wrap around rather than return min / max
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float ClampAndWrap(this float val, float min, float max)
        {
            while (val < min || val > max)
            {
                if (val < min)
                    val += min;
                if (val > max)
                    val -= max;
            }
            return val;
        }

        /// <summary>
        /// Clamp a Pawn's exit point to their hitbox size. Avoids derendering issues for multicell-pawns
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="exitPoint"></param>
        /// <param name="map"></param>
        /// <param name="extraOffset"></param>
        public static void ClampToMap(Pawn pawn, ref IntVec3 exitPoint, Map map, int extraOffset = 0)
        {
            int x = pawn.def.size.x;
            int z = pawn.def.size.z;
            int offset = x > z ? x + extraOffset : z + extraOffset;

            if (exitPoint.x < offset)
            {
                exitPoint.x = (int)(offset / 2);
            }
            else if (exitPoint.x >= (map.Size.x - (offset / 2)))
            {
                exitPoint.x = (int)(map.Size.x - (offset / 2));
            }
            if (exitPoint.z < offset)
            {
                exitPoint.z = (int)(offset / 2);
            }
            else if (exitPoint.z > (map.Size.z - (offset / 2)))
            {
                exitPoint.z = (int)(map.Size.z - (offset / 2));
            }
        }

        /// <summary>
        /// Clamp a Pawn's spawn point to their hitbox size. Avoids derendering issues for multicell-pawns
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="spawnPoint"></param>
        /// <param name="map"></param>
        /// <param name="extraOffset"></param>
        /// <returns></returns>
        public static IntVec3 ClampToMap(this Pawn pawn, IntVec3 spawnPoint, Map map, int extraOffset = 0)
        {
            int x = pawn.def.size.x;
            int z = pawn.def.size.z;
            int offset = x > z ? x + extraOffset : z + extraOffset;
            if (spawnPoint.x < offset)
            {
                spawnPoint.x = offset / 2;
            }
            else if (spawnPoint.x >= (map.Size.x - (offset / 2)))
            {
                spawnPoint.x = map.Size.x - (offset / 2);
            }
            if (spawnPoint.z < offset)
            {
                spawnPoint.z = offset / 2;
            }
            else if (spawnPoint.z > (map.Size.z - (offset / 2)))
            {
                spawnPoint.z = map.Size.z - (offset / 2);
            }
            return spawnPoint;
        }

        /// <summary>
        /// Clamp a pawns active location based on their hitbox size. Avoids derendering issues for multicell-pawns
        /// </summary>
        /// <param name="p"></param>
        /// <param name="nextCell"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static bool ClampHitboxToMap(Pawn p, IntVec3 nextCell, Map map)
        {
            int x = p.def.size.x % 2 == 0 ? p.def.size.x / 2 : (p.def.size.x + 1) / 2;
            int z = p.def.size.z % 2 == 0 ? p.def.size.z / 2 : (p.def.size.z + 1) / 2;

            int hitbox = x > z ? x : z;
            if (nextCell.x + hitbox > map.Size.x || nextCell.z + hitbox > map.Size.z)
            {
                return true;
            }
            if (nextCell.x - hitbox < 0 || nextCell.z - hitbox < 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Convert > 360 and < 0 angles to relative 0:360 angles in a unit circle
        /// </summary>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static float ClampAngle(this float theta)
        {
            while(theta > 360 || theta < 0)
            {
                if (theta > 360)
                    theta -= 360;
                else if (theta < 360)
                    theta += 360;
            }
            return theta;
        }

        /// <summary>
        /// Ensures the cellrect inhabited by the vehicle contains no Things that will block pathing and movement.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool CellRectStandable(this Pawn pawn, Map map, IntVec3? c = null)
        {
            IntVec3 loc = c.HasValue ? c.Value : pawn.Position;
            IntVec2 dimensions = pawn.RotatedSize;
            foreach(IntVec3 cell in CellRect.CenteredOn(loc, dimensions.x, dimensions.z))
            {
                if(pawn.IsBoat() && !GenGridShips.Standable(cell, map))
                {
                    return false;
                }
                else if(!GenGrid.Standable(cell, map))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get occupied cells of pawn with hitbox larger than 1x1
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="centerPoint"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static List<IntVec3> PawnOccupiedCells(this Pawn pawn, IntVec3 centerPoint, Rot4 direction)
        {
            int sizeX;
            int sizeZ;
            switch (direction.AsInt)
            {
                case 0:
                    sizeX = pawn.def.size.x;
                    sizeZ = pawn.def.size.z;
                    break;
                case 1:
                    sizeX = pawn.def.size.z;
                    sizeZ = pawn.def.size.x;
                    break;
                case 2:
                    sizeX = pawn.def.size.x;
                    sizeZ = pawn.def.size.z;
                    break;
                case 3:
                    sizeX = pawn.def.size.z;
                    sizeZ = pawn.def.size.x;
                    break;
                default:
                    throw new NotImplementedException("MoreThan4Rotations");
            }
            return CellRect.CenteredOn(centerPoint, sizeX, sizeZ).Cells.ToList();
        }

        /// <summary>
        /// OccupiedRect shifted from pawns position
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static CellRect OccupiedRectShifted(this Pawn pawn, IntVec2 shift)
        {
            IntVec3 center = pawn.Position;
            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    break;
                case 1:
                    int x = shift.x;
                    shift.x = shift.z;
                    shift.z = x;
                    break;
                case 2:
                    shift.z *= -1;
                    break;
                case 3:
                    int x2 = shift.x;
                    shift.x = -shift.z;
                    shift.z = x2;
                    break;
            }
            center.x += shift.x;
            center.z += shift.z;
            IntVec2 size = pawn.def.size;
            GenAdj.AdjustForRotation(ref center, ref size, pawn.Rotation);
            return new CellRect(center.x - (size.x - 1) / 2, center.z - (size.z - 1) / 2, size.x, size.z);
        }

        /// <summary>
        /// Get edge of map the pawn is closest too
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static Rot4 ClosestEdge(Pawn pawn, Map map)
        {
            IntVec2 mapSize = new IntVec2(map.Size.x, map.Size.z);
            IntVec2 position = new IntVec2(pawn.Position.x, pawn.Position.z);

            SPTuple2<Rot4, int> hDistance = Math.Abs(position.x) < Math.Abs(position.x - mapSize.x) ? new SPTuple2<Rot4, int>(Rot4.West, position.x) : new SPTuple2<Rot4, int>(Rot4.East, Math.Abs(position.x - mapSize.x));
            SPTuple2<Rot4, int> vDistance = Math.Abs(position.z) < Math.Abs(position.z - mapSize.z) ? new SPTuple2<Rot4, int>(Rot4.South, position.z) : new SPTuple2<Rot4, int>(Rot4.North, Math.Abs(position.z - mapSize.z));

            return hDistance.Second <= vDistance.Second ? hDistance.First : vDistance.First;
        }

        /// <summary>
        /// Check if pawn is within certain distance of edge of map. Useful for multicell pawns who are clamped to the map beyond normal edge cell checks.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="distance"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static bool WithinDistanceToEdge(this IntVec3 position, int distance, Map map)
        {
            return position.x < distance || position.z < distance || (map.Size.x - position.x < distance) || (map.Size.z - position.z < distance);
        }

        /// <summary>
        /// Draw selection brackets for pawn with angle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bracketLocs"></param>
        /// <param name="obj"></param>
        /// <param name="worldPos"></param>
        /// <param name="worldSize"></param>
        /// <param name="dict"></param>
        /// <param name="textureSize"></param>
        /// <param name="pawnAngle"></param>
        /// <param name="jumpDistanceFactor"></param>
        public static void CalculateSelectionBracketPositionsWorldForMultiCellPawns<T>(Vector3[] bracketLocs, T obj, Vector3 worldPos, Vector2 worldSize, Dictionary<T, float> dict, Vector2 textureSize, float pawnAngle = 0f, float jumpDistanceFactor = 1f)
        {
            float num;
            float num2;
            if (!dict.TryGetValue(obj, out num))
            {
                num2 = 1f;
            }
            else
            {
                num2 = Mathf.Max(0f, 1f - (Time.realtimeSinceStartup - num) / 0.07f);
            }
            float num3 = num2 * 0.2f * jumpDistanceFactor;
            float num4 = 0.5f * (worldSize.x - textureSize.x) + num3;
            float num5 = 0.5f * (worldSize.y - textureSize.y) + num3;
            float y = AltitudeLayer.MetaOverlays.AltitudeFor();
            bracketLocs[0] = new Vector3(worldPos.x - num4, y, worldPos.z - num5);
            bracketLocs[1] = new Vector3(worldPos.x + num4, y, worldPos.z - num5);
            bracketLocs[2] = new Vector3(worldPos.x + num4, y, worldPos.z + num5);
            bracketLocs[3] = new Vector3(worldPos.x - num4, y, worldPos.z + num5);

            switch (pawnAngle)
            {
                case 45f:
                    for (int i = 0; i < 4; i++)
                    {
                        float xPos = bracketLocs[i].x - worldPos.x;
                        float yPos = bracketLocs[i].z - worldPos.z;
                        SPTuple2<float, float> newPos = SPTrig.RotatePointClockwise(xPos, yPos, 45f);
                        bracketLocs[i].x = newPos.First + worldPos.x;
                        bracketLocs[i].z = newPos.Second + worldPos.z;
                    }
                    break;
                case -45:
                    for (int i = 0; i < 4; i++)
                    {
                        float xPos = bracketLocs[i].x - worldPos.x;
                        float yPos = bracketLocs[i].z - worldPos.z;
                        SPTuple2<float, float> newPos = SPTrig.RotatePointCounterClockwise(xPos, yPos, 45f);
                        bracketLocs[i].x = newPos.First + worldPos.x;
                        bracketLocs[i].z = newPos.Second + worldPos.z;
                    }
                    break;
            }
        }

        /// <summary>
        /// Draw selection brackets transformed on position x,z for pawns whose selection brackets have been shifted
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Vector3 DrawPosTransformed(this Pawn pawn, float x, float z, float angle = 0)
        {
            Vector3 drawPos = pawn.DrawPos;
            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    drawPos.x += x;
                    drawPos.z += z;
                    break;
                case 1:
                    if (angle == -45)
                    {
                        drawPos.x += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        drawPos.z += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        break;
                    }
                    else if (angle == 45)
                    {
                        drawPos.x += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        drawPos.z -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        break;
                    }
                    drawPos.x += z;
                    drawPos.z += x;
                    break;
                case 2:
                    drawPos.x -= x;
                    drawPos.z -= z;
                    break;
                case 3:
                    if (angle == -45)
                    {
                        drawPos.x -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        drawPos.z -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        break;
                    }
                    else if (angle == 45)
                    {
                        drawPos.x -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        drawPos.z += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        break;
                    }
                    drawPos.x -= z;
                    drawPos.z -= x;
                    break;
                default:
                    throw new NotImplementedException("Pawn Rotation outside Rot4");
            }
            return drawPos;
        }
    }
}
