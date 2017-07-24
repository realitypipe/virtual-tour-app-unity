/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.2 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.2

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Runtime.CompilerServices;

using System.Runtime.InteropServices; // required for DllImpor


public class OVRInspector : MonoBehaviour
{
    // Singleton instance
    public static OVRInspector instance { get; private set; }

    // Tweakable values
    [Header("Display parameters")]
    [Tooltip("Default distance from eye to GUI")]
    public float eyeToGUIDistance = 1.5f;
    [Tooltip("Separation between buttons in GUI")]
    public float buttonSpacing = 9;
    [Tooltip("Safety factor applied to ensure GUI is not drawn inside world geometry. 1 is minimum.")]
    public float guiGeometryClearanceScale = 0.75f;
    [Tooltip("Minimum scale of UI")]
    public float minimumScale = 0.1f;

    [Header("Show/Hide Controls")]
    [Tooltip("Key to show UI")]
    public KeyCode showKey = KeyCode.Space;
    [Tooltip("Key to hide UI")]
    public KeyCode hideKey = KeyCode.Escape;
    [Tooltip("Gamepad button to show/hide UI")]
    public OVRInput.Button showButton = OVRInput.Button.Start;

    [Header("UI Materials")]
    [Tooltip("Material used for UI elements")]
    public Material uiMaterial;
    [Tooltip("Material used for Font elements")]
    public Material uiFontMaterial;

    [Header("Misc")]
    [Tooltip("Show a close button to allow the user to hide to UI")]
    public bool allowClose = true;

    [Tooltip("Interval at which FPS is recalculated")]
    public float frameRateUpdateInterval = 1.5f;

    public float frameRate { get { return lastFPSValue; } }


    [Serializable]
    public class InspectorShowEvent : UnityEvent { }

    [Serializable]
    public class InspectorHideEvent : UnityEvent { }
    [Header("Events")]
    [SerializeField]
    public OVRInspector.InspectorShowEvent onInspectorShow = new OVRInspector.InspectorShowEvent();

    [SerializeField]
    public OVRInspector.InspectorHideEvent onInspectorHide = new OVRInspector.InspectorHideEvent();



    #region DisplayModeProperties
    /// <summary>
    /// Properties related to fading world when UI is displayed
    /// </summary>
    public enum UIFadeMode
    {
        never,
        whenNoRoom,
        always
    };
    UIFadeMode _uiFadeMode = UIFadeMode.whenNoRoom;
    /// <summary>
    /// When to fade the backgrond behind UI
    /// </summary>
    public UIFadeMode uiFadeMode
    {
        get
        {
            return _uiFadeMode;
        }
        set
        {
            _uiFadeMode = value;
            UpdateUIFade();
        }
    }

    float _uiFadeLevel = 0.9f;
    /// <summary>
    /// The alpha level to use when fading the background to black. 1 will fully blackout the background
    /// </summary>
    public float uiFadeLevel
    {
        get
        {
            return _uiFadeLevel;
        }
        set
        {
            _uiFadeLevel = value;
            UpdateUIFade();
        }
    }

    bool _drawOverEverything = true;
    /// <summary>
    /// No depth testing when drawing UI
    /// </summary>
    public bool drawOverEverything
    {
        get { return _drawOverEverything; }
        set
        {
            _drawOverEverything = value;
            SetUIMaterials(canvas.transform);
            UpdateUIMaterials();
            UpdateUIFade();
        }
    }

    bool _autoDistance = false;
    /// <summary>
    /// If true the UI will automatically be scaled to not intersect with world geometry
    /// </summary>
    public bool autoDistance
    {
        get
        {
            return _autoDistance;
        }
        set
        {
            _autoDistance = value;
            float rotationAroundPlayer = GetCurrentRotationAroundPlayer();
            Hide();
            Show(rotationAroundPlayer);
        }
    }
    #endregion
    /// <summary>
    /// fader object for sccreen in and out
    /// </summary>
    public OVRScreenFade2 fader
    {
        get;

        private set;
    }

    // OVR SDK Objects for convenience
    public OVRPlayerController playerController { get; private set; }
    static public OVRCameraRig cameraRig 
    {
        get
        {
            return GameObject.Find("OVRCameraRig").GetComponent<OVRCameraRig>();
        }
    }
    public GameObject leftCamera { get; private set; }
    public GameObject rightCamera { get; private set; }
    public OVRManager manager { get; private set; }
    public Transform centerEyeTransform { get; private set; }
    
    
    
    // GUI Canvas and Panels
    GameObject canvas;
    OVRInspectorPanelBuilder centerPanel;
    OVRInspectorPanelBuilder leftPanel;
    OVRInspectorPanelBuilder rightPanel;
    OVRInspectorPanelBuilder currentPointerPanel;
    OVRInspectorPanelBuilder controlsPanel;
    OVRDiscomfortWarning discomfortWarning;
    GameObject docsPanel;
    OVRGazeScroller docsScroller;
    Text docsPanelText;
    string docText = "";

    /// <summary>
    /// Layers that are ignored when working out whether UI will intersect geometry after being summoned
    /// </summary>
    private int summoningIgnoreLayers = 0;


    //Button references for disabling/enabling specific buttons 
    private Button recenterButton;
    private Button lastSelectedContextButton;

    //Cache of playcontroller state
    private bool previouslySkippingMouseRotation = true;
    private bool previouslyHaltedMovementUpdate = true;

    // Unity layer containing the player collider
    private int playerLayer;

    // Current max possible scale for Inspector without intersecting world geometry
    private float maxInspectorScale = 1;
    //Was there room for the inspector without intersecting world geometry
    private bool roomForInspector = false;

    // Cache of position and scale of GUI relative to camera - for smooth UI scaling
    private Vector3 cachedPosition;
    private float cachedScaleRatio;

    // Mouse pointer
    private OVRMousePointer pointer;
    private Vector2 mousePos;

    // Input module
    private OVRInputModule inputModule;

    // Prefabs
    private EventSystem eventSystemPrefab;
    private Button buttonPrefab;
    private Button folderPrefab;

    // Frame rate calculation
    private float lastFrameRateUpdateTime = 0;
    private int lastFrameIndex = 0;
    private float lastFPSValue = 0;
    

    // These all need to start ONLY_ because they are all removed. If a node isn't filtered out
    // then the node is still removed, but its children are reparented to the nodes parent. This
    // is necessary for LayoutElements to behave correctly
    private const string filterPrefix = "ONLY_";
#if UNITY_ANDROID 
    private const string nodeToCull = "ONLY_RIFT";
#elif UNITY_STANDALONE
    private const string nodeToCull = "ONLY_GEARVR";
#else
    private const string nodeToCull = "ALWAYS_CULL";
#endif

    #region MenuContextState
    OVRInspectorContextDetails currentContext;          //Currently selected context
    List<OVRInspectorContextDetails> contextList = new List<OVRInspectorContextDetails>();       //List of contexts currently present in the inspector
    List<OVRInspectorContextDetails> sceneSpecificContexts = new List<OVRInspectorContextDetails>();  //Contexts that are scene specific and are temporarily loaded
    bool menuActive = false;
    #endregion


    // Methods 

    public bool IsMenuActive()
    {
        return menuActive;
    }


#region StartUpFunctions 
    void Awake()
    {
        Debug.Log(string.Format("OVRInspector Awake", 0));

        playerLayer = GetLayerOrReportError("Player");

        if (instance != null)
        {
            Debug.LogError("Existing OVRInspector");
            GameObject.Destroy(gameObject);
            return;
        }
        instance = this;


        UpdateUIMaterials();

        //Find prefabs
        buttonPrefab = (Button)Resources.Load("Prefabs/Button", typeof(Button));
        folderPrefab = (Button)Resources.Load("Prefabs/Folder", typeof(Button));
        eventSystemPrefab = (EventSystem)Resources.Load("Prefabs/EventSystem", typeof(EventSystem));

      

        // Setup canvas and canvas panel builders 
        canvas = transform.Find("Canvas").gameObject;

        leftPanel = new OVRInspectorPanelBuilder(canvas.transform.Find("LeftPanel").gameObject);
        rightPanel = new OVRInspectorPanelBuilder(canvas.transform.Find("RightPanel").gameObject);
        centerPanel = new OVRInspectorPanelBuilder(canvas.transform.Find("CenterPanel").gameObject);

        docsPanel = rightPanel.panel.transform.Find("DocsPanel").gameObject;
        controlsPanel = centerPanel;
        docsPanelText = docsPanel.GetComponentInChildren<Text>();
        docsScroller = docsPanel.GetComponentInChildren<OVRGazeScroller>();

        // Setup links between panels for continous mouse movement
        leftPanel.rightPanel = centerPanel;
        rightPanel.leftPanel = centerPanel;
        centerPanel.leftPanel = leftPanel;
        centerPanel.rightPanel = rightPanel;

        discomfortWarning = GetComponent<OVRDiscomfortWarning>();
        //discomfortWarning = null;

        // Setup mouse pointer
        pointer = canvas.GetComponent<OVRMousePointer>();
        currentPointerPanel = leftPanel;
        LockCursor();


        // Pre-level stuff
        OnAwakeOrLevelLoad();

        // Add UI panels that are part of the prefab
        LoadPanels(centerPanel.panel.transform, false);

        // Search for UI panels and add them to the context list
        SetupAttachedContexts();


        CentreMouse();
        Hide();

    }

    // This is called when Unity loads a level but not when loading the first level
    void OnLevelWasLoaded(int level)
    {
        if (instance == this)
        {
            OnAwakeOrLevelLoad();
            CleanUpTopMenu();
        }
    }

    void OnAwakeOrLevelLoad()
    {
        if (instance != this)
            return;

        OVRManager.display.RecenterPose();

        AssignCameraRig();
        AssignFader();
    }

    public void AssignFader()
    {
        // make sure we have a new fader object
        fader = cameraRig.GetComponentInChildren<OVRScreenFade2>();

        if (fader == null)
        {
            GameObject fadeObj = Instantiate(Resources.Load("Prefabs/Fader", typeof(GameObject))) as GameObject;
            fadeObj.transform.SetParent(cameraRig.centerEyeAnchor, false);
            fader = fadeObj.GetComponent<OVRScreenFade2>();
        }
        fader.PositionForCamera(cameraRig);

        // Make sure legacy fader objects are not present
        if (cameraRig.leftEyeAnchor.GetComponent<OVRScreenFade>() != null ||
            cameraRig.rightEyeAnchor.GetComponent<OVRScreenFade>() != null)
        {
            Debug.LogError("Camera rig has ScreenFade objects");
        }
    }
    
    public void AssignCameraRig()
    {
        FindPlayerAndCamera();
        // There has to be an event system for the GUI to work
        EventSystem eventSystem = GameObject.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.Log("Creating EventSystem");
            eventSystem = (EventSystem)GameObject.Instantiate(eventSystemPrefab);

        }
        else
        {
            //and an OVRInputModule
            if (eventSystem.GetComponent<OVRInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<OVRInputModule>();
            }
        }
        inputModule = eventSystem.GetComponent<OVRInputModule>();

        playerController = FindObjectOfType<OVRPlayerController>();
        if (playerController)
        {
            CachePlayerControlDefaults();
        }
        cameraRig.EnsureGameObjectIntegrity();
        canvas.GetComponent<Canvas>().worldCamera = cameraRig.leftEyeCamera;
    }

    void FindPlayerAndCamera()
    {
        playerController = FindObjectOfType<OVRPlayerController>();
        if (playerController && playerController.gameObject.layer != playerLayer)
        {
            Debug.LogError("PlayerController should be layer \"Player\"");
        }


        if (cameraRig)
        {
            Transform t = cameraRig.transform.Find("TrackingSpace");
            centerEyeTransform = t.Find("CenterEyeAnchor");
        }

        manager = FindObjectOfType<OVRManager>();
    }

    void LockCursor()
    {
#if !UNITY_ANDROID || UNITY_EDITOR
        // Lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("Lock cursor");
#endif
    }
#endregion

    #region GUIPositioning
    /// <summary>
    /// Add layers which the Inspector auto scaling code will avoid intersecting
    /// </summary>
    public void AddSummoningIgnoreLayers(int layers)
    {
        summoningIgnoreLayers |= layers;
    }
    /// <summary>
    /// Summon the Inspector GUI and position appropriately
    /// </summary>
    /// <param name="rotationAroundPlayer">Instead of having the UI appear directly infront of the player, it will appear rotated left or right around a vertical axis through the player</param>
    public void Show(float rotationAroundPlayer = 0)
    {
        menuActive = true;
        GetComponent<OVRPlatformMenu2>().showMenuEnabled = true;
        leftPanel.SetActive(true);
        rightPanel.SetActive(true);
        centerPanel.SetActive(true);

        // We don't want the mouse to control yaw while the mouse navigates the menu
        if (playerController)
        {
            CachePlayerControlDefaults();
            playerController.SetSkipMouseRotation(true);
            playerController.SetHaltUpdateMovement(true);
        }
        //Calculate maximum scale - we may need to make scale smaller if there are objects in the way
        maxInspectorScale = 1;
        
        // Position UI, after this it may be intersecting world geometry
        Reposition(false, rotationAroundPlayer);
        bool successfulShow = true;

        // Calculate based on UI position how we would need to scale it so that it doesn't intersect geometry
        // this new scale is stored in maxInspectorScale
        SetScaleAllowingForGeometry();

        // Using scale information we just calculated rescale the canvas to fit, or if it would be too small don't
        // display it
        successfulShow = PrescaleInspectorUI(rotationAroundPlayer);

        if (successfulShow)
        {
            // Let listeners know we have shown the inspector
            onInspectorShow.Invoke();
        }
    }

    /// <summary>
    /// Reposition the inspector GUI relative to the player
    /// </summary>
    /// <param name="toOrigin">if true then position will be set relative to the cameraRig origin (i.e. ignoring tracked position)</param>
    /// <param name="rotationAroundPlayer">Makes the UI appear at a position rotated left or right around the player's vertical axis</param>
    public void Reposition(bool toOrigin = false, float rotationAroundPlayer = 0)
    {
        if (centerEyeTransform)
        {
            if (toOrigin)
            {
                transform.position = cameraRig.transform.position + cameraRig.transform.forward * eyeToGUIDistance * maxInspectorScale * centerEyeTransform.transform.lossyScale.x;
                transform.localScale = new Vector3(maxInspectorScale, maxInspectorScale, maxInspectorScale) * centerEyeTransform.transform.lossyScale.x;
                transform.rotation = Quaternion.LookRotation(cameraRig.transform.TransformVector(Vector3.forward), cameraRig.transform.TransformVector(Vector3.up));
            }
            else
            {
                //Orientate facing the player but keep upright
                Vector3 forward = centerEyeTransform.forward;
                forward = Quaternion.Euler(new Vector3(0, rotationAroundPlayer, 0)) * forward;
                //remove any up/down component relative to cameraRig
                forward -= (Vector3.Dot(forward, cameraRig.transform.up) * cameraRig.transform.up);

                if (forward.sqrMagnitude == 0)
                    forward = Vector3.forward;
                forward.Normalize();
                transform.position = centerEyeTransform.position + forward * eyeToGUIDistance * maxInspectorScale * centerEyeTransform.transform.lossyScale.x;
                transform.localScale = new Vector3(maxInspectorScale, maxInspectorScale, maxInspectorScale) * centerEyeTransform.transform.lossyScale.x;
                transform.rotation = Quaternion.LookRotation(forward, cameraRig.transform.TransformVector(Vector3.up));
            }
        }
    }

    /// <summary>
    /// Calculates max scale that can be applied to the inspector and have it fit in the available space.
    /// It does this by casting rays from the centre eye and comparing how far it is to the panel versus
    /// how far it is to any intersecting geometry. The result is the scale that can be applied to the 
    /// inspector *when eye scale is also applied*. 
    /// </summary>
    void SetScaleAllowingForGeometry()
    {
        List<Vector3> testPoints = new List<Vector3>();

        leftPanel.AddPanelPointsToList(5, testPoints);
        rightPanel.AddPanelPointsToList(5, testPoints);
        centerPanel.AddPanelPointsToList(5, testPoints);

        float currentInspectorScale = transform.lossyScale.x;

        float maxScale = 100;
        Vector3 o = cameraRig.centerEyeAnchor.position;
        foreach (Vector3 p in testPoints)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(o, (p - o)), out hit, Mathf.Infinity, ~((1 << playerLayer) | summoningIgnoreLayers)))
            {
                float scale = (hit.distance / ((p - o).magnitude / currentInspectorScale)) * guiGeometryClearanceScale;

                maxScale = Mathf.Min(scale, maxScale);
            }
        }
        //maxScale is the max overall scale that can be applied. We need to divide out current eye scale since this
        //will also always be applied
        maxScale /= centerEyeTransform.transform.lossyScale.x;
        maxInspectorScale = Mathf.Min(maxScale, 1);
    }

    /// <summary>
    /// Use the current maxInspectorScale value to reposition or not show the (not yet shown) Inspector UI
    /// </summary>
    /// <param name="rotationAroundPlayer"></param>
    bool PrescaleInspectorUI(float rotationAroundPlayer = 0)
    {
        bool successfulShow = false;
        // Is there room for the inspector at full scale?
        roomForInspector = (maxInspectorScale > 0.999); // Really we just need it to be 1. But do this to avoid float errors

        if (autoDistance)
        {
            if (maxInspectorScale > minimumScale)
            {
                //Automatically reposition at correct scale
                Reposition(false, rotationAroundPlayer);
                successfulShow = true;
            }
            else
            {
                //It would be too small.  (the reason we have this is to stop the UI being too close to your face)
                Debug.Log("Not showing inspector panel - not enough room");
                Hide();
                successfulShow = false;
            }
        }
        else
        {
            // We don't resize just because there's stuff in the way, set scale back to fullsize
            // note that in this case roomForInspector will still be false so UI fade will behave correctly
            maxInspectorScale = 1;
            successfulShow = true;
        }
        UpdateUIFade();
        return successfulShow;
    }

    /// <summary>
    /// Use the current maxInspectorScale value to reposition or not show the (already visible) Inspector UI
    /// </summary>
    public void RescaleInspectorUI()
    {
        //Get the current rotation relative to the player so after rescaling it has the same rotation
        float currentRotationAroundPlayer = GetCurrentRotationAroundPlayer();
        // Calculated scale geometry permits
        SetScaleAllowingForGeometry();
        if (!autoDistance) 
        {
            // We don't need to rescale since we're not auto scaling, but note whether there's room
            // for the purposes of UIFade
            roomForInspector = (maxInspectorScale > 0.999); // Really we just need it to be 1. But do this to avoid float errors
            maxInspectorScale = 1;
            UpdateUIFade();
        }
        Reposition(false, currentRotationAroundPlayer);
    }

    /// <summary>
    /// Returns rotation of UI relative to player. This depends on how much the HMD has turned since the UI was summoned
    /// </summary>
    float GetCurrentRotationAroundPlayer()
    {
        Vector3 from = centerEyeTransform.transform.forward;
        Vector3 to = transform.position - centerEyeTransform.position;
        from.y = 0;
        to.y = 0;
        return Mathf.Asin(Vector3.Cross(from, to).y / (from.magnitude * to.magnitude)) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Dismiss the inspector UI. Will summon a warning if applicable.
    /// </summary>
    public void Hide(bool allowWarning = false)
    {
        GetComponent<OVRPlatformMenu2>().showMenuEnabled = false;
        menuActive = false;
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);
        centerPanel.SetActive(false);

        if (allowWarning && discomfortWarning)
        {
            discomfortWarning.ShowAndConfigureWarningPanel();
        }
        else
        {
            if (discomfortWarning)
            {
                discomfortWarning.HideWarningPanel();
            }
        }

        if (!discomfortWarning || !discomfortWarning.panelActive)
        {
            // Allow the mouse to control yaw
            if (playerController)
            {
                SetPlayerControlDefaults();
            }
            onInspectorHide.Invoke();
            UpdateUIFade();
        }
    }
    #endregion //GUIPositioning


    #region PanelsAndContexts
    /// <summary>
    /// Make the current context update its UI
    /// </summary>
    public void UpdateContextMenu()
    {
        controlsPanel.EraseButtons();
        currentContext.BuildUI(this);
    }

    /// <summary>
    /// Temporarily add these contexts to our UI. Returns true if any were added
    /// </summary>
    public bool LoadSceneSpecificContextsFromPanel(Transform panel)
    {
        SetUIMaterials(panel);
        LoadPanels(panel, true);
        return sceneSpecificContexts.Count > 0;
    }

    /// <summary>
    /// Remove all contexts previously added as scene-specific contexts
    /// </summary>
    public void ClearSceneSpecificContexts()
    {
        if (contextList != null)
        {
            foreach (OVRInspectorContextDetails details in sceneSpecificContexts)
            {
                GameObject.Destroy(details.panel.gameObject);
                contextList.Remove(details);
            }
            sceneSpecificContexts.Clear();
        }

    }
    /// <summary>
    /// Are any scene specific contexts present
    /// </summary>
    public bool ScenePanelPresent()
    {
        return (sceneSpecificContexts.Count > 0);
    }

    /// <summary>
    /// Activate first context specific to this scene
    /// </summary>
    public void ActivateSceneContext()
    {
        if (sceneSpecificContexts.Count > 0)
        {
            ContextActivated(sceneSpecificContexts[0]);
            UpdateTopMenu();
        }
    }

    /// <summary>
    /// Load the panels under this transform as contexts selectable in the top level menu
    /// </summary>
    /// <param name="panel">The parent panel</param>
    /// <param name="markAsSceneSpecific">Should these be marked as scene-specific so we know to remove them later</param>
    void LoadPanels(Transform panel, bool markAsSceneSpecific)
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in panel)
        {
            children.Add(child);
        }

        int prevNumContexts = contextList.Count;
        foreach (Transform child in children)
        {
            if (IsLabeledDontMove(child.gameObject))
            {
                continue;
            }
            CullPlatformSpecificObjects(child.gameObject);
            // Move to our main UI panel
            child.SetParent(controlsPanel.scrollingContent.transform, false);
            OVRInspectorContextDetails contextDetails = new OVRInspectorContextDetails(child.gameObject);
            contextList.Add(contextDetails);
            child.gameObject.SetActive(false);
            if (markAsSceneSpecific)
            {
                sceneSpecificContexts.Add(contextDetails);
            }
        }


        if (contextList.Count > prevNumContexts)
        {
            // Activate the first new context
            ContextActivated(contextList[prevNumContexts]);
        }

        // Make sure menu reflects items
        UpdateTopMenu();
    }
    

    /// <summary>
    /// Add already attached contexts to the meny system
    /// </summary>
    void SetupAttachedContexts()
    {
        foreach (Transform child in controlsPanel.panel.transform)
        {
            if (!IsLabeledDontMove(child.gameObject))
            {
                OVRInspectorContextDetails contextDetails = new OVRInspectorContextDetails(child.gameObject);
                SetUIMaterials(child);
                contextList.Add(contextDetails);
                child.gameObject.SetActive(false);
            }
        }
        if (currentContext == null && contextList.Count > 0)
        {
            ContextActivated(contextList[0]);
        }
    }

    /// <summary>
    /// Rebuild top menu to allow all current contexts to be selected
    /// </summary>
    void UpdateTopMenu()
    {
        leftPanel.EraseButtons();
        lastSelectedContextButton = null;
        foreach (OVRInspectorContextDetails details in contextList)
        {
            if (!details.GoesLastOnMenu())
                AddContextButton(details);
        }
        //Add a button to close the menu
        recenterButton = leftPanel.AddButton("Recenter", delegate { Recenter(); }, buttonPrefab);
        if (allowClose)
        {
            leftPanel.AddButton("Close", delegate { Hide(); }, buttonPrefab);
        }
        foreach (OVRInspectorContextDetails details in contextList)
        {
            if (details.GoesLastOnMenu())
                AddContextButton(details);
        }
    }

    /// <summary>
    /// Remove any menu contexts which belong to scene-specific contexts from a previous scene
    /// </summary>
    void CleanUpTopMenu()
    {
        // remove buttons with no context object
        if (contextList != null)
        {
            List<OVRInspectorContextDetails> toRemove = new List<OVRInspectorContextDetails>();
            foreach (OVRInspectorContextDetails details in contextList)
            {
                if ((object)(details.context) == null && details.panel == null)
                {
                    toRemove.Add(details);
                }
            }
            foreach (OVRInspectorContextDetails details in toRemove)
            {
                contextList.Remove(details);
            }

            // Reposition buttons
            UpdateTopMenu();
        }

    }

    /// <summary>
    /// Add a context button to top level menu
    /// </summary>
    void AddContextButton(OVRInspectorContextDetails details)
    {
        OVRInspectorContextDetails detailsCopy = details;
        Button button = leftPanel.AddButton(details.GetName(), delegate(Button b) { SelectContextButton(detailsCopy, b); }, buttonPrefab);
        if (details == currentContext)
        {
            SelectContextButton(details, button);
        }
    }

    /// <summary>
    /// Handler for when a button is pressed
    /// </summary>
    void SelectContextButton(OVRInspectorContextDetails contextDetails, Button button = null)
    {
        if (lastSelectedContextButton)
        {
            lastSelectedContextButton.interactable = true;
        }
        lastSelectedContextButton = button;
        if (lastSelectedContextButton)
        {
            lastSelectedContextButton.interactable = false;
        }

        ContextActivated(contextDetails);
    }

    /// <summary>
    /// A context was activated
    /// </summary>
    void ContextActivated(OVRInspectorContextDetails contextDetails)
    {
        if (contextDetails != currentContext)
        {
            if (currentContext != null)
            {
                currentContext.SetContextActive(this, false);
            }
            currentContext = contextDetails;
            currentContext.SetContextActive(this, true);
            UpdateContextMenu();
        }
    }


    void SetButtonText(Button button, string newText)
    {
        foreach (Transform t in button.transform)
        {
            var text = t.GetComponent<Text>();
            if (text != null)
            {
                text.text = newText;
                break;
            }
        }
    }


    /// <summary>
    /// Register a new context for that will appear on the top level menu.
    /// </summary>
    /// <param name="context">Interface that will be used to build UI when this context is activated</param>
    /// <param name="subContextID">An ID that is passed back to the context. Useful if this interface supports more than one context</param>
    /// <param name="setCurrent">If true this context will be immediately activiated</param>
    public void RegisterContext(IOVRInspectorContext context, int subContextID, bool setCurrent = false)
    {
        OVRInspectorContextDetails contextDetails = new OVRInspectorContextDetails(context, subContextID);

        contextList.Add(contextDetails);
        if (currentContext == null || setCurrent)
        {
            ContextActivated(contextDetails);
        }
        UpdateTopMenu();
    }

    // Interface for IOVRInspectorContext to use to build its custom UI
    public Button AddButton(string name, OVRInspectorPanelBuilder.ButtonPress callback)
    {
        return controlsPanel.AddButton(name, callback, buttonPrefab);
    }
    public Button AddFolder(string name, OVRInspectorPanelBuilder.ButtonPress callback)
    {
        return controlsPanel.AddButton(name, callback, folderPrefab);
    }
    #endregion PanelsAndContexts

    #region Updates
    // Update is called once per frame
    void Update()
    {
        inputModule.rayTransform = OVRGazePointer.instance.rayTransform =
            (OVRInput.GetActiveController() == OVRInput.Controller.Touch) ? cameraRig.rightHandAnchor :
            (OVRInput.GetActiveController() == OVRInput.Controller.RTouch) ? cameraRig.rightHandAnchor :
            (OVRInput.GetActiveController() == OVRInput.Controller.LTouch) ? cameraRig.leftHandAnchor :
            cameraRig.centerEyeAnchor;
        
        UpdateFramerate();
        if (menuActive)
        {
#if UNITY_ANDROID // don't use back button (comes through as escape key) to clear menu since this is Oculus back button
            if (OVRInput.GetDown(showButton))
#else
            if (Input.GetKeyDown(hideKey) || OVRInput.GetDown(showButton))
#endif
            {
                if (discomfortWarning && !discomfortWarning.panelActive)
                {
                    //Hide but show warning as appropriate
                    Hide(true);
                }
                else
                {
                    //Hide fully, including getting rid of the warning
                    Hide();
                }

            }

            //Lock cursor to window on mouse press
            if (Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }
#if !UNITY_ANDROID || UNITY_EDITOR
            UpdateMousePointer();
#endif

        }
        else
        {
            if (Input.GetKeyDown(showKey) || OVRInput.GetDown(showButton))
            {
                if (discomfortWarning && discomfortWarning.panelActive)
                {
                    //Hide warning
                    Hide();
                }
                else
                {
                    // Show inspector
                    Show();
                }
            }
        }

    }
    void UpdateFramerate()
    {
        if (Time.unscaledTime > lastFrameRateUpdateTime + frameRateUpdateInterval)
        {
            lastFPSValue = (Time.frameCount - lastFrameIndex) / frameRateUpdateInterval;
            lastFrameIndex = Time.frameCount;
            lastFrameRateUpdateTime = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Update mouse pointer based on mouse movement, moving between panels as necessary
    /// </summary>
    void UpdateMousePointer()
    {
        // Only allow mouse movement if gaze is focussed on this canvas
        if (inputModule.activeGraphicRaycaster != canvas.GetComponent<OVRRaycaster>())
            return;
        // Move the mouse according to move speed, and move to next panel if necessary
        mousePos += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * pointer.mouseMoveSpeed;
        float currentPanelWidth = currentPointerPanel.panel.GetComponent<RectTransform>().rect.width;
        float currentPanelHeight = currentPointerPanel.panel.GetComponent<RectTransform>().rect.height;
        if (mousePos.x < -currentPanelWidth / 2)
        {
            if (currentPointerPanel.leftPanel != null)
            {
                mousePos.x += currentPanelWidth / 2 + currentPointerPanel.leftPanel.panel.GetComponent<RectTransform>().rect.width / 2;
                SetPointerPanel(currentPointerPanel.leftPanel);
            }
            else
            {
                mousePos.x = -currentPanelWidth / 2;
            }
        }
        else if (mousePos.x > currentPanelWidth / 2)
        {
            if (currentPointerPanel.rightPanel != null)
            {
                mousePos.x -= currentPanelWidth / 2 + currentPointerPanel.rightPanel.panel.GetComponent<RectTransform>().rect.width / 2;
                SetPointerPanel(currentPointerPanel.rightPanel);
            }
            else
            {
                mousePos.x = currentPanelWidth / 2;
            }
        }
        mousePos.y = Mathf.Clamp(mousePos.y, -currentPanelHeight / 2, currentPanelHeight / 2);

        // Position mouse pointer
        pointer.SetLocalPosition(mousePos);
    }
    #endregion

    #region Mouse Control
    /// <summary>
    /// Position mouse pointer in centre of centre panel
    /// </summary>
    public void CentreMouse()
    {
        mousePos = new Vector2(0, 0);
        SetPointerPanel(centerPanel);
    }

    /// <summary>
    /// Set the panel the mouse pointer is currently in
    /// </summary>
    void SetPointerPanel(OVRInspectorPanelBuilder panel)
    {
        currentPointerPanel = panel;
        pointer.pointerObject.transform.SetParent(panel.panel.transform);
        pointer.pointerObject.transform.localRotation = Quaternion.identity;
    }
    #endregion Mouse Control

    #region Documentation Panel
    /// <summary>
    /// Set the current doc string
    /// </summary>
    public void SetDocText(string text)
    {
        if (text == null || text == "")
            return;

        FilterDocs(ref text);

        // Use Unity's text color system to highlight titles
        text = Regex.Replace(text, "<title>", "<color=#fffcafff>");
        text = Regex.Replace(text, "</title>", "</color>");

        docText = text;

        UpdateDocsVisibility();
        docsScroller.GotoTop();

    }
    
    /// <summary>
    /// Set current doc text from file
    /// </summary>
    public void SetDocTextFromFile(string filename)
    {
        TextAsset text = Resources.Load(filename) as TextAsset;
        if (text)
        {
            OVRInspector.instance.SetDocText(text.ToString());
        }
    }

    public void ResetDocText()
    {
        SetDocText("");
    }

    /// <summary>
    /// Set whether doc panel can be scrolled by user
    /// </summary>
    public void SetDocsScrollEnabled(bool enabled)
    {
        docsScroller.SetEnabled(enabled);
    }
    
    /// <summary>
    /// Remove text from docs which are Rift or Gear VR specific
    /// </summary>
    /// <param name="docs"></param>
    void FilterDocs(ref string docs)
    {
#if UNITY_ANDROID
        string[] patterns = { "<ONLY_RIFT>.*?</ONLY_RIFT>", "<ONLY_GEARVR>", "</ONLY_GEARVR>" };
#else
        string[] patterns = {"<ONLY_GEARVR>.*?</ONLY_GEARVR>","<ONLY_RIFT>","</ONLY_RIFT>"};
#endif

        foreach (string pattern in patterns)
        {
            string replacement = "";
            Regex rgx = new Regex(pattern, RegexOptions.Singleline);
            docs = rgx.Replace(docs, replacement);
        }
    }

    /// <summary>
    /// Refresh whether docs are currently visible
    /// </summary>
    void UpdateDocsVisibility()
    {
        if (docsPanel != null)
        {
            bool active = docText.Length > 0;
            docsPanel.SetActive(active);
            docsScroller.MarkContentChanged();
            if (active)
            {
                docsPanelText.text = docText;
            }

        }
    }
    #endregion Documentation Panel


    #region Top Level Menu Features
    /// <summary>
    /// Fade out and quit. Unless in the editor in which case just stop playing
    /// </summary>
    public void DoQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        StartCoroutine(QuitApp());
#endif
    }

    /// <summary>
    /// Fade out and quit
    /// </summary>
    /// <returns></returns>
    IEnumerator QuitApp()
    {
        Hide();
        fader.SetUIFade(1);
        yield return null;
        Application.Quit();

    }
    
    void Recenter()
    {
        StartCoroutine(RecenterCountdown());
    }
    /// <summary>
    /// Countdown on recenter button to zero then actually recenter tracking
    /// </summary>
    IEnumerator RecenterCountdown()
    {
        recenterButton.interactable = false;
        int seconds = 3;
        while (seconds > 0)
        {
            SetButtonText(recenterButton, string.Format("{0}", seconds));
            yield return new WaitForSeconds(1);
            seconds--;
        }
        OVRManager.display.RecenterPose();
        Reposition(true); // Keep the GUI in front of the camera
        SetButtonText(recenterButton, "Recenter");
        recenterButton.interactable = true;
    }
    #endregion Top Level Menu Features 


    #region Player Controller Caching
    void CachePlayerControlDefaults()
    {
        playerController.GetSkipMouseRotation(ref previouslySkippingMouseRotation);
        playerController.GetHaltUpdateMovement(ref previouslyHaltedMovementUpdate);
    }

    void SetPlayerControlDefaults()
    {
        playerController.SetSkipMouseRotation(previouslySkippingMouseRotation);
        playerController.SetHaltUpdateMovement(previouslyHaltedMovementUpdate);
    }
    #endregion Interaction With Player Controller

    #region UI Graphical Effects
    /// <summary>
    /// Set UI material properties
    /// </summary>
    void UpdateUIMaterials()
    {
        const int shaderLabZTest_Always = 0;
        const int shaderLabZTest_LEqual = 2;

        if (drawOverEverything)
        {
            uiMaterial.SetInt("_ZTest", shaderLabZTest_Always);
            uiFontMaterial.SetInt("_ZTest", shaderLabZTest_Always);
        }
        else
        {
            uiMaterial.SetInt("_ZTest", shaderLabZTest_LEqual);
            uiFontMaterial.SetInt("_ZTest", shaderLabZTest_LEqual);
        }
    }

    /// <summary>
    /// Update if and how much UI should be fading
    /// </summary>
    void UpdateUIFade()
    {
        bool doFade = false;
        if (!menuActive || uiFadeMode == UIFadeMode.never)
        {
            doFade = false;
        }
        else if (uiFadeMode == UIFadeMode.whenNoRoom)
        {
            doFade = !roomForInspector;
        }
        else if (uiFadeMode == UIFadeMode.always)
        {
            doFade = true;
        }


        if (doFade)
        {
            fader.SetUIFade(uiFadeLevel);
        }
        else
        {
            fader.SetUIFade(0);
        }
    }

    /// <summary>
    /// Ensure all UI elements in heirarchy are using our UI materials
    /// </summary>
    void SetUIMaterials(Transform canvasTransform)
    {
        var sprites = canvasTransform.GetComponentsInChildren<Image>();
        foreach (var sprite in sprites)
        {
            sprite.material = uiMaterial;
        }

        var labels = canvasTransform.GetComponentsInChildren<Text>();
        foreach (var label in labels)
        {
            label.material = uiFontMaterial;
        }
    }

    public IEnumerator FadeOutCameras()
    {
        fader.FadeOut();
        while (fader.currentAlpha < 1)
            yield return null;
    }

    #endregion UI Graphical Effects


    int GetLayerOrReportError(string layer)
    {
        int layerIndex = LayerMask.NameToLayer(layer);
        if (layerIndex == -1)
        {
            Debug.LogError(string.Format("No \"{0}\" layer exists",layer));
        }
        return layerIndex;
    }

    /// <summary>
    /// Move all child nodes from src to dst.
    /// </summary>
    /// <param name="startIndex">The sibling index on dst that reparented objects should start at</param>
    void ReparentChildren(Transform src, Transform dst, int startIndex)
    {
        int index = startIndex;
        foreach (Transform child in src)
        {
            child.SetParent(dst);
            child.SetSiblingIndex(index);
            index++;
        }
    }

    void DestroyAllChildren(Transform t)
    {
        foreach (Transform child in t)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Remove nodes in heirarchy beneath obj that are marked as being specific to either
    /// Rift or Gear VR
    /// </summary>
    void CullPlatformSpecificObjects(GameObject obj)
    {
        bool somethingChanged = false;
        do
        {
            int childCount = obj.transform.childCount;
            somethingChanged = false;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);

                if (child.childCount > 0)
                {
                    if (child.name.Equals(nodeToCull) && child.childCount > 0)
                    {
                        child.name = "REMOVED"; // Need to change name so it's not found on next iteration (child count stays even when children are destroyed)
                        DestroyAllChildren(child);
                        somethingChanged = true;
                        break;
                    }
                    else if (child.name.IndexOf(filterPrefix) == 0 && child.childCount > 0)
                    {
                        ReparentChildren(child, obj.transform, i + 1);
                        somethingChanged = true;
                        break;
                    }
                    CullPlatformSpecificObjects(child.gameObject);
                }
            }
        } while (somethingChanged);
    }

    bool IsLabeledDontMove(GameObject panel)
    {
        return panel.tag.CompareTo("dontmove") == 0;
    }
}
