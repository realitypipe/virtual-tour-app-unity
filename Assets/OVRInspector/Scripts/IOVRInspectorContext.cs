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
/// Interface that a class should implement to provide top level UI panel in the
/// inspector
/// </summary>

public interface IOVRInspectorContext 
{
    /// <summary>
    /// This will be called with active=true when the user chooses to make this context active. When 
    /// another context is chosen this will be called again with active=false.
    /// </summary>
    /// <param name="inspector">
    /// The main inspector object
    /// </param>
    /// <param name="subContextID">
    /// Corresponds to the subContextID passed in when the original call to 
    /// OVRInspector.RegisterContext was made.
    /// </param>
    void SetContextActive(OVRInspector inspector, int subContextID, bool active);

    /// <summary>
    /// Called when the inspector needs this panel to be rebuilt
    /// </summary>
    /// <param name="inspector"></param>
    /// <param name="subContextID"></param>
    void BuildUI(OVRInspector inspector, int subContextID);

    /// <summary>
    /// Return the name that should be displayed in the top level inspectory UI
    /// </summary>
    string GetName(int subContextID);
}
