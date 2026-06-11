using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchField : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")] public float sensitivity = 0.5f;

    public event Action PointerDown;
    public event Action PointerUp;

    // The current delta movement of the finger
    //public Vector2 TouchDelta { get; private set; }
    public Vector2 TouchDelta;

    // Check if currently being touched
    public bool IsTouching => _pointerId != -100;

    // Internal tracker for the specific finger ID
    private int _pointerId = -100; // -100 means "no finger"
    private bool _ignoreNextDrag;

    private void Update()
    {
        // Clear the delta from the previous frame at the start of this frame
        if (_pointerId == -100)
        {
            TouchDelta = Vector2.zero;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // If we aren't already tracking a finger, claim this one!

       // Debug.Log($"Pointer Down! pointerId={eventData.pointerId}");
        if (_pointerId == -100)
        {
            _pointerId = eventData.pointerId;
            TouchDelta = Vector2.zero;
            _ignoreNextDrag = true;
            PointerDown?.Invoke();

            // Optional: If you want to stop the "initial touch" from jerking the camera
            // you can leave TouchDelta as zero here.
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Only trust the finger we claimed in OnPointerDown
         //   Debug.Log($"Dragging! delta={eventData.delta}");


        if (eventData.pointerId == _pointerId)
        {
            if (_ignoreNextDrag)
            {
                _ignoreNextDrag = false;
                TouchDelta = Vector2.zero;
                return;
            }

            TouchDelta = eventData.delta * sensitivity;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // If the finger we were tracking is lifted, reset everything
        if (eventData.pointerId == _pointerId)
        {
            _pointerId = -100;
            TouchDelta = Vector2.zero;
            _ignoreNextDrag = false;
            PointerUp?.Invoke();
        }
    }
}
