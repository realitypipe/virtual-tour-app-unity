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
using System.Collections;

/// <summary>
/// The details of a single context in the inspector. Corresponds to a single centre panel and the button
/// on the left panel which activates this panel.
/// </summary>
class OVRInspectorContextDetails
{
    // A context is either represented by the context member, which is an interface which can build a
    // dynamic UI; or by the panel member, which is UI panel where the UI is already built.
    public IOVRInspectorContext context; // if present panel should be null
    public int subContext; // used by the context interface in the case that this interface supports more than one UI

    public GameObject panel; // If present context should be null

    /// <summary>
    /// Construct from IOVRInspectorContext which can build the UI
    /// </summary>
    public OVRInspectorContextDetails(IOVRInspectorContext context, int subContext)
    {
        this.context = context;
        this.subContext = subContext;
        panel = null;
    }
    /// <summary>
    /// Construct from an existing UI panel
    /// </summary>
    public OVRInspectorContextDetails(GameObject panel)
    {
        context = null;
        this.panel = panel;
    }

    public string GetName()
    {
        if (context != null)
            return context.GetName(subContext);
        else
            return panel.gameObject.name;
    }
    /// <summary>
    /// Set this as the active context
    /// </summary>
    public void SetContextActive(OVRInspector inspector, bool active)
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(active);
        }
        if (context != null)
        {
            context.SetContextActive(inspector, subContext, active);
        }
    }
    /// <summary>
    /// Build UI for this context
    /// </summary>
    /// <param name="inspector">Reference to inspector which can be used to add buttons/folders for this context</param>
    public void BuildUI(OVRInspector inspector)
    {
        if (context != null)
        {
            context.BuildUI(inspector, subContext);
        }
    }

    /// <summary>
    /// Is this context flags as having to appear at the end of the menu
    /// </summary>
    /// <returns></returns>
    public bool GoesLastOnMenu()
    {
        return panel && panel.CompareTag("listlast");
    }
};