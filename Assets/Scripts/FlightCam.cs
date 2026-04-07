using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class FlightCam : MonoBehaviour
{
    [SerializeField]
    private bool orbitEnabled = true;

    [SerializeField]
    [Min(0f)]
    private float speedDegreesPerSecond = 30f;

    [SerializeField]
    private Vector3 orbitDirection = Vector3.up;

    [SerializeField]
    [Range(0f, 15f)]
    private float maxYawDegrees = 15f;

    [SerializeField]
    [Range(0f, 15f)]
    private float maxPitchDegrees = 15f;

    [SerializeField]
    [Min(0.01f)]
    private float smoothTime = 0.12f;

    private Quaternion centerLocalRotation;
    private float currentYaw;
    private float currentPitch;
    private float yawVelocity;
    private float pitchVelocity;

#if UNITY_EDITOR
    private double lastEditorUpdateTime;
#endif

    private void OnEnable()
    {
        centerLocalRotation = transform.localRotation;

#if UNITY_EDITOR
        lastEditorUpdateTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= OnEditorUpdate;
#endif
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            ApplyOrbit(Time.deltaTime);
        }
    }

    private void ApplyOrbit(float deltaTime)
    {
        if (!orbitEnabled || deltaTime <= 0f)
            return;

        Transform parent = transform.parent;
        if (parent == null)
            return;

        Vector3 axis = orbitDirection.sqrMagnitude > 0f ? orbitDirection.normalized : Vector3.up;
        transform.RotateAround(parent.position, axis, speedDegreesPerSecond * deltaTime);
    }

#if UNITY_EDITOR
    private void OnEditorUpdate()
    {
        if (Application.isPlaying)
        {
            lastEditorUpdateTime = EditorApplication.timeSinceStartup;
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(now - lastEditorUpdateTime);
        lastEditorUpdateTime = now;

        ApplyOrbit(deltaTime);
        EditorApplication.QueuePlayerLoopUpdate();
    }
#endif
}
