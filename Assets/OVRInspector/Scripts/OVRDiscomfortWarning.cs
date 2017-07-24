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
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls a warning panel that is displayed to users when they dismiss the inspector
/// in a scene which make contain uncomfortable elements
/// </summary>
public class OVRDiscomfortWarning : MonoBehaviour {

    GameObject warningPanel;
    Text messageText;

    public enum DiscomfortWarningType
    {
        FirstPersonControls,    // E.g. using WASD or thumbsticks to control camera movement
        Acceleration,           // E.g. a vehicle
        VerticalAcceleration,   // E.g. an elevator or stairs
        RapidRotation,          // Anything that rapidly changes the view direction
        HeadRelativeUI,         // UI elemtents locked to the users view
        Aliasing,               // Rendering aliasing which may be different between eyes

        Other
    };

    public struct DiscomfortWarning
    {
        public DiscomfortWarningType type;
        public string message;

        public DiscomfortWarning(DiscomfortWarningType type, string message = null)
        {
            this.type = type;
            this.message = message;
        }

    };

    public bool panelActive { get { return warningPanel && warningPanel.activeSelf; } }

    List<DiscomfortWarning> warnings = new List<DiscomfortWarning>();


    void Start()
    {
        warningPanel = transform.Find("Canvas/WarningPanel").gameObject;
        if (!warningPanel)
        {
            Debug.LogError("Couldn't find WarningPanel");
        }
        
        Transform messageGameObject = transform.Find("Canvas/WarningPanel/MessageText");
        if (messageGameObject)
        {
            messageText = messageGameObject.GetComponent<Text>();
        }
        if (!messageText)
        {
            Debug.LogError("Couldn't find MessageText");
        }

    }

    public void ResetWarnings()
    {
        warnings.Clear();
    }

    public bool HasWarnings()
    {
        return warnings.Count > 0;
    }

    /// <summary>
    /// Show the warning panel with warnings for this scene.
    /// </summary>
    public void ShowAndConfigureWarningPanel()
    {
        ResetWarnings();
        OVRDiscomfortWarningSource[] warningSources = FindObjectsOfType<OVRDiscomfortWarningSource>();
        PopulateWarningList(warningSources);

        if (HasWarnings())
        {
            warningPanel.SetActive(true);
            messageText.text = BuildWarningMessage();

        }
        
    }

    public void HideWarningPanel()
    {
        if (warningPanel)
            warningPanel.SetActive(false);
    }
    

    public string GetMessage(DiscomfortWarningType warning)
    {
        switch(warning)
        {
            case DiscomfortWarningType.FirstPersonControls:    
                return "First person controls";
            case DiscomfortWarningType.Acceleration:
                return "Acceleration";
            case DiscomfortWarningType.VerticalAcceleration:
                return "Vertical Acceleration";
            case DiscomfortWarningType.RapidRotation:
                return "Rapid rotation";
            case DiscomfortWarningType.HeadRelativeUI:
                return "Head relative UI";
            case DiscomfortWarningType.Aliasing:
                return "Graphical Aliasing";
            default:
                return null;
        }
    }

    void PopulateWarningList(OVRDiscomfortWarningSource[] sources)
    {
        foreach (OVRDiscomfortWarningSource source in sources)
        {
            foreach (DiscomfortWarning warning in source.GetWarnings())
            {
                warnings.Add(warning);
            }
        }
    }

    public string BuildWarningMessage()
    {
        string s = "This scene may be uncomfortable for the following reasons:\n";

        foreach (DiscomfortWarning warning in warnings)
        {
            string message = (warning.message != null) ? warning.message : GetMessage(warning.type);
            
            s += "* ";
            s += message;
            s += "\n";
        }
        return s;
    }

}
