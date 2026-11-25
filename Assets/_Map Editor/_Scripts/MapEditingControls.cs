/*
 File: MapEditingControls.cs
 Brief: Handles input and editing controls used by the map editor (selection, deletion, navigation, etc.).
*/

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

/// <summary>
/// Controls map editing input and interactions for the map editor UI.
/// Placeholder summary: TODO: Document MapEditingControls responsibilities in detail.
/// </summary>
public class MapEditingControls : MonoBehaviour
{
    /// <summary>
    /// Reference to the MapEditManager that handles map editing operations.
    /// </summary>
    public MapEditManager mapEditManager;

    /// <summary>
    /// InputActionAsset containing input action maps used by the map editor.
    /// </summary>
    public InputActionAsset inputActions;

    /// <summary>
    /// Enable input actions when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        inputActions?.Enable();
    }

    /// <summary>
    /// Disable input actions when the component is disabled.
    /// </summary>
    private void OnDisable()
    {
        inputActions?.Disable();
    }

    /// <summary>
    /// Temporary list used to store UI raycast results.
    /// </summary>
    List<RaycastResult> results = new List<RaycastResult>();
    /// <summary>
    /// Handles timeline enhancement input (e.g. zoom/scale) from the input system.
    /// </summary>
    /// <param name="input">CallbackContext provided by the Input System for the action.</param>
    public void EnhanceTimeline(CallbackContext input)
    {
        results.Clear();


        if(input.started)
        {

        }
        if(input.canceled)
        {


        }
        try
        {
            if(input.performed)
            {
                // Raycast at the pointer to locate the timeline under the user's cursor.
                var pointer = new PointerEventData(EventSystem.current);
                pointer.position = Mouse.current.position.value;
                EventSystem.current.RaycastAll(pointer, results);
                var tmp = results.Select(ting => ting.gameObject.name);

                // Safely find the raycast hit that contains a TimelineImageCreator. Avoid accessing .gameObject on a null result.
                var hit = results.Find(ting => ting.gameObject.GetComponent<TimelineImageCreator>() != null);

                if(hit.gameObject == null) return;

                var timeline = hit.gameObject;
                var scrollRect = timeline.GetComponent<ScrollRect>();

                  pointer.position = scrollRect.content.InverseTransformPoint(pointer.position);





                // Toast.SendToast($"Timeline: \n{pointer.position}");


                // Convert the pointer to content space and normalize its position for zoom anchoring.
                var vert = mapEditManager.tempoMarkManager.vertical;
                var val = input.ReadValue<Vector2>();
                var scrollPos = scrollRect.normalizedPosition;
                var viewportSize = scrollRect.viewport.rect.size;

                var dif = pointer.position - scrollRect.content.rect.min;
                
                var normPos = ((dif) / scrollRect.content.rect.size);
                normPos.y = vert ? normPos.y : 0;
                normPos.x = vert ? 0 : normPos.x;

                
                // Scale only the scrollable axis, leaving the orthogonal content dimension unchanged.
                var timelineRect = timeline.GetComponent<RectTransform>().rect.size;
                var oldContentSize = scrollRect.content.rect.size;
                var deltaSize = new Vector2(
                    scrollRect.vertical ? 0f : timelineRect.x,
                    scrollRect.horizontal ? 0f : timelineRect.y) * val.y;

                var newContentSize = oldContentSize + deltaSize;

                // If there's no effective size change, nothing to do
                if(vert ? Mathf.Approximately(deltaSize.y, 0f) : Mathf.Approximately(deltaSize.x, 0f))
                    return;

                // Apply the new dimensions, then offset content so the point under the cursor stays fixed.
                scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newContentSize.x);
                scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newContentSize.y);

                // Compute how much the content changed and adjust anchoredPosition so the point under the cursor stays fixed.
                var pivot = scrollRect.content.pivot;

                // normalized position within content where the pointer is (0..1), computed earlier as normPos
                // Change in anchored position = -(newSize - oldSize) * (normPos - pivot)
                 var anchoredDelta = -new Vector3(
                    deltaSize.x * (normPos.x - pivot.x),
                    deltaSize.y * (normPos.y - pivot.y),
                    0f
                );

                scrollRect.content.localPosition += anchoredDelta;
                 
            }
        }
        catch(Exception e) { Toast.SendToast($"Error occurred: {e.Message}\n\n{e.StackTrace}", type: Toast.ToastType.Error); }
    }

    /// <summary>
    /// Performs an undo operation through the MapEditManager when the undo action is performed.
    /// </summary>
    /// <param name="context">Input CallbackContext for the undo action.</param>
    public void Undo(CallbackContext context)
    {
        if(context.performed)
            mapEditManager.Undo();

    }

    /// <summary>
    /// Performs a redo operation through the MapEditManager when the redo action is performed.
    /// </summary>
    /// <param name="context">Input CallbackContext for the redo action.</param>
    public void Redo(CallbackContext context)
    {
        if(context.performed)
            mapEditManager.Redo();

    }

    /// <summary>
    /// Saves the current map via the MapEditManager when the save action is performed.
    /// </summary>
    /// <param name="context">Input CallbackContext for the save action.</param>
    public void Save(CallbackContext context)
    {
        if(context.performed)
            mapEditManager.SaveFile();
    }
}
