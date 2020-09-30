﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    public static class VehicleTex
    {
        public static readonly Texture2D UnloadAll = ContentFinder<Texture2D>.Get("UI/UnloadAll");

        public static readonly Texture2D UnloadPassenger = ContentFinder<Texture2D>.Get("UI/UnloadPassenger");

        public static readonly Texture2D UnloadCaptain = ContentFinder<Texture2D>.Get("UI/UnloadCaptain");

        public static readonly Texture2D Anchor = ContentFinder<Texture2D>.Get("UI/Anchor");

        public static readonly Texture2D BroadsideCannon_Port = ContentFinder<Texture2D>.Get("UI/Cannon_Left");

        public static readonly Texture2D BroadsideCannon_Starboard = ContentFinder<Texture2D>.Get("UI/Cannon_Right");

        public static readonly Texture2D Rename = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

        public static readonly Texture2D Recolor = ContentFinder<Texture2D>.Get("UI/Paintbrush");

        public static readonly Texture2D Drop = ContentFinder<Texture2D>.Get("UI/Buttons/Drop");

        public static readonly Texture2D FishingIcon = ContentFinder<Texture2D>.Get("UI/FishingIcon");

        public static readonly Texture2D CaravanIcon = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan");

        public static readonly Texture2D PackCargoIcon = ContentFinder<Texture2D>.Get("UI/CargoCrate");

        public static readonly Texture2D CancelPackCargoIcon = ContentFinder<Texture2D>.Get("UI/CancelCargoCrate");

        public static readonly Texture2D AmmoBG = ContentFinder<Texture2D>.Get("UI/GizmoGrid/AmmoBoxBG");

        public static readonly Texture2D MissingAmmoIcon = ContentFinder<Texture2D>.Get("UI/MissingAmmo");

        public static readonly Texture2D BoatIcon = ContentFinder<Texture2D>.Get("UI/BoatObject");

        public static readonly Texture2D UnloadIcon = ContentFinder<Texture2D>.Get("UI/UnloadIcon");

        public static readonly Texture2D ReloadIcon = ContentFinder<Texture2D>.Get("UI/ReloadIcon");

        public static readonly Texture2D FireSelectIcon = ContentFinder<Texture2D>.Get("UI/FireSelectIcon");

        public static readonly Texture2D AutoTargetIcon = ContentFinder<Texture2D>.Get("UI/AutoTargetIcon");

        public static readonly Texture2D HaltIcon = ContentFinder<Texture2D>.Get("UI/Commands/Halt");

        public static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));

		public static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);

		public static readonly Texture2D TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");

        public static readonly Texture2D SwitchLeft = ContentFinder<Texture2D>.Get("UI/SwitchLeft");

        public static readonly Texture2D SwitchRight = ContentFinder<Texture2D>.Get("UI/SwitchRight");

        public static readonly Texture2D ReverseIcon = ContentFinder<Texture2D>.Get("UI/ReverseIcon");

        public static readonly Texture2D FlickerIcon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower");

        public static readonly Dictionary<ThingDef, Texture2D> CachedTextureIcons = new Dictionary<ThingDef, Texture2D>();

        private static readonly Dictionary<string, Texture2D> cachedTextureFilepaths = new Dictionary<string, Texture2D>();

        static VehicleTex()
        {
            foreach(ThingDef vehicleDef in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.IsVehicleDef()))
            {
                string iconFilePath = vehicleDef.GetCompProperties<CompProperties_Vehicle>().iconTexPath;
                Texture2D tex;
                if(cachedTextureFilepaths.ContainsKey(iconFilePath))
                {
                    tex = cachedTextureFilepaths[iconFilePath];
                }
                else
                {
                    tex = ContentFinder<Texture2D>.Get(iconFilePath);
                    cachedTextureFilepaths.Add(iconFilePath, tex);
                }
                CachedTextureIcons.Add(vehicleDef, tex);
            }
        }
    }
}