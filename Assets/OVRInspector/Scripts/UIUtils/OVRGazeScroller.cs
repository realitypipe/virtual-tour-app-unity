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
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class OVRGazeScroller : MonoBehaviour 
{
    public OVRGazeEventTrigger upTrigger;
    public OVRGazeEventTrigger downTrigger;
    ScrollRect scrollRect;
    float scrollDirection;
    float scrollRange;
    float scrollPower = 60;
    private bool scrollEnabled = true;

	// Use this for initialization
    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();

        upTrigger.onEnter.AddListener(StartScrollUp);
        upTrigger.onExit.AddListener(StopScroll);
        downTrigger.onEnter.AddListener(StartScrollDown);
        downTrigger.onExit.AddListener(StopScroll);
        RefreshContentSize();

    }

    public void MarkContentChanged()
    {
    }
    public void StartScrollUp()
    {
        scrollDirection = 1;
    }
    public void StopScroll()
    {
        scrollDirection = 0;
    }
    public void StartScrollDown()
    {
        scrollDirection = -1;
    }
    public void SetEnabled(bool enabled)
    {
        scrollEnabled = enabled;
    }


    void OnEnable()
    {
        scrollDirection = 0;
    }
   

    void RefreshContentSize()
    {
        float scrollRectHeight = GetComponent<RectTransform>().rect.height;
        float contentRectHeight = scrollRect.content.GetComponent<RectTransform>().rect.height;
        if (contentRectHeight != 0)
        {
            scrollRange = contentRectHeight - scrollRectHeight;
        }
    }

    public void GotoTop()
    {
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1;
    }

	
	// Update is called once per frame
	void Update () {
        RefreshContentSize();
        bool canGoUp = scrollRect.verticalNormalizedPosition*scrollRange < (scrollRange -0.01f);
        bool canGoDown = scrollRect.verticalNormalizedPosition*scrollRange > 0.01f; 
        if ((scrollDirection > 0 &&  canGoUp) || (scrollDirection < 0 && canGoDown))
        {
            scrollRect.verticalNormalizedPosition = scrollRect.verticalNormalizedPosition + Time.deltaTime * scrollDirection * scrollPower * scrollRect.scrollSensitivity / scrollRange;
        }
        if (!scrollEnabled)
        {
            canGoDown = canGoUp = false;
        }
        upTrigger.gameObject.SetActive(canGoUp);
        downTrigger.gameObject.SetActive(canGoDown);
        if (scrollDirection > 0 && !canGoUp)
        {
            scrollDirection = 0;
        }
        if (scrollDirection < 0 && !canGoDown)
        {
            scrollDirection = 0;
        }
	}
}
