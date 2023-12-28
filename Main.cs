using MelonLoader;
using UnityEngine;
using BoneLib;
using BoneLib.BoneMenu.Elements;
using BoneLib.BoneMenu;
using System;
using SLZ.Rig;
using ParkourMovement.InternalHelpers;
using SLZ.Vehicle;
using System.Linq;
using System.Reflection;
using System.IO;
using SLZ.Props;
using TriangleNet.Logging;

namespace ParkourMovement
{
    public class ParkourMovementMod : MelonMod
    {
        public static ParkourMovementMod Instance;
        public static bool isReady = false;
        private PhysicsRig Phys;
        private Transform head;
        private Rigidbody pelvis;
        private RigManager RM;
        private BaseController controller;

        //stat changes

        //WallRun / WallJump 
        public float CurrWallCheckLength = 1f;
        public float WRStickMult = 0.3f;
        public static float WRFwdMult = 1;
        public float WRUptimeMult = 0.25f;
        public float WRMinFU = 1;
        public float WRWallCheckLength = 3f;
        public static float normWallCheckLength = 1f;
        public static float WJForceMult = 60f;
        private float ungVel = 0.5f;
        private float minVelForWR = 0.4f;
        public float wallRunAngle;
        public float MaxWRSpeed = 8.25f;
        public Camera cam;
        private LayerMask WallMask;

        private bool canWallRun = true;
        private bool isInWR = false;
        private int framesUngrounded = 0;

        //Slide
        public float minSlVel = 1.2f;
        public float crouch, MinSlCrouch = -0.5f;
        public bool isInSlide = false;
        public float SlSpeedMult = 6400f;
        public float SlEndTime;
        public static float maxSlVel = 0.15f;//0.162f 
        public Seat SlidingSeat;
        private Vector3 dir;
        private float seatVelIncr = 0.35f;//.99f
        private float currSlVel;
        private float seatGrav = 2.6f;//subtracts y component, keep this positive

        

        //Bundles
        public static AssetBundle SlidingSeatBundle;
        public static GameObject SlidingSeatObj;
        public const string SSBundleName = "ParkourMovement.dependencies.slidingseat.bundle";
        

        //Preferences
        public static MelonPreferences_Category MelonPrefCategory { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefEnabled { get; private set; }
        public static bool isEnabled { get; private set; }
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
            WRFwdMult = MelonPrefWRFMult.Value;

            MelonPrefWRWCLength = MelonPrefCategory.CreateEntry<float>("WRWCLength", 1, "Wall Check Distance");
            normWallCheckLength = MelonPrefWRWCLength.Value;

            MelonPrefWJMult = MelonPrefCategory.CreateEntry<float>("WJMult", 60, "Wall Jump Multiplier");
            WJForceMult = MelonPrefWJMult.Value;

            MelonPrefMSLVel = MelonPrefCategory.CreateEntry<float>("MSLVel", 0.15f, "Max Slide Velocity");
            maxSlVel = MelonPrefMSLVel.Value;

        }
        public static void SetupBoneMenu()
        {
            BoneMenuCategory = MenuManager.CreateCategory("Parkour", Color.white);
            EnabledElement = BoneMenuCategory.CreateBoolElement("Mod Toggle", Color.white, isEnabled, new Action<bool>(OnSetEnabled));
            WREnabledElement = BoneMenuCategory.CreateBoolElement("Wallrun Toggle", Color.white, isWREnabled, new Action<bool>(OnSetWREnabled));
            SlEnabledElement = BoneMenuCategory.CreateBoolElement("Slide Toggle", Color.white, isSlEnabled, new Action<bool>(OnSetSlEnabled));
            DJEnabledElement = BoneMenuCategory.CreateBoolElement("Double Jump Toggle", Color.white, isDJEnabled, new Action<bool>(OnSetDJEnabled));
            WJEnabledElement = BoneMenuCategory.CreateBoolElement("Wall Jump Toggle", Color.white, isWJEnabled, new Action<bool>(OnSetWJEnabled));
            WRFMultElement = BoneMenuCategory.CreateFloatElement("Wallrun Forward Mult", Color.white, WRFwdMult, 0.25f, 0, 10,new Action<float>(OnSetWRFwdMult));
            WRWCLengthElement = BoneMenuCategory.CreateFloatElement("Wall Check Distance", Color.white, normWallCheckLength, 0.25f, .25f, 5, new Action<float>(OnSetWRWCLength));
            WJMultElement = BoneMenuCategory.CreateFloatElement("Walljump Force Mult", Color.white, WJForceMult, 5f, 5f, 300f, new Action<float>(OnSetWJMult));
            MSLVelElement = BoneMenuCategory.CreateFloatElement("Max Slide Velocity", Color.white, maxSlVel, .01f, .01f, .3f, new Action<float>(OnSetMSLVel));
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
            WRFwdMult = value;
            MelonPrefWRFMult.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetWRWCLength(float value)
        {
            normWallCheckLength = value;
            MelonPrefWRWCLength.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetWJMult(float value)
        {
            WJForceMult = value;
            MelonPrefWJMult.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }
        public static void OnSetMSLVel(float value)
        {
            maxSlVel = value;
            MelonPrefMSLVel.Value = value;
            MelonPrefCategory.SaveToFile(false);
        }


        public override void OnPreferencesLoaded()
        {
            isEnabled = MelonPrefEnabled.Value;
            EnabledElement.SetValue(isEnabled);
            WREnabledElement.SetValue(isWREnabled);
            SlEnabledElement.SetValue(isSlEnabled);
            DJEnabledElement.SetValue(isDJEnabled);
            WJEnabledElement.SetValue(isWJEnabled);

            WRFMultElement.SetValue(WRFwdMult);
            WRWCLengthElement.SetValue(normWallCheckLength);
        }

        // end of Preferences


        public override void OnInitializeMelon()
        {
            Instance = this;
            WallMask |= (1 << 0);
            WallMask |= (1 << 13);
            SetupMelonPrefs();
            SetupBoneMenu();

            

            SlidingSeatBundle = EmbeddedAssembly.LoadFromAssembly(Assembly.GetExecutingAssembly(), SSBundleName);
            if (SlidingSeatBundle == null)
            {
                MelonLogger.Msg("failed to load spawnable target bundle, check dll/ check you made spawnable target bundle into embedded resource");
            }
            var refs = SlidingSeatBundle.LoadAllAssets();
            refs[0].hideFlags = HideFlags.DontUnloadUnusedAsset;
            SlidingSeatObj = refs[0].TryCast<GameObject>(); 
        }
        

        public override void OnFixedUpdate()
        {
            
            //check for nulls
            if(Player.physicsRig != null && !isReady)
            {
                RM = Player.rigManager;
                
                Phys = Player.physicsRig;
                
                head = Player.playerHead;
                
                pelvis = Phys.torso.rbPelvis;
                
                controller = Player.rightController;
                
                isReady = true;
            }else if(Player.physicsRig == null)
            {
                isReady = false;
                Phys = null;
                head = null;
                pelvis = null;
                controller = null;
                SlidingSeat = null;
            }
            //wallrun
            if (isEnabled && isReady)
            {
                if(isWREnabled)//Wallrun and WallJump 
                {
                    if (Mathf.Abs(Phys.wholeBodyVelocity.y) > ungVel)
                    {
                        framesUngrounded++;
                    }
                    else
                    {
                        framesUngrounded = 0;
                        isInWR = false;
                        CurrWallCheckLength = normWallCheckLength;
                    }

                    if (isInWR)
                    {
                        CurrWallCheckLength = WRWallCheckLength;
                        Player.remapRig.doubleJump = false;
                        //wallJump
                        if (Player.rightController.GetAButtonDown() && isWJEnabled)
                        {
                            
                            pelvis.velocity = Vector3.zero;
                            Vector3 Force = new Vector3(head.forward.x * WJForceMult, head.forward.y * WJForceMult, head.forward.z * WJForceMult);
                            pelvis.AddForce(Force, ForceMode.VelocityChange);
                        }
                    }
                    else
                    {
                        Player.remapRig.doubleJump = isDJEnabled;
                    }

                    if (framesUngrounded >= WRMinFU && canWallRun && Phys.wholeBodyVelocity.magnitude > minVelForWR)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(head.position, head.right, out hit, CurrWallCheckLength, WallMask, QueryTriggerInteraction.Ignore))
                        {
                            if (Vector3.Angle(Vector3.down, hit.normal) > 80)
                            {
                                if (!isInWR) pelvis.velocity = Vector3.zero; //should slow down player

                                Vector3 sideNorm = new Vector3(-hit.normal.z, 0, hit.normal.x); //check one of two sidenormals, if angle is less than 90deg apply vel in this direction
                                if (Vector3.Angle(sideNorm, head.forward) >= 90)
                                {
                                    sideNorm = -sideNorm;//invert sidenorm if its the wrong one
                                }

                                Vector3 Force = new Vector3(-hit.normal.x * WRStickMult, -hit.normal.y * WRStickMult, -hit.normal.z * WRStickMult) + //stick force
                                    new Vector3(sideNorm.x * WRFwdMult, WRUptimeMult, sideNorm.z * WRFwdMult); //movement force
                                pelvis.AddForce(Force, ForceMode.VelocityChange);
                                pelvis.velocity = Vector3.ClampMagnitude(pelvis.velocity, MaxWRSpeed);
                                //EXP
                                wallRunAngle = 15;
                                // /EXP
                                isInWR = true;
                            }
                        }
                        if (Physics.Raycast(head.position, -head.right, out hit, CurrWallCheckLength, WallMask, QueryTriggerInteraction.Ignore))
                        {
                            if (Vector3.Angle(Vector3.down, hit.normal) > 80)
                            {
                                if (!isInWR) pelvis.velocity = Vector3.zero; //should slow down player

                                Vector3 sideNorm = new Vector3(-hit.normal.z, 0, hit.normal.x); //check one of two sidenormals, if angle is less than 90deg apply vel in this direction
                                if (Vector3.Angle(sideNorm, head.forward) >= 90)
                                {
                                    sideNorm = -sideNorm;//invert sidenorm if is wrong one
                                }

                                Vector3 Force = new Vector3(-hit.normal.x * WRStickMult, -hit.normal.y * WRStickMult, -hit.normal.z * WRStickMult) + //stick force
                                    new Vector3(sideNorm.x * WRFwdMult, WRUptimeMult, sideNorm.z * WRFwdMult); //movement force
                                pelvis.AddForce(Force, ForceMode.VelocityChange);
                                pelvis.velocity = Vector3.ClampMagnitude(pelvis.velocity, MaxWRSpeed);
                                isInWR = true;
                            }
                        }
                    }
                }
                
                if(isSlEnabled)
                {
                    crouch = controller._thumbstickAxis.y;
                    
                    if (isInSlide)
                    {
                        if(Time.time >= SlEndTime || crouch > MinSlCrouch) //end crouch
                        {
                            isInSlide = false;
                            pelvis.velocity = Vector3.zero;
                            SlidingSeat.DeRegister();
                            GameObject.Destroy(SlidingSeat.gameObject);
                        }
                        else
                        {
                            currSlVel = Mathf.Clamp(currSlVel + seatVelIncr * Time.deltaTime, 0, maxSlVel);
                            Vector3 t = SlidingSeat.transform.position + currSlVel * dir;
                            if(!Physics.Raycast(SlidingSeat.transform.position + new Vector3(0, .3f, 0), Vector3.down, 0.5f, WallMask))
                            {
                                t.y -= seatGrav * Time.deltaTime;
                            }
                            
                            SlidingSeat.seatRb.MovePosition(t); //note retry with physics based seat
                        }
                    }
                    else
                    {
                        if(crouch < MinSlCrouch && Phys.wholeBodyVelocity.magnitude > minSlVel && Mathf.Abs(Phys.wholeBodyVelocity.y) < minSlVel) 
                        {
                            isInSlide = true;
                            SlidingSeat = GameObject.Instantiate(SlidingSeatObj).GetComponent<Seat>();
                            SlidingSeat.seatRb = SlidingSeat.gameObject.GetComponent<Rigidbody>();

                            SlidingSeat.transform.position = pelvis.position;
                            dir = head.transform.forward;
                            dir = new Vector3(dir.x, 0,dir.z); //make dir parallel to ground
                            currSlVel = 0;
                            SlidingSeat.transform.eulerAngles = new Vector3(70, 0, 0); //has to be done before or it will rotate the head too
                            SlidingSeat.Register(RM);
                            SlidingSeat.transform.forward = head.transform.forward;
                            SlidingSeat.transform.eulerAngles = new Vector3(0, SlidingSeat.transform.eulerAngles.y, SlidingSeat.transform.eulerAngles.z); //adjust rotations with rigs
                            SlEndTime = Time.time +  2;
                            
                        }
                    }
                }
            }
        }
    }
}
