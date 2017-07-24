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
using System;
using System.Collections;
using System.Runtime.InteropServices; // required for DllImpor


public class OVRSettingsMenu : MonoBehaviour {

    // Slider references to make sure they reflect current settings
    Slider virtualTextureSlider;       
    Slider msaaSlider;
    Slider gpuLevelSlider;
    Slider cpuLevelSlider;
    Text msaaSliderLabel;
    Text fpsLabel;


    // Currently selected settings
    int chosenEyeBufferAA = 0;
    float chosenVirtualTextureSize = 1;
    
    OVRInspector inspector;

	int[] MSAALookupTable = new int[] {0,
        2,
        4,
        8};




	// Use this for initialization
	void Start ()
    {

#if UNITY_ANDROID && !UNITY_EDITOR
        SetCPULevel(3);
        SetGPULevel(2);
        gpuLevelSlider =transform.FindChild("GPULevel/Slider").GetComponent<Slider>();
        cpuLevelSlider =transform.FindChild("CPULevel/Slider").GetComponent<Slider>();
#endif

        fpsLabel = transform.Find("FPS").GetComponent<Text>();
        virtualTextureSlider = transform.Find("VirtualTextureSizeSlider/Slider").GetComponent<Slider>();
        msaaSlider = transform.Find("MSAASlider/Slider").GetComponent<Slider>();
        msaaSliderLabel = transform.Find("MSAASlider/ValueText").GetComponent<Text>();


        var msaaSetting = QualitySettings.antiAliasing;
        for (int i = 0; i < MSAALookupTable.Length; i++)
        {
            if (MSAALookupTable[i] == msaaSetting)
            {
                msaaSlider.value = i;
                msaaSliderLabel.text = string.Format("{0}", (int)msaaSetting);
                break;
            }
        }

		virtualTextureSlider.value = UnityEngine.VR.VRSettings.renderScale;

	}

	// Update is called once per frame
	void Update()
    {
        UpdateFPSSliderText();
    }

    void UpdateFPSSliderText()
    {
        fpsLabel.text = string.Format("Current FPS: {0:0.0}",OVRInspector.instance.frameRate);
    }

    public void SetEyeBufferAA(float float_value)
    {
        chosenEyeBufferAA = (int)float_value;
        msaaSliderLabel.text = string.Format("{0}", (int)MSAALookupTable[chosenEyeBufferAA]);

    }

    public void ApplyEyeBufferAA()
    {
        QualitySettings.antiAliasing = MSAALookupTable[chosenEyeBufferAA];
        //There's a bug in the integration that means the virtual texture size will be reset now.
        //so we reset it in our UI too so the UI is consistent.
        chosenVirtualTextureSize = 1;
        UnityEngine.VR.VRSettings.renderScale = 1;
        virtualTextureSlider.value = 1;

        // This bug also means that tracking is lost so we recenter it now as well
        StartCoroutine(RecenterFix());
    }

    IEnumerator RecenterFix()
    {
        yield return new WaitForSeconds(1f);
        UnityEngine.VR.InputTracking.Recenter();
    }
   
    public void SetVirtualTextureSize(float value)
    {
        chosenVirtualTextureSize = Mathf.Clamp(value, 0.1f, 1);
        
    }

    public void ApplyVirtualTextureSize()
    {
        UnityEngine.VR.VRSettings.renderScale = chosenVirtualTextureSize;
    }


    public void SetCPULevel(float value)
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
            OVRPlugin.cpuLevel = (int)value;
#endif
        Debug.Log(string.Format("SetCPULevel {0}", value));
    }

    public void SetGPULevel(float value)
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
            OVRPlugin.gpuLevel = (int)value;
#endif
        Debug.Log(string.Format("SetGPULevel {0}", value));
    }
}
