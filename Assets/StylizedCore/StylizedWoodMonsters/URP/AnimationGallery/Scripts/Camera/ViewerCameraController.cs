using UnityEngine;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.Core;

namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.CameraControllers
{
    /// <summary>
    /// Controls the viewer camera for character previews.
    /// Includes orbit, zoom, pan, auto-rotate and presentation mode.
    /// </summary>
    public class ViewerCameraController : MonoBehaviour
    {
        [Header("TARGET")]
        /// <summary>
        /// Target model the camera will orbit around and frame.
        /// </summary>
        [SerializeField] private Transform targetModel;

        [Header("CAMERA SETTINGS")]
        /// <summary>Speed at which the camera orbits horizontally.</summary>
        [SerializeField] private float rotationSpeed = 90f;

        /// <summary>Speed used for keyboard-based zoom.</summary>
        [SerializeField] private float zoomSpeed = 2f;

        /// <summary>Speed used to tilt the camera up and down.</summary>
        [SerializeField] private float pitchSpeed = 40f;

        /// <summary>Speed used for vertical and lateral camera movement.</summary>
        [SerializeField] private float heightSpeed = 2f;

        /// <summary>Minimum allowed zoom distance.</summary>
        [SerializeField] private float minZoom = 0.5f;

        /// <summary>Maximum allowed zoom distance.</summary>
        [SerializeField] private float maxZoom = 20f;

        /// <summary>Minimum vertical tilt angle.</summary>
        [SerializeField] private float minPitch = -20f;

        /// <summary>Maximum vertical tilt angle.</summary>
        [SerializeField] private float maxPitch = 35f;

        /// <summary>Minimum camera height offset.</summary>
        [SerializeField] private float minHeight = 0.05f;

        /// <summary>Maximum camera height offset.</summary>
        [SerializeField] private float maxHeight = 5f;

        [Header("MOUSE CONTROLS")]
        /// <summary>Mouse sensitivity for orbit rotation.</summary>
        [SerializeField] private float mouseRotateSpeed = 3f;

        /// <summary>Mouse wheel sensitivity for zoom.</summary>
        [SerializeField] private float mouseZoomSpeed = 2f;

        [Header("AUTO ROTATION")]
        /// <summary>Whether the camera automatically rotates around the target.</summary>
        [SerializeField] private bool autoRotate = false;

        /// <summary>Speed of the automatic rotation.</summary>
        [SerializeField] private float autoRotateSpeed = 20f;

        /// <summary>Returns whether auto-rotation is currently enabled.</summary>
        public bool AutoRotate => autoRotate;

        [Header("PRESENTATION MODE")]
        /// <summary>Total duration of the presentation animation.</summary>
        [SerializeField] private float presentationDuration = 10f;

        /// <summary>Initial vertical look offset at presentation start.</summary>
        [SerializeField] private float lookOffsetStart = 0.1f;

        /// <summary>Final vertical look offset at presentation end.</summary>
        [SerializeField] private float lookOffsetEnd = 1.1f;

        /// <summary>Final rotation angle after the presentation completes.</summary>
        [SerializeField] private float rotationFinalTarget = 0f;

        /// <summary>True while the camera is playing a presentation animation.</summary>
        public bool IsPresenting => presentationMode;

        /// <summary>Cached reference to the active camera transform.</summary>
        private Transform cameraTransform;

        /// <summary>True while the user is rotating the camera with the mouse.</summary>
        private bool rotatingWithMouse = false;

        // ---------------- CURRENT CAMERA STATE ----------------

        /// <summary>Current zoom distance.</summary>
        private float currentZoom = 5f;

        /// <summary>Current horizontal rotation angle.</summary>
        private float currentRotation = 0f;

        /// <summary>Current vertical pitch angle.</summary>
        private float currentPitch = 10f;

        /// <summary>Current vertical camera offset.</summary>
        private float cameraHeight = 1.5f;

        /// <summary>Current look offset used during presentation.</summary>
        private float currentLookOffset = 1f;

        /// <summary>Current lateral camera offset.</summary>
        private float currentSideOffset = 0f;

        // ---------------- PRESENTATION STATE ----------------

        /// <summary>True while presentation mode is active.</summary>
        private bool presentationMode = false;

        /// <summary>Elapsed time since presentation started.</summary>
        private float presentationTime = 0f;

        // Presentation interpolation values
        private float startZoom, endZoom;
        private float startPitch, endPitch;
        private float startHeight, endHeight;
        private float startRotation, endRotation;
        private float sLookOffset, eLookOffset;

        /// <summary>Returns the currently assigned target model.</summary>
        public Transform GetTarget() { return targetModel; }

        /// <summary>Locks camera position when used in grouped shots.</summary>
        private bool lockedByGroup = false;

        /// <summary>
        /// Global flag to hide UI elements during recording or clean screenshots.
        /// </summary>
        public static bool UIHiddenGlobal = false;

        // ------------------------------------------------------------------

        void Start()
        {
            cameraTransform = UnityEngine.Camera.main.transform;
            UnityEngine.Camera.main.nearClipPlane = 0.01f;
            UpdateCameraPosition();
        }

        void Update()
        {
            HandleInput();

            if (presentationMode)
                UpdatePresentation();
            else if (autoRotate)
                currentRotation += autoRotateSpeed * Time.deltaTime;

            UpdateCameraPosition();
        }

        /// <summary>
        /// Assigns a new target model for the camera to orbit and frame.
        /// </summary>
        public void SetTarget(Transform target)
        {
            targetModel = target;
        }

        /// <summary>
        /// Handles all keyboard and mouse input for camera movement.
        /// Disabled while presentation mode is active.
        /// </summary>
        void HandleInput()
        {
            if (presentationMode) return;

            // Toggle auto-rotation
            if (Input.GetKeyDown(KeyCode.C))
            {
                autoRotate = !autoRotate;
                if (!autoRotate) currentRotation = 0f;
            }

            // Orbit rotation
            if (Input.GetKey(KeyCode.Q)) currentRotation += rotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) currentRotation -= rotationSpeed * Time.deltaTime;

            // Zoom
            if (Input.GetKey(KeyCode.W)) currentZoom -= zoomSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) currentZoom += zoomSpeed * Time.deltaTime;

            // Vertical pan
            if (Input.GetKey(KeyCode.UpArrow)) cameraHeight += heightSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow)) cameraHeight -= heightSpeed * Time.deltaTime;

            // Lateral pan
            if (Input.GetKey(KeyCode.RightArrow)) currentSideOffset -= heightSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftArrow)) currentSideOffset += heightSpeed * Time.deltaTime;

            // Pitch control
            if (Input.GetKey(KeyCode.PageUp)) currentPitch -= pitchSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.PageDown)) currentPitch += pitchSpeed * Time.deltaTime;

            // Toggle UI visibility
            if (Input.GetKeyDown(KeyCode.H))
                UIHiddenGlobal = !UIHiddenGlobal;

            // Reset camera
            if (Input.GetKeyDown(KeyCode.R)) ResetCamera();

            // Mouse zoom
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                currentZoom -= scroll * mouseZoomSpeed;

            // Mouse rotation
            if (Input.GetMouseButtonDown(2)) rotatingWithMouse = true;
            if (Input.GetMouseButtonUp(2)) rotatingWithMouse = false;

            if (rotatingWithMouse)
            {
                currentRotation -= Input.GetAxis("Mouse X") * mouseRotateSpeed * 10f;
                currentPitch += Input.GetAxis("Mouse Y") * mouseRotateSpeed * 10f;
            }

            // Clamp values
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            cameraHeight = Mathf.Clamp(cameraHeight, minHeight, maxHeight);
            currentSideOffset = Mathf.Clamp(currentSideOffset, -10f, 10f);

            // Start presentation
            if (Input.GetKeyDown(KeyCode.V)) StartPresentation();
        }

        /// <summary>
        /// Starts an automatic presentation animation based on the target model settings.
        /// </summary>
        void StartPresentation()
        {
            if (targetModel == null) return;

            presentationMode = true;
            presentationTime = 0f;

            UniversalAnimationViewer viewer = targetModel.GetComponentInChildren<UniversalAnimationViewer>();

            float h = (viewer != null) ? viewer.alturaManual : 1.8f;
            float zMult = (viewer != null) ? viewer.zoomPersonalizado : 1.8f;

            startHeight = 0.05f;
            startZoom = 0.8f;
            startPitch = 5f;
            sLookOffset = lookOffsetStart;

            endHeight = h * 0.5f;
            endZoom = h * zMult;
            endPitch = 10f;
            eLookOffset = h * lookOffsetEnd;

            startRotation = currentRotation;
            float currentMod = currentRotation % 360f;
            endRotation = currentRotation + (360f - currentMod) + rotationFinalTarget;

            Debug.Log($"[Presentation] {targetModel.name} -> Zoom {endZoom:F2} (Multiplier {zMult})");
        }

        /// <summary>
        /// Updates the presentation animation over time.
        /// </summary>
        void UpdatePresentation()
        {
            presentationTime += Time.deltaTime;
            float t = presentationTime / presentationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cameraHeight = Mathf.Lerp(startHeight, endHeight, smoothT);
            currentZoom = Mathf.Lerp(startZoom, endZoom, smoothT);
            currentPitch = Mathf.Lerp(startPitch, endPitch, smoothT);
            currentRotation = Mathf.Lerp(startRotation, endRotation, smoothT);
            currentLookOffset = Mathf.Lerp(sLookOffset, eLookOffset, smoothT);

            if (t >= 1f)
            {
                presentationMode = false;
                currentRotation = rotationFinalTarget;
                UpdateCameraPosition();
            }
        }

        /// <summary>
        /// Calculates and applies the final camera position and rotation.
        /// Uses a fixed forward-facing orientation instead of LookAt.
        /// </summary>
        void UpdateCameraPosition()
        {
            Vector3 targetPos = lockedByGroup
                ? Vector3.zero
                : (targetModel != null ? targetModel.position : transform.position);

            float rad = currentRotation * Mathf.Deg2Rad;
            float pitchRad = currentPitch * Mathf.Deg2Rad;

            float horizontalRadius = currentZoom * Mathf.Cos(pitchRad);
            float x = Mathf.Sin(rad) * horizontalRadius;
            float z = Mathf.Cos(rad) * horizontalRadius;
            float y = cameraHeight + (Mathf.Sin(pitchRad) * currentZoom);

            Vector3 finalPosition = targetPos + new Vector3(x, y, z);

            Vector3 right = new Vector3(Mathf.Cos(rad), 0, -Mathf.Sin(rad));
            finalPosition += right * currentSideOffset;

            cameraTransform.position = finalPosition;
            cameraTransform.eulerAngles = new Vector3(currentPitch, currentRotation + 180f, 0);
        }

        /// <summary>
        /// Resets the camera to its default framing based on the current target model.
        /// </summary>
        public void ResetCamera()
        {
            lockedByGroup = false;
            autoRotate = false;

            currentRotation = 0f;
            currentPitch = 10f;
            currentSideOffset = 0f;

            if (targetModel != null)
            {
                UniversalAnimationViewer viewer = targetModel.GetComponentInChildren<UniversalAnimationViewer>();
                if (viewer != null)
                {
                    cameraHeight = viewer.alturaManual * 0.5f;
                    currentZoom = viewer.alturaManual * viewer.zoomPersonalizado;
                }
                else
                {
                    cameraHeight = 1.5f;
                    currentZoom = 5f;
                }
            }

            UpdateCameraPosition();
        }

        /// <summary>
        /// Automatically adapts the camera to a model using its height and zoom multiplier.
        /// </summary>
        public void AdaptToModel(float height, float individualZoomMult)
        {
            if (presentationMode) return;

            cameraHeight = height * 0.5f;
            currentZoom = height * individualZoomMult;
            currentLookOffset = height * lookOffsetEnd;

            currentPitch = 10f;
            currentRotation = 0f;

            UpdateCameraPosition();
        }

        /// <summary>
        /// Forces a fixed camera view, typically used for grouped model shots.
        /// </summary>
        public void ForceGroupView(Vector3 pos, Vector3 rot)
        {
            lockedByGroup = true;
            presentationMode = false;

            currentRotation = rot.y;
            currentPitch = rot.x;
            cameraHeight = pos.y;
            currentZoom = pos.magnitude;

            cameraTransform.position = pos;
            cameraTransform.eulerAngles = rot;
        }
    }

}
