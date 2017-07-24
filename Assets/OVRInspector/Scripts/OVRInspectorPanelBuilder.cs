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
/// Class which handles building a Button/Folder panel view in the inspector.
/// </summary>
public class OVRInspectorPanelBuilder
{
    public GameObject panel{get;private set;}                // The panel UI element
    public GameObject scrollingContent { get; private set; }     // Content which scrolls within higher layer panel
    
    // Links to adjacent panels for mouse movement
    public OVRInspectorPanelBuilder leftPanel;
    public OVRInspectorPanelBuilder rightPanel;
    
    
    private GameObject buttonPanel;            // Panel containing buttons (may be the same as scrollingContent) 
    private List<Button> buttonList;
    private Vector2 insertPosition;            // Where to insert the next button, increments as list is made
    private float initialScrollingContentSize; // Scroll region size to keep scroll behaviour correct as panel fills/empties

    public delegate void ButtonPress(Button button);

    public OVRInspectorPanelBuilder(GameObject panel)
    {
        leftPanel = rightPanel = null;
        buttonList = new List<Button>();
        this.panel = panel;

        Transform scrollingContentTransform = panel.transform.Find("PanelContent");
        if (scrollingContentTransform != null)
        {
            buttonPanel = scrollingContent = scrollingContentTransform.gameObject;
            initialScrollingContentSize = scrollingContent.GetComponent<RectTransform>().rect.height;
        }
        else
        {
            scrollingContent = null;
            buttonPanel = panel;
        }
    }

    /// <summary>
    /// Add new button to panel
    /// </summary>
    /// <param name="name">Text to display on button</param>
    /// <param name="callback">Function to call when pressed</param>
    /// <param name="buttonPrefab">Prefab to use for button graphic</param>
    /// <returns></returns>
    public Button AddButton(string name, ButtonPress callback, Button buttonPrefab)
    {
        Button button = (Button)GameObject.Instantiate(buttonPrefab);

        // Parent to panel and make it first item so that it appears behind the mouse
        RectTransform rectTransform = button.gameObject.GetComponent<RectTransform>();
        button.transform.SetParent(buttonPanel.transform, false);
        button.transform.SetAsFirstSibling();

        // Place at bottom of GUI and move insertPosition down for next time
        rectTransform.anchoredPosition = insertPosition - Vector2.up * rectTransform.rect.height / 2;
        insertPosition.y -= rectTransform.rect.height + OVRInspector.instance.buttonSpacing;
        if (scrollingContent)
        {
            // Keep scrolling panel correct size for new amount of content
            scrollingContent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -insertPosition.y);
        }

        button.onClick.AddListener(delegate { callback(button); });
        // Set label
        ((Text)(button.GetComponentsInChildren(typeof(Text), true)[0])).text = name;
        buttonList.Add(button);
        return button;
    }

    /// <summary>
    /// Remove all buttons and reset scrolling window size
    /// </summary>
    public void EraseButtons()
    {
        insertPosition = new Vector2();
        foreach (Button button in buttonList)
        {
            GameObject.Destroy(button.gameObject);
        }
        buttonList.Clear();
        if (scrollingContent)
        {
            scrollingContent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialScrollingContentSize);
        }
    }

    public void SetActive(bool active)
    {
        panel.SetActive(active);
    }

    /// <summary>
    /// Adds points that form a grid across the surface of the panel. 
    /// </summary>
    /// <param name="pointsPerEdge">Number of points per edge. Total number of points will be this squared</param>
    /// <param name="points">List for points to be returned in</param>
    public void AddPanelPointsToList(int pointsPerEdge, List<Vector3> points)
    {
        pointsPerEdge = Mathf.Max(pointsPerEdge, 2); // Always do at least 2 points per edge
        Vector3[] corners = new Vector3[4];
        panel.GetComponent<RectTransform>().GetWorldCorners(corners);

        var x = (corners[1] - corners[0]) / (pointsPerEdge - 1);
        var y = (corners[3] - corners[0]) / (pointsPerEdge - 1);

        // Build a grid of points across the panel
        for (int i = 0; i < pointsPerEdge; i++)
        {
            for (int j = 0; j < pointsPerEdge; j++)
            {
                points.Add(corners[0] + x * i + y * j);
            }
        }
    }
}
