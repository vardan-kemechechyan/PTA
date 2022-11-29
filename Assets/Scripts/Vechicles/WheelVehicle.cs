/*
 * This code is part of Arcade Car Physics for Unity by Saarg (2018)
 * 
 * This is distributed under the MIT Licence (see LICENSE.md for details)
 */
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VehicleBehaviour;

#if MULTIOSCONTROLS
    using MOSC;
#endif

[RequireComponent(typeof(Rigidbody))]
public class WheelVehicle : MonoBehaviour
{
    [SerializeField] TargetIndicator targetIndicator;
    [SerializeField] MeshRenderer carBodyRenderer;

    [SerializeField] VechiclePath vechiclePath;
    List<Transform> vechiclePathNodes;
    int currentVechiclePathNode = 0;

    bool IsOnCollisionGameOverEnabled;

    [SerializeField] CameraController.CameraShakeMode cameraShakeMode;

    public enum Control 
    {
        Player,
        Ghost,
        Traffic
    }

    [SerializeField] ParticleSystem crashSmoke;
    [SerializeField] ParticleSystem[] smoke;

    [SerializeField] Transform[] trafficWaypoints;

    [SerializeField] SpriteRenderer emoji;

    public Control control;

    public bool isCrashed;

    [SerializeField] float stopSpeed = 1.0f;

    public bool waitForStart;
    public float starterTime = 1.0f;

    public Transform chaseTransform;

    [Header("Inputs")]
#if MULTIOSCONTROLS
        [SerializeField] PlayerNumber playerId;
#endif
    // If isPlayer is false inputs are ignored
    [SerializeField] bool isPlayer = true;
    public bool IsPlayer { get { return isPlayer; } set { isPlayer = value; } }

    // Input names to read using GetAxis
    [SerializeField] string throttleInput = "Throttle";
    [SerializeField] string brakeInput = "Brake";
    [SerializeField] string turnInput = "Horizontal";
    [SerializeField] string jumpInput = "Jump";
    [SerializeField] string driftInput = "Drift";
    [SerializeField] string boostInput = "Boost";

    public Transform cameraAnchor;

    [SerializeField] float input;

    [SerializeField] Track currentTrack;
    public Track CurrentTrack
    {
        get => currentTrack;
        set
        {
            currentTrack = value;
        }
    }

    public Vector3 trackPosition;
    public Quaternion trackRotation;

    public Quaternion trackFlRrotation;
    public Quaternion trackFrRrotation;
    public Quaternion trackBlRrotation;
    public Quaternion trackBrRrotation;

    public float trackThrottle;

    int currentTrackMove;

    public CarTrailDrawer CarTrail;

    GameObject lights;

    [SerializeField] bool move;
    public bool Move 
    {
        get => move;
        set 
        {
            if (value)
            {
                ShowEmoji(false);

                shakeCamera = false;

                isCrashed = false;
                Handbrake = false;
                vechicleWarning = false;

                if (vechiclePath != null)
                    waypointMoveDelay = vechiclePath.startDelay;

                if (waitForStart) 
                {
                    StopCoroutine("StarterDelay");
                    StartCoroutine("StarterDelay");
                }

                switch (control)
                {
                    case Control.Player:
                        RecordTrack(true);

                        StopCoroutine("CheckIfStuck");
                        StartCoroutine("CheckIfStuck");
                        break;
                    case Control.Ghost:
                        PlayTrack(true);
                        break;
                }
            }
            else 
            {
                Handbrake = true;

                StopCoroutine("CheckIfStuck");

                switch (control)
                {
                    case Control.Player:
                        RecordTrack(false);
                        break;
                    case Control.Ghost:
                        PlayTrack(false);
                        break;
                }
            }

            move = value;
        }
    }

    WaitForSeconds starterDelay;

    bool isStarterOn;

    [SerializeField] float antiRoll = 1000.0f;

    IEnumerator StarterDelay()
    {
        isStarterOn = false;
        yield return starterDelay;
        isStarterOn = true;
    }

    public void SetColor(Color color) 
    {
        if(carBodyRenderer)
            carBodyRenderer.material.SetColor("_Color", color);
        //carBodyRenderer.material.SetColor("_BaseColor", color);

        if(control == Control.Player)
            CarTrail.SetTrailRendererMaterialColor( color );
    }

    public void ClearParticles() 
    {
        crashSmoke.Clear();

        foreach(var s in smoke)
            s.Clear();
    }

    public void SetTarget(Transform taget) 
    {
        targetIndicator.SetTarget(taget);
    }

    public void OnStartStage() 
    {
        targetIndicator.PlayStartAnimation();
    }

    public void StopImmediately() 
    {
        _rb.isKinematic = true;
        _rb.velocity = Vector3.zero;
        _rb.isKinematic = false;
    }

    /* 
     *  Turn input curve: x real input, y value used
     *  My advice (-1, -1) tangent x, (0, 0) tangent 0 and (1, 1) tangent x
     */
    [SerializeField] AnimationCurve turnInputCurve = AnimationCurve.Linear(-1.0f, -1.0f, 1.0f, 1.0f);

    [Header("Wheels")]
    [SerializeField] WheelCollider[] driveWheel;
    public WheelCollider[] DriveWheel { get { return driveWheel; } }
    [SerializeField] WheelCollider[] turnWheel;

    public WheelCollider[] TurnWheel { get { return turnWheel; } }

    // This code checks if the car is grounded only when needed and the data is old enough
    bool isGrounded = false;
    int lastGroundCheck = 0;
    public bool IsGrounded
    {
        get
        {
            if (lastGroundCheck == Time.frameCount)
                return isGrounded;

            lastGroundCheck = Time.frameCount;
            isGrounded = true;
            foreach (WheelCollider wheel in wheels)
            {
                if (!wheel.gameObject.activeSelf || !wheel.isGrounded)
                    isGrounded = false;
            }
            return isGrounded;
        }
    }

    [Header("Behaviour")]
    /*
     *  Motor torque represent the torque sent to the wheels by the motor with x: speed in km/h and y: torque
     *  The curve should start at x=0 and y>0 and should end with x>topspeed and y<0
     *  The higher the torque the faster it accelerate
     *  the longer the curve the faster it gets
     */
    [SerializeField] AnimationCurve motorTorque = new AnimationCurve(new Keyframe(0, 200), new Keyframe(50, 300), new Keyframe(200, 0));

    // Differential gearing ratio
    [Range(2, 16)]
    [SerializeField] float diffGearing = 4.0f;
    public float DiffGearing { get { return diffGearing; } set { diffGearing = value; } }

    // Basicaly how hard it brakes
    [SerializeField] float brakeForce = 1500.0f;
    public float BrakeForce { get { return brakeForce; } set { brakeForce = value; } }

    // Max steering hangle, usualy higher for drift car
    [Range(0f, 50.0f)]
    [SerializeField] float steerAngle = 30.0f;
    public float SteerAngle { get { return steerAngle; } set { steerAngle = Mathf.Clamp(value, 0.0f, 50.0f); } }

    // The value used in the steering Lerp, 1 is instant (Strong power steering), and 0 is not turning at all
    [Range(0.001f, 1.0f)]
    [SerializeField] float steerSpeed = 0.2f;
    public float SteerSpeed { get { return steerSpeed; } set { steerSpeed = Mathf.Clamp(value, 0.001f, 1.0f); } }

    // How hight do you want to jump?
    [Range(1f, 1.5f)]
    [SerializeField] float jumpVel = 1.3f;
    public float JumpVel { get { return jumpVel; } set { jumpVel = Mathf.Clamp(value, 1.0f, 1.5f); } }

    // How hard do you want to drift?
    [Range(0.0f, 2f)]
    [SerializeField] float driftIntensity = 1f;
    public float DriftIntensity { get { return driftIntensity; } set { driftIntensity = Mathf.Clamp(value, 0.0f, 2.0f); } }

    // Reset Values
    Vector3 spawnPosition;
    Quaternion spawnRotation;

    /*
     *  The center of mass is set at the start and changes the car behavior A LOT
     *  I recomment having it between the center of the wheels and the bottom of the car's body
     *  Move it a bit to the from or bottom according to where the engine is
     */
    [SerializeField] Transform centerOfMass;

    // Force aplied downwards on the car, proportional to the car speed
    [Range(0.5f, 10f)]
    [SerializeField] float downforce = 1.0f;
    public float Downforce { get { return downforce; } set { downforce = Mathf.Clamp(value, 0, 5); } }

    // When IsPlayer is false you can use this to control the steering
    float steering;
    public float Steering { get { return steering; } set { steering = Mathf.Clamp(value, -1f, 1f); } }

    // When IsPlayer is false you can use this to control the throttle
    float throttle;
    public float Throttle { get { return throttle; } set { throttle = Mathf.Clamp(value, -1f, 1f); } }

    // Like your own car handbrake, if it's true the car will not move
    [SerializeField] bool handbrake;
    public bool Handbrake { get { return handbrake; } set { handbrake = value; } }

    // Use this to disable drifting
    [HideInInspector] public bool allowDrift = true;
    bool drift;
    public bool Drift { get { return drift; } set { drift = value; } }

    // Use this to read the current car speed (you'll need this to make a speedometer)
    [SerializeField] float speed = 0.0f;
    public float Speed { get { return speed; } }

    [Header("Particles")]
    // Exhaust fumes
    [SerializeField] ParticleSystem[] gasParticles;

    [Header("Boost")]
    // Disable boost
    [HideInInspector] public bool allowBoost = true;

    // Maximum boost available
    [SerializeField] float maxBoost = 10f;
    public float MaxBoost { get { return maxBoost; } set { maxBoost = value; } }

    // Current boost available
    [SerializeField] float boost = 10f;
    public float Boost { get { return boost; } set { boost = Mathf.Clamp(value, 0f, maxBoost); } }

    // Regen boostRegen per second until it's back to maxBoost
    [Range(0f, 1f)]
    [SerializeField] float boostRegen = 0.2f;
    public float BoostRegen { get { return boostRegen; } set { boostRegen = Mathf.Clamp01(value); } }

    /*
     *  The force applied to the car when boosting
     *  NOTE: the boost does not care if the car is grounded or not
     */
    [SerializeField] float boostForce = 5000;
    public float BoostForce { get { return boostForce; } set { boostForce = value; } }

    // Use this to boost when IsPlayer is set to false
    public bool boosting = false;
    // Use this to jump when IsPlayer is set to false
    public bool jumping = false;

    // Boost particles and sound
    [SerializeField] ParticleSystem[] boostParticles;
    [SerializeField] AudioClip boostClip;
    [SerializeField] AudioSource boostSource;
    [SerializeField] AudioSource hornSource;
    // Private variables set at the start
    Rigidbody _rb;
    WheelCollider[] wheels;

    Vector3 initPos;
    Quaternion initRot;

    bool initialized;

    Dictionary<float, float> throttleOverDistance = new Dictionary<float, float>();

    // Init rigidbody, center of mass, wheels and more
    void Start()
    {
        IsOnCollisionGameOverEnabled = FindObjectOfType<Level>().IsOnCollisionGameOverEnabled;

        lights = transform.Find("Lights").gameObject;

        ShowEmoji(false);

        throttleOverDistance.Add(10f, 1.0f);
        throttleOverDistance.Add(5f, 0.25f);

        initPos = transform.position;
        initRot = transform.rotation;

        if (control == Control.Traffic) 
        {
            Transform[] pathTransforms = vechiclePath.GetComponentsInChildren<Transform>();

            vechiclePathNodes = new List<Transform>();

            for (int i = 0; i < pathTransforms.Length; i++)
            {
                if (pathTransforms[i] != vechiclePath.transform)
                    vechiclePathNodes.Add(pathTransforms[i]);
            }
        }

        starterDelay = new WaitForSeconds(starterTime);

#if MULTIOSCONTROLS
            Debug.Log("[ACP] Using MultiOSControls");
#endif
        if (boostClip != null)
        {
            boostSource.clip = boostClip;
        }

        boost = maxBoost;

        _rb = GetComponent<Rigidbody>();

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (_rb != null && centerOfMass != null)
        {
            _rb.centerOfMass = centerOfMass.localPosition;
        }

        wheels = GetComponentsInChildren<WheelCollider>();

        // Set the motor torque to a non null value because 0 means the wheels won't turn no matter what
        foreach (WheelCollider wheel in wheels)
        {
            wheel.motorTorque = 0.0001f;
        }

        //StartCoroutine(Test());

        initialized = true;
    }

    float checkVechiclesTime;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 5.0f);
    }

    bool vechicleWarning;

    private void CheckNearVechiles()
    {
        if (!vechicleWarning) 
        {
            var vechicles = Physics.OverlapSphere(transform.position, 5.0f, 1 << LayerMask.NameToLayer("Vechicle"));

            if (vechicles.Length > 1) 
            {
                vechicleWarning = true;

                ShowEmoji(true);
                hornSource.Play();
            }
        }
    }

    private void ShowEmoji(bool show) 
    {
        emoji.gameObject.SetActive(false);
        StopCoroutine("ShowEmojiCoroutine");

        if (show)
        {
            StartCoroutine("ShowEmojiCoroutine");
        }
    }

    WaitForSeconds emojiDelay = new WaitForSeconds(2.0f);

    IEnumerator ShowEmojiCoroutine() 
    {
        emoji.gameObject.SetActive(true);
        yield return emojiDelay;
        emoji.gameObject.SetActive(false);
    }

    public void ResetPath() 
    {
        currentVechiclePathNode = 0;
    }

    public void ResetToStartPosition() 
    {
        if (!initialized) return;

        transform.position = initPos;
        transform.rotation = initRot;
    }

    IEnumerator Test()
    {
        Move = true;
        yield return new WaitForSeconds(10.0f);
        Move = false;

        control = Control.Ghost;
        Move = true;
    }

    bool shakeCamera;

    void Update()
    {
        if (move && waypointMoveDelay > 0)
            waypointMoveDelay -= Time.deltaTime;

        if (lights) 
        {
            if (lights.activeInHierarchy && GameManager.GetInstance().MainLight.intensity > 0.5f)
                lights.SetActive(false);
            else if(!lights.activeInHierarchy && GameManager.GetInstance().MainLight.intensity <= 0.5f)
                lights.SetActive(true);
        }

        foreach ( ParticleSystem gasParticle in gasParticles)
        {
            gasParticle.Play();
            ParticleSystem.EmissionModule em = gasParticle.emission;
            em.rateOverTime = handbrake ? 0 : Mathf.Lerp(em.rateOverTime.constant, Mathf.Clamp(150.0f * throttle, 30.0f, 100.0f), 0.1f);
        }

        if (isPlayer && allowBoost)
        {
            boost += Time.deltaTime * boostRegen;
            if (boost > maxBoost) { boost = maxBoost; }
        }

        if (control == Control.Player)
        {
            if (speed > 20)
            {
                CameraController.GetInstance().zoom = 1.5f;

                if (!shakeCamera) 
                {
                    shakeCamera = true;
                    CameraController.GetInstance().CameraShake(6.0f, cameraShakeMode);
                }
            }
            else 
            {
                CameraController.GetInstance().zoom = 0.8f;
            }

            if (Input.GetMouseButton(0) && GameManager.GetInstance().IsTurning)
            {
                if (Input.mousePosition.x > Screen.width / 2) input = 1;
                else input = -1;

                CameraController.GetInstance().ApplyShifting(input);
            }

            if (Input.GetMouseButtonUp(0))
            {
                input = 0;
                CameraController.GetInstance().ApplyShifting(0);
            }

            GameManager.GetInstance().Steer(Mathf.CeilToInt(input));
        }
        else 
        {
            checkVechiclesTime -= Time.deltaTime;

            if (checkVechiclesTime <= 0)
            {
                checkVechiclesTime = 0.25f;
                CheckNearVechiles();
            }
        }
    }

    void FixedUpdate()
    {
        if (!move)
            _rb.velocity = Vector3.Lerp(_rb.velocity, Vector3.zero, stopSpeed * Time.fixedDeltaTime);

        // Mesure current speed
        speed = transform.InverseTransformDirection(_rb.velocity).z * 3.6f;

        // Get all the inputs!
        if (isPlayer)
        {
            if (move)
            {
                if (!IsOnRoad())
                {
                    GameManager.GetInstance().Crash();
                }
            }

            // Accelerate & brake
            if (throttleInput != "" && throttleInput != null)
            {
                //throttle = GetInput(throttleInput) - GetInput(brakeInput);
            }

            if (waitForStart && isStarterOn && move)
                throttle = 1f;
            else if (!waitForStart && move) throttle = 1f;
            else throttle = 0;

            // Boost
            //boosting = (GetInput(boostInput) > 0.5f);
            // Turn
            steering = turnInputCurve.Evaluate(GetInput(turnInput)) * steerAngle;
            // Dirft
            //drift = GetInput(driftInput) > 0 && _rb.velocity.sqrMagnitude > 100;
            // Jump
            //jumping = GetInput(jumpInput) != 0;
        }

        if (control == Control.Traffic)
        {
            SteerToWaypoint();
            CheckWaypoint();

            if (waypointMoveDelay > 0)
            {
                throttle = 0;
            }
            else 
            {
                if (move) throttle = throttleOverDistance.
                    OrderBy(x => Math.Abs(Vector3.Distance(
                        transform.position,
                        vechiclePathNodes[currentVechiclePathNode].position) - x.Key)).
                        First().Value;

                else throttle = 0;
            }
        }

        // Direction
        foreach (WheelCollider wheel in turnWheel)
        {
            wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, steering, steerSpeed);
        }

        foreach (WheelCollider wheel in wheels)
        {
            wheel.brakeTorque = 0;
        }

        // Handbrake
        if (handbrake)
        {
            foreach (WheelCollider wheel in wheels)
            {
                // Don't zero out this value or the wheel completly lock up
                wheel.motorTorque = 0.0001f;
                wheel.brakeTorque = brakeForce;
            }
        }
        else if (Mathf.Abs(speed) < 4 || Mathf.Sign(speed) == Mathf.Sign(throttle))
        {
            foreach (WheelCollider wheel in driveWheel)
            {
                if (control == Control.Traffic) wheel.motorTorque = throttle * 800;
                else wheel.motorTorque = throttle * motorTorque.Evaluate(speed) * diffGearing / driveWheel.Length;
            }
        }
        else
        {
            foreach (WheelCollider wheel in wheels)
            {
                wheel.brakeTorque = Mathf.Abs(throttle) * brakeForce;
            }
        }

        // Jump
        if (jumping && isPlayer)
        {
            if (!IsGrounded)
                return;

            _rb.velocity += transform.up * jumpVel;
        }

        // Boost
        if (boosting && allowBoost && boost > 0.1f)
        {
            _rb.AddForce(transform.forward * boostForce);

            boost -= Time.fixedDeltaTime;
            if (boost < 0f) { boost = 0f; }

            if (boostParticles.Length > 0 && !boostParticles[0].isPlaying)
            {
                foreach (ParticleSystem boostParticle in boostParticles)
                {
                    boostParticle.Play();
                }
            }

            if (boostSource != null && !boostSource.isPlaying)
            {
                boostSource.Play();
            }
        }
        else
        {
            if (boostParticles.Length > 0 && boostParticles[0].isPlaying)
            {
                foreach (ParticleSystem boostParticle in boostParticles)
                {
                    boostParticle.Stop();
                }
            }

            if (boostSource != null && boostSource.isPlaying)
            {
                boostSource.Stop();
            }
        }

        // Drift
        if (drift && allowDrift)
        {
            Vector3 driftForce = -transform.right;
            driftForce.y = 0.0f;
            driftForce.Normalize();

            if (steering != 0)
                driftForce *= _rb.mass * speed / 7f * throttle * steering / steerAngle;
            Vector3 driftTorque = transform.up * 0.1f * steering / steerAngle;


            _rb.AddForce(driftForce * driftIntensity, ForceMode.Force);
            _rb.AddTorque(driftTorque * driftIntensity, ForceMode.VelocityChange);
        }

        // Downforce
        _rb.AddForce(-transform.up * speed * downforce);

        AntiRoll();
    }

    Vector3 waypointRelativeVector;
    float waypointMoveDelay;

    private void SteerToWaypoint() 
    {
        waypointRelativeVector = transform.
            InverseTransformPoint(vechiclePathNodes[currentVechiclePathNode].position);

        steering = (waypointRelativeVector.x / waypointRelativeVector.magnitude) * steerAngle;
    }

    [SerializeField] float dist;

    private void CheckWaypoint() 
    {
        var a = transform.position;
        var b = vechiclePathNodes[currentVechiclePathNode].position;

        b.y = a.y;

        if (Vector3.Distance(a, b) < 10.0f) 
        {
            if (currentVechiclePathNode == vechiclePathNodes.Count - 1) 
            {
                //currentVechiclePathNode = 0;
                Move = false;
            }
            else
                currentVechiclePathNode++;
        }
    }

    private void AntiRoll() 
    {
        WheelHit hit;
        float travelL = 1.0f;
        float travelR = 1.0f;

        var wheelL = wheels[0];
        var wheelR = wheels[1];

        bool groundedL = wheelL.GetGroundHit(out hit);

        if (groundedL)
            travelL = (-wheelL.transform.InverseTransformPoint(hit.point).y - wheelL.radius) / wheelL.suspensionDistance;

        bool groundedR = wheelR.GetGroundHit(out hit);

        if (groundedR)
            travelR = (-wheelR.transform.InverseTransformPoint(hit.point).y - wheelR.radius) / wheelR.suspensionDistance;

        float antiRollForce = (travelL - travelR) * antiRoll;

        if (groundedL)
            _rb.AddForceAtPosition(wheelL.transform.up * -antiRollForce, wheelL.transform.position);

        if (groundedR)
            _rb.AddForceAtPosition(wheelR.transform.up * antiRollForce, wheelR.transform.position);
    }

    WheelHit roadHit;

    public bool IsOnRoad() 
    {
        foreach (var w in wheels) 
        {
            w.GetGroundHit(out roadHit);

            if (roadHit.collider != null && !roadHit.collider.CompareTag("Road"))
                if( roadHit.collider.name == "Level" )
                    return false;
        }

        return true;
    }

    public void CrashSmoke(bool enabled) 
    {
        if (enabled) 
        {
            crashSmoke.Play();
        }
        else
        {
            crashSmoke.Stop();
            crashSmoke.Clear();
        }
    }

    public void Crash() 
    {
        isCrashed = true;

        Move = false;

        if (control == Control.Player) 
        {
            GameManager.GetInstance().Crash();
        }
    }

    float lastCollisionMessage;

    public void NonCrashCollision() 
    {
        if (control == Control.Player && (Time.time - lastCollisionMessage) > 5.0f) 
        {
            lastCollisionMessage = Time.time;

            GameManager.GetInstance().PlayerToast("!!!WOOOW!!!");
        }
    }

    WaitForSeconds checkIfStuckDelay = new WaitForSeconds(1.0f);

    IEnumerator CheckIfStuck() 
    {
        while (true) 
        {
            yield return checkIfStuckDelay;

            if (_rb.velocity.magnitude <= 0.05f)
                GameManager.GetInstance().Crash();
        }
    }

    // Reposition the car to the start position
    public void ResetPos()
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    public void toogleHandbrake(bool h)
    {
        handbrake = h;
    }

    // MULTIOSCONTROLS is another package I'm working on ignore it I don't know if it will get a release.
#if MULTIOSCONTROLS
        private static MultiOSControls _controls;
#endif

    // Use this method if you want to use your own input manager
    private float GetInput(string input)
    {
        //#if MULTIOSCONTROLS
        //        return MultiOSControls.GetValue(input, playerId);
        //#else
        //        return Input.GetAxis(input);
        //#endif

        return this.input;
    }

    public void RecordTrack(bool record) 
    {
        if (record)
        {
            CurrentTrack = new Track();

            StopCoroutine("RecordTrackCoroutine");
            StartCoroutine("RecordTrackCoroutine");
        }
        else 
        {
            if (trackMoves != null)
                CurrentTrack.moves = trackMoves.ToArray();

            StopCoroutine("RecordTrackCoroutine");
        }
    }

    WaitForEndOfFrame endFrame = new WaitForEndOfFrame();

    new List<Track.Move> trackMoves;

    IEnumerator RecordTrackCoroutine()
    {
        trackMoves = new List<Track.Move>(); 

        while (true)
        {
            trackMoves.Add(new Track.Move
            {
                position = transform.position,
                rotation = transform.rotation,

                flRrotation = wheels[0].transform.rotation,
                frRrotation = wheels[1].transform.rotation,
                blRrotation = wheels[2].transform.rotation,
                brRrotation = wheels[3].transform.rotation,

                throttle = throttle
            });

            //TODO: Trail draws here
            CarTrail.SetTrailRendererNewPosition( transform.position );

            yield return endFrame;
        }
    }

    public void PlayTrack(bool play) 
    {
        if (play)
        {
            StopCoroutine("PlayTrackCoroutine");
            StartCoroutine("PlayTrackCoroutine");
        }
        else 
        {
            isPlayTrack = false;
            StopCoroutine("PlayTrackCoroutine");
        }
    }

    [SerializeField] bool isPlayTrack;

    IEnumerator PlayTrackCoroutine() 
    {
        for (int i = 0; i < CurrentTrack.moves.Length; i++)
        {
            int n = 0;

            if (i > 0)
                n = i - 1;

            transform.position = CurrentTrack.moves[i].position;
            transform.rotation = CurrentTrack.moves[i].rotation;

            wheels[0].transform.rotation = CurrentTrack.moves[i].flRrotation;
            wheels[1].transform.rotation = CurrentTrack.moves[i].frRrotation;
            wheels[2].transform.rotation = CurrentTrack.moves[i].blRrotation;
            wheels[3].transform.rotation = CurrentTrack.moves[i].brRrotation;

            throttle = Mathf.Lerp(throttle, trackThrottle, 0.5f);

            yield return null;
        }

        isPlayTrack = false;

        Move = false;
    }

    public class Track
    {
        public Move[] moves;

        public class Move
        {
            public Vector3 position;
            public Quaternion rotation;

            public Quaternion flRrotation;
            public Quaternion frRrotation;
            public Quaternion blRrotation;
            public Quaternion brRrotation;

            public float throttle;
        }
    }
}
