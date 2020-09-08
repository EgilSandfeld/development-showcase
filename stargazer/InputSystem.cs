using UnityEngine;
using Es.InkPainter;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Input System to capture all inputs and forward them
/// </summary>
public class InputSystem : MonoBehaviour
{
    #region Variables

    public static InputSystem Instance;
    public static System.Action<Vector3> OnPlayerRotated;
    public static System.Action OnButton0Trigger;
    public static System.Action<HMDState> OnHMD;

    [Header("Configuration")]
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private Transform ovrCameraRig;
    [SerializeField] private float mouseSensitivity = 200.0f;
    [SerializeField] private float clampAngle = 80.0f;
    [SerializeField] private bool log;
    [SerializeField] private bool gazeDebugRay;
    [SerializeField] private float vectorFilter = 0.8f;
    [SerializeField] private float angularVelocityCaptureThreshold = 2f;
    [SerializeField] private float rayLength = 550;

    [Header("Read Only")]
    [SerializeField] private GameObject currentGazedObject;
    [SerializeField] private GameObject movingTowardsConstellationStarObject;
    [SerializeField] private float angularVelocity;
    [SerializeField] private float angularVelocityFiltered = 0f;
    [SerializeField] private Vector3 gazeDirectionDelta;
    [SerializeField] private Vector3 gazeRealTimePosition;
    [SerializeField] private Vector3 gazeFilteredPosition;
    [SerializeField] private Vector3 gazeFilteredPositionOld;
    [SerializeField] private float rollAngle;
    [SerializeField] private float gazeAngleFromForward;
    [SerializeField] private bool movingTowardsStar;
    [SerializeField] private HMDState hmdState = hmdState.Mounted;
    [SerializeField] private bool gazingAtConstellationStar;

    private Quaternion playerDirectionOld;
    private float starDistance;
    private Vector3 oldCenterEyeForward;
    private Color gazeVectorColor = Color.yellow;
    private Vector3 nextConstellationStarPosition;
    private Ray ray;
    private RaycastHit hit;
    private bool getIndexTriggerLastFrame;
    private CalibrateSystem calibrateSystem;
    private float lastPaintTime;
    private bool captureVelocitiesForTelemetry;
    private int maxVelocityMeasured;
    private int averageVelocityMeasured;
    private List<int> velocitiesCaptured = new List<int>();
    private GameObject raycastObject;
    private ConstellationStar cs;
    private Selectable selectable;
    private Selectable selectable2;
    private int updateTick;

    #endregion

    #region Init

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        calibrateSystem = CalibrateSystem.Instance;

        Cursor.visible = false;
        starDistance = GalaxySystem.Instance.StarDistance;
        gazeRealTimePosition = centerEyeAnchor.forward * starDistance;
        gazeFilteredPosition = gazeRealTimePosition;
        gazeFilteredPositionOld = gazeFilteredPosition;

        ExerciseSystem.OnExerciseStateChanged += OnExerciseStateChanged;
        OVRManager.HMDMounted += OnHMDMounted;
        OVRManager.HMDUnmounted += OnHMDUnmounted;
    }

    //Reset input telemetry when game exercise state changes
    private void OnExerciseStateChanged(bool reset)
    {
        captureVelocitiesForTelemetry = !reset;

        if (log)
            Debug.Log("Exercise state changed to reset: " + reset + " and velocitiesCaptured.Count: " + velocitiesCaptured.Count);

        //When turning off, check if there was any captured velocities that we can make an average from and write to csv
        if (reset && velocitiesCaptured.Count > 0)
        {
            averageVelocityMeasured = (int)velocitiesCaptured.Average();
            if (log)
                Debug.Log("Calculated max velocity in exercise to: " + maxVelocityMeasured.ToString("F1") + " and avg: " + averageVelocityMeasured.ToString("F1"));

            CalibrateSystem.Instance.OnVelocitiesMeasured(maxVelocityMeasured, averageVelocityMeasured);
            averageVelocityMeasured = 0;
            maxVelocityMeasured = 0;
            velocitiesCaptured.Clear();
        }
        else
            velocitiesCaptured.Clear();
    }

    #endregion

    private void Update()
    {
        FetchOVRInput();

#if UNITY_EDITOR
        FetchEditorInput();
#endif

        FetchRaycast();
        CalculateRotation();

        updateTick++;
    }

    private void FixedUpdate()
    {
        //Initialize old vector direction
        if (oldCenterEyeForward == Vector3.zero)
            oldCenterEyeForward = centerEyeAnchor.forward;

        angularVelocity = (Vector3.Distance(centerEyeAnchor.forward, oldCenterEyeForward) * Mathf.Rad2Deg) / Time.fixedDeltaTime;
        oldCenterEyeForward = centerEyeAnchor.forward;

        if (angularVelocityFiltered == 0f)
            angularVelocityFiltered = angularVelocity;

        angularVelocityFiltered = (angularVelocityFiltered * 0.8f) + (angularVelocity * 0.2f);
        gazeRealTimePosition = centerEyeAnchor.forward * starDistance;
        gazeFilteredPosition = (gazeFilteredPosition * vectorFilter) + (gazeRealTimePosition * (1f - vectorFilter));
        
        gazeDirectionDelta = starDistance * (gazeFilteredPosition - gazeFilteredPositionOld);
        if (gazeDebugRay)
            Debug.DrawRay(gazeFilteredPositionOld, gazeDirectionDelta, gazeVectorColor);

        gazeFilteredPositionOld = (gazeFilteredPositionOld * vectorFilter) + (gazeFilteredPosition * (1f - vectorFilter));

        AkSoundEngine.SetRTPCValue("AngularVel", angularVelocityFiltered);
    }

    #region OVR

    private void FetchOVRInput()
    {
        OVRInput.Update();
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.Gamepad))
            OnButton0Trigger?.Invoke();
        else if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) && !getIndexTriggerLastFrame)
        {
            OnButton0Trigger?.Invoke();
            getIndexTriggerLastFrame = true;
        }
        else if (!OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) && getIndexTriggerLastFrame)
            getIndexTriggerLastFrame = false;
    }

    private void OnHMDUnmounted()
    {
        if (hmdState != hmdState.Unmounted)
        {
            hmdState = hmdState.Unmounted;
            OnHMD?.Invoke(hmdState);
            //Debug.Log("Unmounted!");
        }
    }

    private void OnHMDMounted()
    {
        if (hmdState != hmdState.Mounted)
        {
            hmdState = hmdState.Mounted;
            OnHMD?.Invoke(hmdState);
            //Debug.Log("Mounted!");
        }
    }

    #endregion

    #region Editor Input

#if UNITY_EDITOR
    private void FetchEditorInput()
    {
        //Only use mouse in editor and rotate the OVRCameraRig in editor using mouse
        if (Input.GetKey(KeyCode.LeftControl))
            OVRCameraRig.eulerAngles = new Vector3(OVRCameraRig.eulerAngles.x, OVRCameraRig.eulerAngles.y, OVRCameraRig.eulerAngles.z + Input.GetAxis("Mouse X") * MouseSensitivity);
        else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetMouseButton(1))
            OVRCameraRig.eulerAngles = new Vector3(OVRCameraRig.eulerAngles.x - Input.GetAxis("Mouse Y") * MouseSensitivity, OVRCameraRig.eulerAngles.y + Input.GetAxis("Mouse X") * MouseSensitivity, 0);
        if (Input.GetMouseButtonDown(0))
            OnButton0Trigger?.Invoke();
    }
#endif

    #endregion

    #region Raycasting

    private void FetchRaycast()
    {
        // Detecting whether the player gazes upon something
        ray = new Ray(centerEyeAnchor.position, centerEyeAnchor.forward);
        hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, rayLength))
            OnRayHit(hit);
        else
            OnNoRayHit();
    }

    private void OnRayHit(RaycastHit hit)
    {
        // The GameObject the collider is attached to
        raycastObject = hit.collider.gameObject;

        // CONSTELLATION STARS
        cs = raycastObject.GetComponent<ConstellationStar>();
        if (cs != null)
        {
            if (currentGazedObject != raycastObject)
            {
                currentGazedObject = raycastObject;
                gazingAtConstellationStar = true;
                cs.OnFocusedEnter();
                if (gazeDebugRay)
                    Debug.Log("Focused On " + cs.name);
            }

            if (gazeDebugRay)
                Debug.DrawRay(centerEyeAnchor.position, centerEyeAnchor.forward * rayLength, Color.green);
        }
        else
        {
            // SELECTABLES
            selectable = raycastObject.GetComponent<Selectable>();
            if (selectable != null)
            {
                if (currentGazedObject != raycastObject)
                {
                    currentGazedObject = raycastObject;
                    selectable.OnFocusEnter();
                    if (gazeDebugRay)
                        Debug.Log("Focused " + currentGazedObject.name + " with tag: " + raycastObject.tag);
                }
            }
            // OTHER STUFF THAT IS RAYCASTED BECAUSE IT HAS A COLLIDER (COLLIDER SHOULD BE REMOVED OR PROPERLY USED)
            else if (currentGazedObject != raycastObject)
            {
                currentGazedObject = raycastObject;

                if (log)
                    Debug.Log("Focused " + currentGazedObject.name + " with tag: " + raycastObject.tag);
                
                if (gazeDebugRay)
                    Debug.DrawRay(centerEyeAnchor.position, centerEyeAnchor.forward * rayLength, Color.red);
            }
        }
    }

    private void OnNoRayHit()
    {
        if (currentGazedObject != null)
        {
            cs = currentGazedObject.GetComponent<ConstellationStar>();
            if (cs != null)
            {
                cs.OnFocusedExit();
                gazingAtConstellationStar = false;
            }
            else
            {
                selectable = currentGazedObject.GetComponent<Selectable>();
                if (selectable != null)
                    selectable.OnFocusExit();
            }

            if (gazeDebugRay)
                Debug.Log("Focused Off " + currentGazedObject.name);

            currentGazedObject = null;
        }

        cs = null;
        selectable = null;

        if (gazeDebugRay)
            Debug.DrawRay(centerEyeAnchor.position, centerEyeAnchor.forward * rayLength, Color.white);
    }

    #endregion

    #region Rotation

    private void CalculateRotation()
    {
        //Initialize the old value
        if (playerDirectionOld == Quaternion.identity)
        {
            playerDirectionOld = centerEyeAnchor.rotation;
            OnPlayerRotated?.Invoke(centerEyeAnchor.forward);
        }      

        if (centerEyeAnchor.rotation != playerDirectionOld)
        {
            //Calculate Angular Velocity / second
            //AngularVelocity = Quaternion.Angle(PlayerDirectionOld, CenterEyeAnchor.rotation) / Time.deltaTime;
            OnPlayerRotated?.Invoke(centerEyeAnchor.forward);
            playerDirectionOld = centerEyeAnchor.rotation;

            rollAngle = playerDirectionOld.eulerAngles.z;
            if (rollAngle > 180f)
                rollAngle -= 360f;

            gazeAngleFromForward = Vector3.Angle(Vector3.forward, gazeFilteredPosition);
            AkSoundEngine.SetRTPCValue("GazeAngleFromForward", gazeAngleFromForward);

            //
            //Velocity
            //

            if (captureVelocitiesForTelemetry && updateTick % 5 == 0)
            {
                if (angularVelocityFiltered > angularVelocityCaptureThreshold)
                {
                    if (angularVelocityFiltered > maxVelocityMeasured)
                        maxVelocityMeasured = (int)angularVelocityFiltered;

                    velocitiesCaptured.Add((int)angularVelocityFiltered);
                }
            }
        }
    }

    #endregion

    #region Helpers

    public void SetGazeVectorTargeting(bool moveTowardsStar)
    {
        movingTowardsStar = moveTowardsStar;
        gazeVectorColor = movingTowardsStar ? Color.green : Color.yellow;
    }

    public float AngleFromGazeVectorToStar(Vector3 starPosition)
    {
        return Vector3.Angle(gazeDirectionDelta, starPosition - gazeFilteredPositionOld);
    }

    #endregion
}