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
using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class OVRGazeEventTrigger : Graphic, IPointerEnterHandler, IPointerExitHandler
{
    [Serializable]
    public class PointerEnterEvent : UnityEvent { }

    public PointerEnterEvent onEnter = new PointerEnterEvent();
    public PointerEnterEvent onExit = new PointerEnterEvent();

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        onEnter.Invoke();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        onExit.Invoke();
    }
    /// <summary>
    /// Test if pointer intersects graphic. We always return true so that user can scroll
    /// by looking anywhere in this rect
    /// </summary>
    public override bool Raycast(Vector2 sp, Camera eventCamera)
    {
        return true;
    }
    [Obsolete]
    protected override void OnFillVBO(List<UIVertex> vbo)
    {
    }


}
