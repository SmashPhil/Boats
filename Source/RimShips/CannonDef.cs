﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimShips
{
    public class CannonDef : Def
    {
        public WeaponType weaponType;
        public WeaponLocation weaponLocation;
        public ThingDef projectile;
        public ThingDef moteCannon;
        public ThingDef moteFlash;
        public SoundDef cannonSound;

        public string cannonTexPath;
        public string baseCannonTexPath;

        public List<float> centerPoints = new List<float>();
        public List<int> cannonsPerPoint = new List<int>();

        [DefaultValue(ProjectileHitFlags.All)]
        public ProjectileHitFlags hitFlags;

        public bool splitCannonGroups = false;

        public float spreadRadius = 0f;

        public float maxRange = -1f;

        public float minRange = 0f;

        public int numberOfShots = 1;

        public float cooldownTimer = 5;

        public float warmUpTimer = 3;

        public int numberCannons = 1;

        public float spacing = 0f;

        public float offset = 0f;

        public float projectileOffset = 0f;

        public int baseTicksBetweenShots = 50;

        public float moteSpeedThrown = 2;
    }
}