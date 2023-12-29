using MelonLoader;
using UnityEngine;
using BoneLib;
using BoneLib.BoneMenu.Elements;
using BoneLib.BoneMenu;
using System;
using SLZ.Rig;
using ParkourMovement.InternalHelpers;
using SLZ.Vehicle;
using System.Reflection;

namespace ParkourMovement
{
    static class MelonPrefs
    {
        public static MelonPreferences_Category MelonPrefCategory { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefEnabled { get; private set; }
        public static bool isEnabled { get; set; }
        public static MenuCategory BoneMenuCategory { get; private set; }
        public static BoolElement EnabledElement { get; private set; }
        public static bool isWREnabled { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefWREnabled { get; private set; }
        public static BoolElement WREnabledElement { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefWJEnabled { get; private set; }
        public static bool isWJEnabled { get; private set; }
        public static BoolElement WJEnabledElement { get; private set; }
        public static bool isSlEnabled { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefSlEnabled { get; private set; }
        public static BoolElement SlEnabledElement { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefDJEnabled { get; private set; }
        public static bool isDJEnabled { get; private set; }
        public static BoolElement DJEnabledElement { get; private set; }
        public static MelonPreferences_Entry<float> MelonPrefWRFMult { get; private set; }
        public static FloatElement WRFMultElement { get; private set; }
        public static MelonPreferences_Entry<float> MelonPrefWRWCLength { get; private set; }
        public static FloatElement WRWCLengthElement { get; private set; }
        public static MelonPreferences_Entry<float> MelonPrefWJMult { get; private set; }
        public static FloatElement WJMultElement { get; private set; }
        public static MelonPreferences_Entry<float> MelonPrefMSLVel { get; private set; }
        public static FloatElement MSLVelElement { get; private set; }

        public static void SetupMelonPrefs()
        {
            
            MelonPrefCategory = MelonPreferences.CreateCategory("Parkour");
            MelonPrefEnabled = MelonPrefCategory.CreateEntry<bool>("isEnabled", true, "Enabled");
            isEnabled = MelonPrefEnabled.Value;
            MelonPrefWREnabled = MelonPrefCategory.CreateEntry<bool>("isWREnabled", true, "Wallrun Enabled");
            isWREnabled = MelonPrefWREnabled.Value;
            MelonPrefSlEnabled = MelonPrefCategory.CreateEntry<bool>("isSlEnabled", true, "Slide Enabled");
            isSlEnabled = MelonPrefSlEnabled.Value;
            MelonPrefDJEnabled = MelonPrefCategory.CreateEntry<bool>("isDJEnabled", true, "Double Jump Enabled");
            isDJEnabled = MelonPrefDJEnabled.Value;
            MelonPrefWJEnabled = MelonPrefCategory.CreateEntry<bool>("isWJEnabled", true, "Wall Jump Enabled");
            isWJEnabled = MelonPrefWJEnabled.Value;

            MelonPrefWRFMult = MelonPrefCategory.CreateEntry<float>("WRFMult", 1, "Wallrun Forward Multiplier");
            ParkourMovementMod.WRFwdMult = MelonPrefWRFMult.Value;

            MelonPrefWRWCLength = MelonPrefCategory.CreateEntry<float>("WRWCLength", 1, "Wall Check Distance");
            ParkourMovementMod.normWallCheckLength = MelonPrefWRWCLength.Value;

            MelonPrefWJMult = MelonPrefCategory.CreateEntry<float>("WJMult", 60, "Wall Jump Multiplier");
            ParkourMovementMod.WJForceMult = MelonPrefWJMult.Value;

            MelonPrefMSLVel = MelonPrefCategory.CreateEntry<float>("MSLVel", 0.15f, "Max Slide Velocity");
            ParkourMovementMod.maxSlVel = MelonPrefMSLVel.Value;

        }
        public static void SetupBoneMenu()
        {
            BoneMenuCategory = MenuManager.CreateCategory("Parkour", Color.white);
            EnabledElement = BoneMenuCategory.CreateBoolElement("Mod Toggle", Color.white, isEnabled, new Action<bool>(OnSetEnabled));
            WREnabledElement = BoneMenuCategory.CreateBoolElement("Wallrun Toggle", Color.white, isWREnabled, new Action<bool>(OnSetWREnabled));
            SlEnabledElement = BoneMenuCategory.CreateBoolElement("Slide Toggle", Color.white, isSlEnabled, new Action<bool>(OnSetSlEnabled));
            DJEnabledElement = BoneMenuCategory.CreateBoolElement("Double Jump Toggle", Color.white, isDJEnabled, new Action<bool>(OnSetDJEnabled));
            WJEnabledElement = BoneMenuCategory.CreateBoolElement("Wall Jump Toggle", Color.white, isWJEnabled, new Action<bool>(OnSetWJEnabled));
            WRFMultElement = BoneMenuCategory.CreateFloatElement("Wallrun Forward Mult", Color.white, ParkourMovementMod.WRFwdMult, 0.25f, 0, 10, new Action<float>(OnSetWRFwdMult));
            WRWCLengthElement = BoneMenuCategory.CreateFloatElement("Wall Check Distance", Color.white, ParkourMovementMod.normWallCheckLength, 0.25f, .25f, 5, new Action<float>(OnSetWRWCLength));
            WJMultElement = BoneMenuCategory.CreateFloatElement("Walljump Force Mult", Color.white, ParkourMovementMod.WJForceMult, 5f, 5f, 300f, new Action<float>(OnSetWJMult));
            MSLVelElement = BoneMenuCategory.CreateFloatElement("Max Slide Velocity", Color.white, ParkourMovementMod.maxSlVel, .01f, .01f, .3f, new Action<float>(OnSetMSLVel));
        }
        public static void OnSetEnabled(bool value)
        {
            isEnabled = value;
            MelonPrefEnabled.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetWREnabled(bool value)
        {
            isWREnabled = value;
            MelonPrefWREnabled.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetSlEnabled(bool value)
        {
            isSlEnabled = value;
            MelonPrefSlEnabled.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetDJEnabled(bool value)
        {
            isDJEnabled = value;
            Player.remapRig.doubleJump = value;
            MelonPrefDJEnabled.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetWJEnabled(bool value)
        {
            isWJEnabled = value;
            MelonPrefWJEnabled.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetWRFwdMult(float value)
        {
            ParkourMovementMod.WRFwdMult = value;
            MelonPrefWRFMult.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetWRWCLength(float value)
        {
            ParkourMovementMod.normWallCheckLength = value;
            MelonPrefWRWCLength.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetWJMult(float value)
        {
            ParkourMovementMod.WJForceMult = value;
            MelonPrefWJMult.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetMSLVel(float value)
        {
            ParkourMovementMod.maxSlVel = value;
            MelonPrefMSLVel.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }


        
    }
}
