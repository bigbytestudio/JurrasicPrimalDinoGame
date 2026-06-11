using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CinemachineTouchController : MonoBehaviour
{
    public TouchField touchField;
    public float xSpeed = 0.5f;
    public float ySpeed = 0.5f;
    public bool invertY = true;
    public bool enableFallbackPointerInput;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera responds to input. Lower = smoother/more damped, Higher = snappier.")]
    public float smoothTime = 0.12f;

    private CinemachineOrbitalFollow _orbital;

    // Current smoothed velocity for each axis
    float _hVelocity;
    float _vVelocity;

    // Smoothed target values
    float _hTarget;
    float _vTarget;
    float hCurrent;
    float smoothH;
    float smoothV;
    float _initialHTarget;
    float _initialVTarget;

    public bool b_InCombat;

    private int _fallbackTouchId = -1;
    private Vector2 _fallbackLastPosition;
    private bool _fallbackPointerActive;

    private void Awake()
    {
        _orbital = GetComponent<CinemachineOrbitalFollow>();
        if (_orbital != null)
        {
            // Seed targets from the current axis values so SmoothDamp starts with no jump
            _hTarget = _orbital.HorizontalAxis.Value;
            _vTarget = _orbital.VerticalAxis.Value;
            _initialHTarget = _hTarget;
            _initialVTarget = _vTarget;

            // Ensure Cinemachine isn't fighting your script
            // by trying to move the axis on its own.
        }
    }

    private void LateUpdate()
    {
        if (_orbital == null || b_InCombat) return;

        Vector2 delta = GetInputDelta();

        // Accumulate raw input into targets
        _hTarget += delta.x * xSpeed;
        _vTarget += (invertY ? -delta.y : delta.y) * ySpeed;

        // Clamp targets to axis ranges
        if (!_orbital.HorizontalAxis.Wrap)
            _hTarget = Mathf.Clamp(_hTarget, _orbital.HorizontalAxis.Range.x, _orbital.HorizontalAxis.Range.y);

        _vTarget = Mathf.Clamp(_vTarget, _orbital.VerticalAxis.Range.x, _orbital.VerticalAxis.Range.y);

        // Smoothly move current axis values toward targets.
        // For the horizontal axis we SmoothDamp on the unbounded _hTarget so
        // the interpolation never takes the long way around the ±180 seam.
        // We derive a "current" value in the same unbounded space by offsetting
        // from _hTarget by however far the axis currently lags behind.
        hCurrent = _hTarget - Mathf.DeltaAngle(_orbital.HorizontalAxis.Value, StandardizeAngle(_hTarget));
        smoothH = Mathf.SmoothDamp(hCurrent, _hTarget, ref _hVelocity, smoothTime);
        smoothV = Mathf.SmoothDamp(_orbital.VerticalAxis.Value, _vTarget, ref _vVelocity, smoothTime);

        // Apply Horizontal — only wrap the final value written to the axis
        if (_orbital.HorizontalAxis.Wrap)
        {
            _orbital.HorizontalAxis.Value = StandardizeAngle(smoothH);
            // NOTE: _hTarget is intentionally NOT wrapped — it stays unbounded
            //       so SmoothDamp always travels the short linear path.
        }
        else
        {
            _orbital.HorizontalAxis.Value = smoothH;
        }

        // Apply Vertical
        _orbital.VerticalAxis.Value = smoothV;
    }

    // Helper to keep angles between -180 and 180 for horizontal wrapping
    private float StandardizeAngle(float angle)
    {
        // Replaces while loops with a single wrap-around calculation
        return Mathf.Repeat(angle + 180f, 360f) - 180f;
        //while (angle > 180) angle -= 360;
        //while (angle < -180) angle += 360;
        //return angle;
    }

    private Vector2 GetInputDelta()
    {
        if (touchField != null && touchField.isActiveAndEnabled && touchField.IsTouching)
        {
            ResetFallbackPointer();
            return touchField.TouchDelta;
        }

        return enableFallbackPointerInput ? GetFallbackPointerDelta() : Vector2.zero;
    }

    private Vector2 GetFallbackPointerDelta()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.isInProgress)
                {
                    continue;
                }

                int touchId = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();

                if (!_fallbackPointerActive || _fallbackTouchId != touchId)
                {
                    _fallbackPointerActive = true;
                    _fallbackTouchId = touchId;
                    _fallbackLastPosition = position;
                    return Vector2.zero;
                }

                Vector2 delta = position - _fallbackLastPosition;
                _fallbackLastPosition = position;
                return delta * (touchField != null ? touchField.sensitivity : 1f);
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 position = Mouse.current.position.ReadValue();

            if (!_fallbackPointerActive || _fallbackTouchId != -2)
            {
                _fallbackPointerActive = true;
                _fallbackTouchId = -2;
                _fallbackLastPosition = position;
                return Vector2.zero;
            }

            Vector2 delta = position - _fallbackLastPosition;
            _fallbackLastPosition = position;
            return delta * (touchField != null ? touchField.sensitivity : 1f);
        }

        ResetFallbackPointer();
        return Vector2.zero;
    }

    private void ResetFallbackPointer()
    {
        _fallbackTouchId = -1;
        _fallbackPointerActive = false;
        _fallbackLastPosition = Vector2.zero;
    }

    public void ResetToInitialOrbit()
    {
        if (_orbital == null) _orbital = GetComponent<CinemachineOrbitalFollow>();
        if (_orbital == null) return;

        _hTarget = _initialHTarget;
        _vTarget = _initialVTarget;

        _hVelocity = 0f;
        _vVelocity = 0f;
        hCurrent = _hTarget;
        smoothH = _hTarget;
        smoothV = _vTarget;

        _orbital.HorizontalAxis.Value = _orbital.HorizontalAxis.Wrap ? StandardizeAngle(_initialHTarget) : _initialHTarget;
        _orbital.VerticalAxis.Value = Mathf.Clamp(_initialVTarget, _orbital.VerticalAxis.Range.x, _orbital.VerticalAxis.Range.y);

        ResetFallbackPointer();
    }

    public void SetInitialOrbitFromWorldPose(Vector3 cameraPosition, Transform target)
    {
        if (_orbital == null) _orbital = GetComponent<CinemachineOrbitalFollow>();
        if (_orbital == null || target == null) return;

        Vector3 offset = cameraPosition - target.position;
        Vector3 flatOffset = new Vector3(offset.x, 0f, offset.z);

        if (flatOffset.sqrMagnitude > 0.0001f)
        {
            float targetToCameraAngle = Mathf.Atan2(flatOffset.x, flatOffset.z) * Mathf.Rad2Deg;
            _initialHTarget = StandardizeAngle(targetToCameraAngle + 180f);
        }

        if (offset.sqrMagnitude > 0.0001f)
        {
            float verticalAngle = Mathf.Atan2(offset.y, flatOffset.magnitude) * Mathf.Rad2Deg;
            _initialVTarget = Mathf.Clamp(verticalAngle, _orbital.VerticalAxis.Range.x, _orbital.VerticalAxis.Range.y);
        }

        ResetToInitialOrbit();
    }

    /// <summary>
    /// Get the current horizontal axis value from the orbital component.
    /// Call this before you lock the camera so you can restore the same angle later.
    /// </summary>
    public float GetHorizontalAngle()
    {
        if (_orbital == null) _orbital = GetComponent<CinemachineOrbitalFollow>();
        return _orbital != null ? _orbital.HorizontalAxis.Value : 0f;
    }

    /// <summary>
    /// Restore the orbital horizontal angle to a previously cached value.
    /// This sets both the Cinemachine axis and the internal smoothing target so
    /// the touch controller won't immediately drive the camera away from the restored angle.
    /// </summary>
    public void RestoreHorizontalAngle(float angle)
    {
        if (_orbital == null) _orbital = GetComponent<CinemachineOrbitalFollow>();
        if (_orbital == null) return;

        // If wrapping is enabled, keep the written axis within -180..180
        float writeValue = _orbital.HorizontalAxis.Wrap ? StandardizeAngle(angle) : angle;

        // Set the axis immediately so the camera snaps to the cached angle
        _orbital.HorizontalAxis.Value = writeValue;

        // Also update the controller's smoothing target so it doesn't interpolate back
        // to the old _hTarget on the next frame.
        _hTarget = angle; // keep unbounded target so smoothing travels the short way

        // Reset smoothing velocity to avoid momentum carrying it away
        _hVelocity = 0f;

        // Recompute hCurrent so SmoothDamp continues smoothly from this new value
        hCurrent = _hTarget - Mathf.DeltaAngle(_orbital.HorizontalAxis.Value, StandardizeAngle(_hTarget));
    }
}
