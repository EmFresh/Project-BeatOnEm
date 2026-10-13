using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Timeline;

using System.Linq;

[RequireComponent(typeof(EventTrigger))]
[RequireComponent(typeof(EventTrigger))]
/// <summary>
/// A tempo mark placed on the timeline that can be clicked to seek the playback to its time.
/// </summary>
public class TempoMark : MonoBehaviour
{
    /// <summary>
    /// Time in seconds represented by this tempo mark.
    /// </summary>
    public double time;

    /// <summary>
    /// Setup click handling to move the parent SeekBar to this mark's time.
    /// </summary>
    void Awake()
    {
        SeekBar seekBar = GetComponentInParent<SeekBar>();

        var eventTriggers = GetComponent<EventTrigger>();
        eventTriggers.triggers.Add(new EventTrigger.Entry() { eventID = EventTriggerType.PointerClick });
        eventTriggers.triggers.Last().callback.AddListener(
            new UnityAction<BaseEventData>((data) =>
            {
                PointerEventData ped = (PointerEventData)data;

                if(ped.button == PointerEventData.InputButton.Left)
                    seekBar.SetLocation(time);

            })
        );
    }
}
