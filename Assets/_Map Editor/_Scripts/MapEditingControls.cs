

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using static UnityEngine.InputSystem.InputAction;
using UnityEngine.UIElements;

public class MapEditingControls : MonoBehaviour
{
    public MapEditManager mapEditManager;
    public InputActionAsset inputActions;

    private void OnEnable()
    {
        inputActions?.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    List<RaycastResult> results = new List<RaycastResult>(); 
    public void EnhanceTimeline(CallbackContext input)
    {
        results.Clear();
        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = Mouse.current.position.ReadValue();
        EventSystem.current.RaycastAll(pointer, results);

        var tmp = results.Select(ting => ting.gameObject.name);
        var timeline = results.Find(ting => ting.gameObject.GetComponent<TimelineImageCreator>() != null).gameObject;

        if(timeline == null) return;
        var scrollRect = timeline.GetComponent<ScrollRect>();

        if(input.started)
        {

        }
        if(input.canceled)
        {


        }

        if(input.performed && timeline)
        {
            var vert = mapEditManager.tempoMarkManager.vertical;
            var val = input.ReadValue<Vector2>();
            float pos = Mathf.Clamp01(vert ? 1 - scrollRect.normalizedPosition.y : scrollRect.normalizedPosition.x);

            
            var delta = scrollRect.content.sizeDelta +
                    new Vector2(
                        scrollRect.vertical ? 0 : timeline.GetComponent<RectTransform>().sizeDelta.x,
                        scrollRect.horizontal ? 0 : timeline.GetComponent<RectTransform>().sizeDelta.y) * val.y;

            var sizeCheck = delta - scrollRect.GetComponent<RectTransform>().sizeDelta;

            if(vert ? sizeCheck.y < 0 : sizeCheck.x < 0) return;

            scrollRect.content.sizeDelta = delta;

            var size = scrollRect.GetComponent<RectTransform>().sizeDelta;



            scrollRect.content.anchoredPosition =
                new Vector2(vert ? scrollRect.content.anchoredPosition.x :
                (delta.x - scrollRect.GetComponent<RectTransform>().sizeDelta.x) * pos,
                vert ? (delta.y - scrollRect.GetComponent<RectTransform>().sizeDelta.y) * pos :
                scrollRect.content.anchoredPosition.y);

            print($"Event triggered val: {scrollRect.normalizedPosition.y}");
            //EnhanceTimeline();
        }
    }


}
