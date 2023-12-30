using SLZ.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RootMotion.FinalIK.Grounding;
using UnityEngine;

namespace ParkourMovement
{
    static class Sliding
    {
        static ParkourMovementMod PM;
        public static void Setup()
        {
            PM = ParkourMovementMod.Instance;
        }
        public static void SetupSlide()
        {
            PM.isInSlide = true;
            PM.SlidingSeat = GameObject.Instantiate(ParkourMovementMod.SlidingSeatObj).GetComponent<Seat>();
            PM.SlidingSeat.seatRb = PM.SlidingSeat.gameObject.GetComponent<Rigidbody>();

            PM.SlidingSeat.transform.position = PM.pelvis.position;
            PM.dir = PM.head.transform.forward;
            PM.dir = new Vector3(PM.dir.x, 0, PM.dir.z); //make dir parallel to ground
            PM.currSlVel = 0;
            PM.SlidingSeat.transform.eulerAngles = new Vector3(70, 0, 0); //has to be done before or it will rotate the head too
            PM.SlidingSeat.Register(PM.RM);
            PM.SlidingSeat.transform.forward = PM.head.transform.forward;
            PM.SlidingSeat.transform.eulerAngles = new Vector3(0, PM.SlidingSeat.transform.eulerAngles.y, PM.SlidingSeat.transform.eulerAngles.z); //adjust rotations with rigs
            PM.SlEndTime = Time.time + 2;
        }

        public static void EndSlide()
        {
            PM.isInSlide = false;
            PM.pelvis.velocity = Vector3.zero;
            PM.SlidingSeat.DeRegister();
            GameObject.Destroy(PM.SlidingSeat.gameObject);
        }

        public static void Slide()
        {
            PM.currSlVel = Mathf.Clamp(PM.currSlVel + PM.seatVelIncr * Time.deltaTime, 0, ParkourMovementMod.maxSlVel);
            Vector3 t = PM.SlidingSeat.transform.position + PM.currSlVel * PM.dir;
            if (!Physics.Raycast(PM.SlidingSeat.transform.position + new Vector3(0, .3f, 0), Vector3.down, 0.5f, PM.WallMask))
            {
                t.y -= PM.seatGrav * Time.deltaTime;
                PM.midAirSlid = true;
            }

            PM.SlidingSeat.seatRb.MovePosition(t); //note: retry with physics based seat
        }
    }
}
