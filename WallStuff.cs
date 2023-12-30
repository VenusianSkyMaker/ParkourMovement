using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ParkourMovement;
using static RootMotion.FinalIK.Grounding;

namespace ParkourMovement
{
    static class WallStuff
    {
        static ParkourMovementMod PM;

        public static void Setup()
        {
            PM = ParkourMovementMod.Instance;
        }
        public static void UpdateGroundedVariables()
        {
            if (Mathf.Abs(PM.Phys.wholeBodyVelocity.y) > PM.ungVel)
            {
                PM.framesUngrounded++;
            }
            else
            {
                PM.framesUngrounded = 0;
                PM.isInWR = false;
                PM.CurrWallCheckLength = ParkourMovementMod.normWallCheckLength;
            }
        }

        public static void WallJump()
        {
            PM.pelvis.velocity = Vector3.zero;
            Vector3 Force = new Vector3(PM.head.forward.x * ParkourMovementMod.WJForceMult , 
                PM.head.forward.y * ParkourMovementMod.WJForceMult ,
                PM.head.forward.z * ParkourMovementMod.WJForceMult );

            PM.pelvis.AddForce(Force, ForceMode.VelocityChange);
        }

        public static void WallRun(bool rightSide, RaycastHit hit)
        {
            if(Vector3.Angle(Vector3.down, hit.normal) > 80)
            {
                if (rightSide)
                {
                    if (!PM.isInWR) PM.pelvis.velocity = Vector3.zero; //should slow down player

                    Vector3 sideNorm = new Vector3(-hit.normal.z, 0, hit.normal.x); //check one of two sidenormals, if angle is less than 90deg apply vel in this direction
                    if (Vector3.Angle(sideNorm, PM.head.forward) >= 90)
                    {
                        sideNorm = -sideNorm;//invert sidenorm if its the wrong one
                    }

                    Vector3 Force = new Vector3(-hit.normal.x * PM.WRStickMult, -hit.normal.y * PM.WRStickMult, -hit.normal.z * PM.WRStickMult) + //stick force
                        new Vector3(sideNorm.x * ParkourMovementMod.WRFwdMult, PM.WRUptimeMult, sideNorm.z * ParkourMovementMod.WRFwdMult); //movement force
                    PM.pelvis.AddForce(Force, ForceMode.VelocityChange);
                    PM.pelvis.velocity = Vector3.ClampMagnitude(PM.pelvis.velocity, PM.MaxWRSpeed);
                    PM.isInWR = true;
                }
                else
                {
                    if (!PM.isInWR) PM.pelvis.velocity = Vector3.zero; //should slow down player

                    Vector3 sideNorm = new Vector3(-hit.normal.z, 0, hit.normal.x); //check one of two sidenormals, if angle is less than 90deg apply vel in this direction
                    if (Vector3.Angle(sideNorm, PM.head.forward) >= 90)
                    {
                        sideNorm = -sideNorm;//invert sidenorm if is wrong one
                    }

                    Vector3 Force = new Vector3(-hit.normal.x * PM.WRStickMult, -hit.normal.y * PM.WRStickMult, -hit.normal.z * PM.WRStickMult) + //stick force
                        new Vector3(sideNorm.x * ParkourMovementMod.WRFwdMult, PM.WRUptimeMult, sideNorm.z * ParkourMovementMod.WRFwdMult); //movement force
                    PM.pelvis.AddForce(Force, ForceMode.VelocityChange);
                    PM.pelvis.velocity = Vector3.ClampMagnitude(PM.pelvis.velocity, PM.MaxWRSpeed);
                    PM.isInWR = true;
                }
            }
            
        }
    }
}
