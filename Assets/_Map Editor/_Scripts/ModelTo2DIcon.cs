using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ModelTo2DIcon : MonoBehaviour
{

    public Camera renderCam;

    public GameObject targetObject;

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

    private void Awake()
    {

        GetComponent<RawImage>().texture = Convert(targetObject, renderCam, true);
    }

    struct ConversionParams
    {
        public GameObject obj;
        public Camera cam;
        public Texture2D img;
        public bool instantiateObj;
    }

    private static HashSet<ConversionParams> values = new HashSet<ConversionParams>();
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