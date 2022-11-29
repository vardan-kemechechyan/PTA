using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    public TimeOfDay timeOfDay;
    public float changeLightSpeed;

    // Default values is true which means, that crash into another car will result Game Over
    // Level 1 - 4 are set to false
    public bool IsOnCollisionGameOverEnabled = true;

    public Stage[] stages;
    public WheelVehicle[] traffic;
    public TrafficMode trafficMode;

	public enum TimeOfDay 
    {
        Day,
        Evening,
        Night
    }

    public enum TrafficMode 
    {
        None,
        Random
    }

    public Transform startCameraAnchor;

    public List<CarTrailDrawer> ReturnAllTrail()
    {
        List<CarTrailDrawer> AllTrails = new List<CarTrailDrawer>();

		foreach ( var stage in stages )
		{
            AllTrails.Add( stage.start.GetComponent<CarTrailDrawer>() );
        }

        return AllTrails;
    }

    [Serializable]
    public class Stage
    {
        public enum StageColor 
        {
            White,
            Black,
            Red,
            Green,
            Blue,
            Yellow,
            Purple,
            LightBlue
        }

        public StageColor color;
        public WheelVehicle vechicle;
        public Transform start;
        public Transform startCamera;
        public int finish;
    }
}