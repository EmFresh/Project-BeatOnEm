using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Timeline;

using System.Linq;

[RequireComponent(typeof(EventTrigger))]
public class TempoMark : MonoBehaviour
{
    public double time;
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
                //  print($"Pointer Entered Tempo Mark at time: {time}");

            })
        );
    }
}
