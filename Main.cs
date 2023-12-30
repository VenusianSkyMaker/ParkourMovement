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
    public class ParkourMovementMod : MelonMod
    {
        public static ParkourMovementMod Instance;
        public static bool isReady = false;
        public PhysicsRig Phys;
        public Transform head;
        public Rigidbody pelvis;
        public RigManager RM;
        public BaseController controller;

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
        public float ungVel = 0.5f;
        public float minVelForWR = 0.4f;
        public float wallRunAngle;
        public float MaxWRSpeed = 8.25f;
        public Camera cam;
        public LayerMask WallMask;

        public bool canWallRun = true;
        public bool isInWR = false;
        public int framesUngrounded = 0;

        //Slide
        public float minSlVel = 1.2f;
        public float crouch, MinSlCrouch = -0.5f;
        public bool isInSlide = false;
        public float SlSpeedMult = 6400f;
        public float SlEndTime;
        public static float maxSlVel = 0.15f;//0.162f 
        public Seat SlidingSeat;
        public Vector3 dir;
        public float seatVelIncr = 0.35f;//.99f
        public float currSlVel;
        public float seatGrav = 2.6f;//subtracts y component, keep this positive
        public bool midAirSlid;

        

        //Bundles
        public static AssetBundle SlidingSeatBundle;
        public static GameObject SlidingSeatObj;
        public const string SSBundleName = "ParkourMovement.dependencies.slidingseat.bundle";


        //Preferences

        public override void OnPreferencesLoaded()
        {

            MelonPrefs.isEnabled = MelonPrefs.MelonPrefEnabled.Value;
            MelonPrefs.EnabledElement.SetValue(MelonPrefs.isEnabled);
            MelonPrefs.WREnabledElement.SetValue(MelonPrefs.isWREnabled);
            MelonPrefs.SlEnabledElement.SetValue(MelonPrefs.isSlEnabled);
            MelonPrefs.DJEnabledElement.SetValue(MelonPrefs.isDJEnabled);
            MelonPrefs.WJEnabledElement.SetValue(MelonPrefs.isWJEnabled);

            MelonPrefs.WRFMultElement.SetValue(WRFwdMult);
            MelonPrefs.WRWCLengthElement.SetValue(normWallCheckLength);
        }

        // end of Preferences


        public override void OnInitializeMelon()
        {
            Instance = this;
            WallMask |= (1 << 0); //default
            WallMask |= (1 << 13); //background layer
            MelonPrefs.SetupMelonPrefs();
            MelonPrefs.SetupBoneMenu();

            Sliding.Setup();
            WallStuff.Setup();
            

            SlidingSeatBundle = EmbeddedAssembly.LoadFromAssembly(Assembly.GetExecutingAssembly(), SSBundleName);
            if (SlidingSeatBundle == null)
            {
                MelonLogger.Msg("failed to load target bundle, check dll / check you made target bundle into embedded resource");
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
            }
            else if(Player.physicsRig == null) //necessary to not break on level change
            {
                isReady = false;
                Phys = null;
                head = null;
                pelvis = null;
                controller = null;
                SlidingSeat = null;
            }

            //wallrun
            if (MelonPrefs.isEnabled && isReady)
            {
                if(MelonPrefs.isWREnabled)//Wallrun and WallJump 
                {
                    WallStuff.UpdateGroundedVariables();

                    if (isInWR)
                    {
                        CurrWallCheckLength = WRWallCheckLength;
                        Player.remapRig.doubleJump = false;


                        if (Player.rightController.GetAButtonDown() && MelonPrefs.isWJEnabled)
                        {
                            WallStuff.WallJump();
                        }

                    }
                    else
                    {
                        Player.remapRig.doubleJump = MelonPrefs.isDJEnabled;
                    }

                    if (framesUngrounded >= WRMinFU && canWallRun && Phys.wholeBodyVelocity.magnitude > minVelForWR)
                    {
                        RaycastHit hit;

                        if (Physics.Raycast(head.position, head.right, out hit, CurrWallCheckLength, WallMask, QueryTriggerInteraction.Ignore))
                        {
                            WallStuff.WallRun(true, hit);
                        }
                        if (Physics.Raycast(head.position, -head.right, out hit, CurrWallCheckLength, WallMask, QueryTriggerInteraction.Ignore))
                        {
                            WallStuff.WallRun(false, hit);
                        }
                    }
                }
                
                if(MelonPrefs.isSlEnabled)
                {
                    crouch = controller._thumbstickAxis.y;

                    if(midAirSlid && Mathf.Abs(Phys.wholeBodyVelocity.y) < ungVel) { midAirSlid = false; }
                    
                    if (isInSlide)
                    {
                        if(Time.time >= SlEndTime || crouch > MinSlCrouch) //end crouch
                        {
                            Sliding.EndSlide();
                        }
                        else
                        {
                            Sliding.Slide();
                        }
                    }
                    else
                    {
                        if(crouch < MinSlCrouch && Phys.wholeBodyVelocity.magnitude > minSlVel && Mathf.Abs(Phys.wholeBodyVelocity.y) < minSlVel && !midAirSlid) 
                        {
                            Sliding.SetupSlide();
                        }
                    }
                }
            }
        }
    }
}
