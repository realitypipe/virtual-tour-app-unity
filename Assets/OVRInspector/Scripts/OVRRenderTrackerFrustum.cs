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

// Render Oculus position tracking camera frustum
// https://developer.oculus.com/documentation/pcsdk/latest/concepts/dg-sensor/
// - add this to a Unity object to visualize the tracking frustum
// sgreen 12/2014

using UnityEngine;
using System.Collections;
using VR = UnityEngine.VR;

public class OVRRenderTrackerFrustum : MonoBehaviour {

    public bool wireframe = true;
    public Material lineMaterial;
    public Material solidMaterial;

    public Color color = Color.yellow;
    public Color errorColor = Color.red;

    public bool drawCamera = true;
    public float cameraDisplaySize = 0.05f;

    // draw frustum in wireframe
    void DrawFrustumWire(float sx, float sy, float nearZ, float farZ, Material mat, Color color)
    {
        mat.SetColor("_Color", color);
        mat.SetPass(0);

        GL.Begin(GL.LINES);

        //GL.Color(color);

        // 4 corner edges
        GL.Vertex3(nearZ * sx, nearZ * sy, nearZ);
        GL.Vertex3(farZ * sx, farZ * sy, farZ);

        GL.Vertex3(-nearZ * sx, nearZ * sy, nearZ);
        GL.Vertex3(-farZ * sx, farZ * sy, farZ);

        GL.Vertex3(-nearZ * sx, -nearZ * sy, nearZ);
        GL.Vertex3(-farZ * sx, -farZ * sy, farZ);

        GL.Vertex3(nearZ * sx, -nearZ * sy, nearZ);
        GL.Vertex3(farZ * sx, -farZ * sy, farZ);

        // far plane
        GL.Vertex3(-farZ * sx, -farZ * sy, farZ);
        GL.Vertex3(farZ * sx, -farZ * sy, farZ);

        GL.Vertex3(-farZ * sx, farZ * sy, farZ);
        GL.Vertex3(farZ * sx, farZ * sy, farZ);

        GL.Vertex3(farZ * sx, -farZ * sy, farZ);
        GL.Vertex3(farZ * sx, farZ * sy, farZ);

        GL.Vertex3(-farZ * sx, -farZ * sy, farZ);
        GL.Vertex3(-farZ * sx, farZ * sy, farZ);

        // near plane
        GL.Vertex3(-nearZ * sx, -nearZ * sy, nearZ);
        GL.Vertex3(nearZ * sx, -nearZ * sy, nearZ);

        GL.Vertex3(-nearZ * sx, nearZ * sy, nearZ);
        GL.Vertex3(nearZ * sx, nearZ * sy, nearZ);

        GL.Vertex3(nearZ * sx, -nearZ * sy, nearZ);
        GL.Vertex3(nearZ * sx, nearZ * sy, nearZ);

        GL.Vertex3(-nearZ * sx, -nearZ * sy, nearZ);
        GL.Vertex3(-nearZ * sx, nearZ * sy, nearZ);

        GL.End();
   }

    // draw frustum in solid polygons
    void DrawFrustumSolid(float sx, float sy, float nearZ, float farZ, Material mat, Color color)
    {
        // frustum vertices
        Vector3 [] v =
        {
            new Vector3(-sx*nearZ, -sy*nearZ, nearZ),
            new Vector3(sx*nearZ, -sy*nearZ, nearZ),
            new Vector3(-sx*nearZ, sy*nearZ, nearZ),
            new Vector3(sx*nearZ, sy*nearZ, nearZ),

            new Vector3(-sx*farZ, -sy*farZ, farZ),
            new Vector3(sx*farZ, -sy*farZ, farZ),
            new Vector3(-sx*farZ, sy*farZ, farZ),
            new Vector3(sx*farZ, sy*farZ, farZ),
        };

        mat.SetColor("_Color", color);

        mat.SetVector("_UAxis", new Vector3(1.0f, 0.0f, 0.0f));
        mat.SetVector("_VAxis", new Vector3(0.0f, 1.0f, 0.0f));
        mat.SetPass(0);

        // near
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0, 0); GL.Vertex(v[1]);
        GL.MultiTexCoord2(0, 1, 0); GL.Vertex(v[0]);
        GL.MultiTexCoord2(0, 1, 1); GL.Vertex(v[2]);
        GL.MultiTexCoord2(0, 0, 1); GL.Vertex(v[3]);
        GL.End();

        // far
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord3(1, 1.0f, 1.0f, 0.0f);
        GL.MultiTexCoord2(0, 0, 0); GL.Vertex(v[4]);
        GL.MultiTexCoord2(0, 1, 0); GL.Vertex(v[5]);
        GL.MultiTexCoord2(0, 1, 1); GL.Vertex(v[7]);
        GL.MultiTexCoord2(0, 0, 1); GL.Vertex(v[6]);
        GL.End();

        mat.SetVector("_UAxis", new Vector3(0.0f, 0.0f, 1.0f));
        mat.SetVector("_VAxis", new Vector3(0.0f, 1.0f, 0.0f));
        mat.SetPass(0);

        // left
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0, 0); GL.Vertex(v[0]);
        GL.MultiTexCoord2(0, 1, 0); GL.Vertex(v[4]);
        GL.MultiTexCoord2(0, 1, 1); GL.Vertex(v[6]);
        GL.MultiTexCoord2(0, 0, 1); GL.Vertex(v[2]);
        GL.End();

        // right
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0, 0); GL.Vertex(v[5]);
        GL.MultiTexCoord2(0, 1, 0); GL.Vertex(v[1]);
        GL.MultiTexCoord2(0, 1, 1); GL.Vertex(v[3]);
        GL.MultiTexCoord2(0, 0, 1); GL.Vertex(v[7]);
        GL.End();

        mat.SetVector("_UAxis", new Vector3(1.0f, 0.0f, 0.0f));
        mat.SetVector("_VAxis", new Vector3(0.0f, 0.0f, 1.0f));
        mat.SetPass(0);

        // top
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0, 0); GL.Vertex(v[3]);
        GL.MultiTexCoord2(0, 1, 0); GL.Vertex(v[2]);
        GL.MultiTexCoord2(0, 1, 1); GL.Vertex(v[6]);
        GL.MultiTexCoord2(0, 0, 1); GL.Vertex(v[7]);
        GL.End();

        // bottom
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0, 0); GL.Vertex(v[0]);
        GL.MultiTexCoord2(0, 1, 0); GL.Vertex(v[1]);
        GL.MultiTexCoord2(0, 1, 1); GL.Vertex(v[5]);
        GL.MultiTexCoord2(0, 0, 1); GL.Vertex(v[4]);
        GL.End();

    }

    // called after camera finishes rendering scene
    // render tracker frustum
    void OnRenderObject()
    {
        if (!OVRManager.tracker.isPresent)
            return;

        OVRTracker.Frustum frustum = OVRManager.tracker.GetFrustum();
        float nearZ = frustum.nearZ;
        float farZ = frustum.farZ;
        float hFOV = Mathf.Deg2Rad * frustum.fov.x * 0.5f;
        float vFOV = Mathf.Deg2Rad * frustum.fov.y * 0.5f;
        float sx = Mathf.Sin(hFOV);
        float sy = Mathf.Sin(vFOV);

        // get tracker pose
        OVRPose trackerPose = OVRManager.tracker.GetPose(0);
        Matrix4x4 trackerMat = Matrix4x4.TRS(trackerPose.position, trackerPose.orientation, Vector3.one);

        // draw frustum
        GL.PushMatrix();
        GL.MultMatrix(trackerMat);

        Color col = OVRManager.tracker.isPositionTracked ? color : errorColor;

        // draw camera
        if (drawCamera)
        {
            cameraDisplaySize = Mathf.Clamp(cameraDisplaySize, 0.0f, nearZ);
            DrawFrustumWire(sx, sy, 0.0f, cameraDisplaySize, lineMaterial, col);
        }

        if (wireframe)
        {
            DrawFrustumWire(sx, sy, nearZ, farZ, lineMaterial, col);
        }
        else
        {
            DrawFrustumSolid(sx, sy, nearZ, farZ, solidMaterial, col);
        }

        GL.PopMatrix();
    }
}
