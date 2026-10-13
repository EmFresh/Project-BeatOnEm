/*
 File: ModelTo2DIcon.cs
 Brief: Renders a 3D GameObject into a 2D texture using a dedicated camera and assigns it to a RawImage.
 Useful for generating thumbnails/icons from in-scene models.
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
/// <summary>
/// Utility that renders a 3D GameObject to a 2D texture using a dedicated camera and assigns it to a RawImage.
/// Useful for creating icons or thumbnails from models.
/// </summary>
public partial class ModelTo2DIcon : MonoBehaviour
{

    /// <summary>
    /// Camera used to render the target object to a RenderTexture.
    /// </summary>
    public Camera renderCam;

    /// <summary>
    /// The target GameObject to render as a 2D icon.
    /// </summary>
    public GameObject targetObject;

    /// <summary>
    /// Convert the target GameObject to a 2D Texture using the configured render camera and
    /// assign it to the RawImage on this GameObject. Runs during Awake.
    /// </summary>

    // private void Update()
    // {
    //
    //     if(Keyboard.current.spaceKey.wasReleasedThisFrame)
    //         StartCoroutine(thing());
    //
    //     IEnumerator thing()
    //     {
    //         var img = new GameObject($"TEst", typeof(RawImage));
    //         img.transform.SetParent(transform, false);
    //         var trans = img.GetComponent<RectTransform>();
    //         trans.sizeDelta += new Vector2(0, 150);
    //
    //         yield return null;
    //
    //         img.GetComponent<RawImage>().texture = Convert(targetObject, Vector3.one, true);
    //     }
    // }

    /// <summary>
    /// Unity Awake callback. Converts the configured target object into a texture and assigns it to the RawImage.
    /// </summary>
    private void Awake()
    {

        GetComponent<RawImage>().texture = Convert(targetObject, renderCam, true);
    }

    /// <summary>Stores the inputs and rendered texture associated with a model-to-icon conversion.</summary>
    struct ConversionParams
    {
        /// <summary>Object rendered to produce the icon.</summary>
        public GameObject obj;
        /// <summary>Camera used to render the object.</summary>
        public Camera cam;
        /// <summary>Texture receiving the rendered image.</summary>
        public Texture2D img;
        /// <summary>Whether conversion renders a temporary instantiated object.</summary>
        public bool instantiateObj;
    }

    [AutoStaticsCleanup] private static HashSet<ConversionParams> values = new HashSet<ConversionParams>();
    /// <summary>
    /// Perform queued conversions in the static conversion set. This helper is used internally by the Convert overload.
    /// </summary>
    public static void Convert()
    {

        for(int i = 0; i < values.Count; ++i)
        {
            var param = values.ElementAt(i);
            var obj = param.obj;
            var renderCam = param.cam;
            var instantiateObj = param.instantiateObj;

            if(instantiateObj)
                obj = Instantiate(obj);

            var bounds = obj.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            //foreach(var mesh in obj.GetComponentsInChildren<MeshFilter>())
            //{
            //    var meshBounds = mesh.sharedMesh.bounds;
            //    var size = transform == mesh.transform ? meshBounds.size : transform.InverseTransformPoint(mesh.transform.TransformPoint(meshBounds.size));
            //
            //    bounds.size = Vector3.Max(bounds.size, size);
            //
            //}

            renderCam.orthographic = true;
            renderCam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y);

            obj.transform.position = (float3)renderCam.transform.position - (float3)bounds.size * (new float3(0, 0.5f, 0)) + (float3)renderCam.transform.forward;
            obj.transform.rotation.SetLookRotation(-renderCam.transform.forward, renderCam.transform.up);


            if(renderCam.targetTexture == null)
                renderCam.targetTexture = RenderTexture.GetTemporary(500, 500, 1);

            renderCam.Render();


            RenderTexture.active = renderCam.activeTexture;
            param.img = new Texture2D(renderCam.activeTexture.width, renderCam.activeTexture.height, TextureFormat.RGBA32, false);
            param.img.ReadPixels(new Rect(0, 0, param.img.width, param.img.height), 0, 0);
            param.img.Apply();



            if(instantiateObj)
                Destroy(obj);
        }

        // return img;
    }

    /// <summary>
    /// Convert a GameObject into a Texture2D by rendering it with the provided camera.
    /// </summary>
    /// <param name="obj">GameObject to render.</param>
    /// <param name="renderCam">Camera used to render the object.</param>
    /// <param name="instantiateObj">Whether to instantiate a temporary copy of the object for rendering.</param>
    /// <returns>Texture2D containing the rendered icon.</returns>
    public static Texture2D Convert(GameObject obj, Camera renderCam, bool instantiateObj = false)
    {
        var val = new ConversionParams()
        {
            img = null,
            obj = obj,
            cam = renderCam,
            instantiateObj = instantiateObj
        };

        values.Add(val);
        Convert();
        values.Remove(val);

        return val.img;
    }
}