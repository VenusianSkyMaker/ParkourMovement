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

namespace ParkourMovement
{
    public class ParkourMovementMod : MelonMod
    {
        public static ParkourMovementMod Instance;
        private PhysicsRig Phys;
        private Transform head;
        private Rigidbody pelvis;
        private RigManager RM;

        //stat changes

        //WallRun / WallJump
        public float CurrWallCheckLength = 1f;
        public float WRStickMult = 0.3f;
        public float WRFwdMult = 1;
        public float WRUptimeMult = 0.25f;
        public float WRMinFU = 1;
        public float WRWallCheckLength = 3f;
        public float normWallCheckLength = 1f;
        public float WJForceMult = 40f;
        private float ungVel = 0.5f;
        private float minVelForWR = 0.4f;
        public float wallRunAngle;
        public float MaxWRSpeed = 8.25f;
        public Camera cam;

        //Slide
        public float minSlVel = 0.7f;
        public float crouch, MinSlCrouch = -0.5f;
        public bool isInSlide = false;
        public float initialFeetDrag, SlFeetDrag = 0f;
        public float SlSpeedMult = 6400f;
        public float SlEndTime;
        public float maxSlVel = 3f;
        public Seat SlidingSeat;

        public static bool isReady = false;
        private bool canWallRun = true;
        private bool isInWR = false;
        private int framesUngrounded = 0;

        private Vector3 dir;
        private float seatVelIncr = 0.55f;
        private float currSlVel;

        private LayerMask WallMask;

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
        public static bool isSlEnabled { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefSlEnabled { get; private set; }
        public static BoolElement SlEnabledElement { get; private set; }
        public static MelonPreferences_Entry<bool> MelonPrefDJEnabled { get; private set; }
        public static bool isDJEnabled { get; private set; }
        public static BoolElement DJEnabledElement { get; private set; }




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
        }
        public static void SetupBoneMenu()
        {
            BoneMenuCategory = MenuManager.CreateCategory("Parkour", Color.white);
            EnabledElement = BoneMenuCategory.CreateBoolElement("Mod Toggle", Color.white, isEnabled, new Action<bool>(OnSetEnabled));
            WREnabledElement = BoneMenuCategory.CreateBoolElement("Wallrun Toggle", Color.white, isWREnabled, new Action<bool>(OnSetWREnabled));
            SlEnabledElement = BoneMenuCategory.CreateBoolElement("Slide Toggle", Color.white, isSlEnabled, new Action<bool>(OnSetSlEnabled));
            DJEnabledElement = BoneMenuCategory.CreateBoolElement("Double Jump Toggle", Color.white, isDJEnabled, new Action<bool>(OnSetDJEnabled));
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

        public override void OnPreferencesLoaded()
        {
            isEnabled = MelonPrefEnabled.Value;
            EnabledElement.SetValue(isEnabled);
        }

        // end of Preferences

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            
        }
        public override void OnFixedUpdate()
        {
            
            //check for nulls
            if(Player.physicsRig != null)
            {
                RM = Player.rigManager;
                Phys = Player.physicsRig;
                head = Player.playerHead;
                pelvis = Phys.torso.rbPelvis;
                isReady = true;
                initialFeetDrag = Phys.rbFeet.drag;
            }
            //wallrun
            if (isEnabled && isReady)
            {
                if(isWREnabled)//Wallrun and WallJump 
                {
                    if (Phys.wholeBodyVelocity.y < -ungVel || Phys.wholeBodyVelocity.y > ungVel)
                    {
                        framesUngrounded++;
                    }
                    else
                    {
                        framesUngrounded = 0;
                        isInWR = false;
                        CurrWallCheckLength = normWallCheckLength;
                    }

                    if (isInWR && isDJEnabled)
                    {
                        CurrWallCheckLength = WRWallCheckLength;

                        //wallJump
                        if (Player.rightController.GetAButtonDown())
                        {
                            
                            pelvis.velocity = Vector3.zero;
                            Vector3 Force = new Vector3(head.forward.x * WJForceMult, head.forward.y * WJForceMult, head.forward.z * WJForceMult);
                            pelvis.AddForce(Force, ForceMode.VelocityChange);
                        }
                    }
                    else
                    {
                        wallRunAngle = 0;
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
                    /*
                    FramesInSlide += 1;
                    crouch = Player.controllerRig.GetCrouch();
                    MelonLogger.Msg("1st Pass: slide" + isInSlide);
                    //slide continuations
                    if (Time.time >= SlEndTime && isInSlide)
                    {
                        isInSlide = false;
                        SlidingSeat.DeRegister();
                        MelonLogger.Warning("Deregistered");
                        SlidingSeat.transform.GetChild(0).gameObject.SetActive(true);
                    }
                    else if (isInSlide && Time.time < SlEndTime && crouch < MinSlCrouch && FramesInSlide > MinFramesInSlide)
                    {
                        //continue slide
                        //SlidingSeat.seatRb.velocity = new Vector3(SlidingSeatObj.transform.forward.x * SlSpeedMult / 2, 0, SlidingSeatObj.transform.forward.z * SlSpeedMult / 2);
                        SlidingSeat.seatRb.velocity = SlidingSeat.transform.forward * 500;
                    }

                    MelonLogger.Msg("2nd Pass: slide" + isInSlide);
                    //is not in wallrun, is not in slide (prevent infinite slide speed), has minimum start velocity, doesnt exceed maximum vertical velocity
                    if (!isInWR && !isInSlide && Phys.wholeBodyVelocity.magnitude > minSlVel && Mathf.Abs(Phys.wholeBodyVelocity.y) < ungVel)
                    {
                        if(crouch < MinSlCrouch && SlidingSeat == null)
                        {
                            isInSlide = true;
                            SlidingSeat = GameObject.Instantiate(SlidingSeatObj).GetComponent<Seat>();
                            SlidingSeat.transform.position = pelvis.position;
                            SlidingSeatObj.transform.eulerAngles = new Vector3(0, pelvis.rotation.eulerAngles.y, 0);
                            SlidingSeat.Register(Player.rigManager);
                            //SlidingSeat.seatRb.velocity = new Vector3(SlidingSeatObj.transform.forward.x * SlSpeedMult / 2, 0, SlidingSeatObj.transform.forward.z * SlSpeedMult / 2);
                            SlidingSeat.seatRb.velocity = SlidingSeat.transform.forward * 500;
                            MelonLogger.Msg("SeatCreated");
                            FramesInSlide = 0;

                        }else if(crouch < MinSlCrouch && SlidingSeat.rigManager != Player.rigManager)
                        {
                            isInSlide = true;
                            SlidingSeat = GameObject.Instantiate(SlidingSeatObj).GetComponent<Seat>();
                            SlidingSeat.transform.position = pelvis.position; 
                            SlidingSeatObj.transform.eulerAngles = new Vector3(0, pelvis.rotation.eulerAngles.y, 0);
                            //SlidingSeat.seatRb.velocity = new Vector3(SlidingSeatObj.transform.forward.x * SlSpeedMult / 2, 0, SlidingSeatObj.transform.forward.z * SlSpeedMult / 2);
                            SlidingSeat.seatRb.velocity = SlidingSeat.transform.forward * 500;
                            SlidingSeat.Register(Player.rigManager);
                            MelonLogger.Msg("SeatCreated");
                            FramesInSlide = 0;
                        }
                    }

                    */
                    crouch = Player.controllerRig.GetCrouch();
                    if (isInSlide)
                    {
                        if(Time.time >= SlEndTime)
                        {
                            isInSlide = false;
                            SlidingSeat.DeRegister();
                            GameObject.Destroy(SlidingSeat.gameObject);
                        }
                        else
                        {
                            currSlVel = Mathf.Clamp(currSlVel + seatVelIncr, 0, maxSlVel);
                            SlidingSeat.seatRb.MovePosition(SlidingSeat.transform.position + currSlVel * dir);
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
                            SlidingSeat.transform.forward = pelvis.transform.forward;
                            dir = SlidingSeat.transform.forward;
                            dir = new Vector3(dir.x, 0,dir.z);
                            currSlVel = 0;
                            SlidingSeat.Register(RM);
                            SlEndTime = Time.time +  3;
                        }
                    }
                }
            }
        }
    }
}