using BezierSolution;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SPEngine
{
    public class VR
    {
        public RaycastHit hitObject;


        public GameObject PlayerControllerGameObject
        {
            get
            {
                return _playerControllerGameObject;
            }

            set
            {
                _playerControllerGameObject = value;
                InitialisePlayerController();
            }
        }

        public List<GameObject> FlockGameObjects
        {
            get
            {
                return _flockGameObjects;
            }

            set
            {
                _flockGameObjects = value;
                InitialiseFlockingAI();
            }
        }

        public bool EnableFreeMovement = false;

        /// <summary>
        /// The rate acceleration during movement.
        /// </summary>
        public float Acceleration = 0.1f;

        /// <summary>
        /// Modifies the strength of gravity.
        /// </summary>
        public float GravityModifier = 0.379f;

        public float separationWeight = 0.8f;
        public float alignmentWeight = 0.5f;
        public float cohesionWeight = 0.7f;

        /// <summary>
        /// The rate of damping on movement.
        /// </summary>
        private float Damping = 0.3f;


        /// <summary>
        /// The rate of rotation when using a gamepad.
        /// </summary>
        private float RotationAmount = 1.5f;

        /// <summary>
        /// The rate of rotation when using the keyboard.
        /// </summary>
        private float RotationRatchet = 45.0f;


        /// <summary>
        /// If true, reset the initial yaw of the player controller when the Hmd pose is recentered.
        /// </summary>
        private bool HmdResetsY = true;

        /// <summary>
        /// If true, tracking data from a child OVRCameraRig will update the direction of movement.
        /// </summary>
        private bool HmdRotatesY = true;

        

        /// <summary>
        /// If true, each OVRPlayerController will use the player's physical height.
        /// </summary>
        private bool useProfileData = false;

        /// <summary>
        /// The CameraHeight is the actual height of the HMD and can be used to adjust the height of the character controller, which will affect the
        /// ability of the character to move into areas with a low ceiling.
        /// </summary>
        [NonSerialized]
        private float CameraHeight;


        /// <summary>
        /// This bool is set to true whenever the player controller has been teleported. It is reset after every frame. Some systems, such as
        /// CharacterCameraConstraint, test this boolean in order to disable logic that moves the character controller immediately
        /// following the teleport.
        /// </summary>
        [NonSerialized] // This doesn't need to be visible in the inspector.
        private bool Teleported;


        protected CharacterController Controller = null;
        protected OVRCameraRig CameraRig = null;


        private OVRPose? InitialPose;
        private float InitialYRotation { get; set; }
        private float MoveScaleMultiplier = 1.0f;
        private float RotationScaleMultiplier = 1.0f;
        private bool SkipMouseRotation = true; // It is rare to want to use mouse movement in VR, so ignore the mouse by default.
        private bool HaltUpdateMovement = false;

        private bool ReadyToSnapTurn; // Set to true when a snap turn has occurred, code requires one frame of centered thumbstick to enable another snap turn.

        private OVRInput.Button runButton = OVRInput.Button.One;
        private OVRInput.Button alternateRunButton = OVRInput.Button.PrimaryThumbstick;

        private bool rotationSnap = false;
        float PendingRotation = 0;
        float SimulationRate_ = 60f;
        private bool prevHatLeft_;
        private bool prevHatRight_;
        private OVRPose? InitialPose_;
        private Vector3 MoveThrottle_ = Vector3.zero;
        private float FallSpeed_ = 0.0f;
        private float InitialYRotation_ = 0.0f;

        private float axisDeadZone = 0.1f;

        float rotationAnimation = 0;
        float targetYaw = 0;
        bool animating;

        private GameObject _playerControllerGameObject;
        private List<GameObject> _flockGameObjects;

        private List<Vector3> flockGameObjectVelocityList;
        private const float neighbourSquaredDistance = 5.0f;
        private const float maxVelocity = 1.0f;
        private bool initialisedPlayerController = false;

        //Rollercoaster

        public enum TravelMode { Once, Loop, PingPong };


        public static readonly int TRAVEL_MODE_ONCE = 1;
        public static readonly int TRAVEL_MODE_LOOP = 2;
        public static readonly int TRAVEL_MODE_TO_AND_FRO = 3;

        private float progress = 0f;

        //private float movementLerpModifier = 10f;
        private float rotationLerpModifier = 10f;

        private bool lookForward = true;

        private bool isGoingForward = true;

        private UnityEvent onPathCompleted = new UnityEvent();

        [Range(0f, 0.06f)]
        private float relaxationAtEndPoints = 0.01f;

        private bool onPathCompletedCalledAt1 = false;
        private bool onPathCompletedCalledAt0 = false;

        public VR()
        {

        }

        private void InitialisePlayerController()
        {
            HmdResetsY = false;
            Acceleration = 0.1f;
            useProfileData = false;

            Controller = _playerControllerGameObject.GetComponent<CharacterController>();

            if (Controller == null)
                Debug.LogWarning("OVRPlayerController: No CharacterController attached.");

            // We use OVRCameraRig to set rotations to cameras,
            // and to be influenced by rotation
            List<OVRCameraRig> cameraRigs = new List<OVRCameraRig>();
            foreach (Transform child in _playerControllerGameObject.transform)
            {
                OVRCameraRig childCameraRig = child.gameObject.GetComponent<OVRCameraRig>();
                if (childCameraRig != null)
                {
                    cameraRigs.Add(childCameraRig);
                }
            }

            if (cameraRigs.Count == 0)
                Debug.LogWarning("OVRPlayerController: No OVRCameraRig attached.");
            else if (cameraRigs.Count > 1)
                Debug.LogWarning("OVRPlayerController: More then 1 OVRCameraRig attached.");
            else
                CameraRig = cameraRigs[0];

            InitialYRotation_ = _playerControllerGameObject.transform.rotation.eulerAngles.y;
        }

        public void InitialiseFlockingAI()
        {
            if (_flockGameObjects.Count == 0)
            {
                Debug.LogError("No flockObjects found. Please initialise the flockObjects of SPEngine.VR from your script");
            }
            flockGameObjectVelocityList = new List<Vector3>();
            foreach (GameObject flockGameObject in _flockGameObjects)
            {
                Vector3 velocity = flockGameObject.transform.forward;
                velocity = Vector3.ClampMagnitude(velocity, maxVelocity);
                flockGameObjectVelocityList.Add(velocity);

            }
        }
        public void UseFlockingAI()
        {
            int count = 0;

            foreach (GameObject flockGameObject in _flockGameObjects)
            {
                flockGameObjectVelocityList[count] += CalculateFlockingBehaviourVelocity(count);

                flockGameObjectVelocityList[count] = Vector3.ClampMagnitude(flockGameObjectVelocityList[count], maxVelocity);

                flockGameObject.transform.position += flockGameObjectVelocityList[count] * Time.deltaTime;

                flockGameObject.transform.forward = flockGameObjectVelocityList[count].normalized; // https://docs.unity3d.com/ScriptReference/Transform-forward.html

                count++;
            }

        }

        private Vector3 CalculateFlockingBehaviourVelocity(int currentObjectIndex)
        {
            //print("FIshFlock.instance: " + FishFlock.instance);

            Vector3 cohesionVector = new Vector3();
            Vector3 separateVector = new Vector3();
            Vector3 alignmentVector = new Vector3();

            int count = 0;
           
            Transform currentObjectTransform = _flockGameObjects[currentObjectIndex].transform;

            for (int index = 0; index < _flockGameObjects.Count; index++)
            {

                if (index != currentObjectIndex)
                {
                    float distance = (currentObjectTransform.position - _flockGameObjects[index].transform.position).sqrMagnitude;

                    if (distance > 0 && distance < neighbourSquaredDistance)
                    {
                        cohesionVector += _flockGameObjects[index].transform.position;
                        separateVector += _flockGameObjects[index].transform.position - currentObjectTransform.position;
                        alignmentVector += _flockGameObjects[index].transform.forward;

                        count++;
                    }
                }
            }
            

            if (count == 0)
            {
                return Vector3.zero;
            }

            // separation step
            separateVector /= count;
            separateVector *= -1;

            // forward step
            alignmentVector /= count;

            // cohesion step
            cohesionVector /= count;
            cohesionVector = (cohesionVector - currentObjectTransform.position);

            // Add All vectors together to get flocking
            Vector3 flockingVector = ((separateVector.normalized * separationWeight) +
                                        (cohesionVector.normalized * cohesionWeight) +
                                        (alignmentVector.normalized * alignmentWeight));

            return flockingVector;
        }

        // Check if the line between start and end intersect a collider.
        public bool CheckObjectIntersection(Vector3 start, Vector3 end)
        {
            Ray r = new Ray(start, end - start);
            RaycastHit hit;
            if (Physics.Raycast(r, out hit, Vector3.Distance(start, end)))
            {

                hitObject = hit;
                return true;
            }

            return false;
        }

        public void BuildRollercoasterTrack(GameObject trackGameObject, GameObject splineRootGameObject, GameObject leftRailPrefab, GameObject rightRailPrefab, GameObject crossBeamPrefab, float resolution)
        {


            BezierSpline bezierSpline = splineRootGameObject.GetComponent<BezierSpline>();
            // Delete all of the children of the track holding game object
            List<Component> childComponents = new List<Component>(trackGameObject.GetComponentsInChildren(typeof(Transform))); //Returns all components of Type type in the GameObject and any of its children.
            List<Transform> childTransforms = childComponents.ConvertAll(c => (Transform)c);
            childTransforms.Remove(trackGameObject.transform); //Remove the current game object (parent)'s transform from the list. We need only the transforms of the children
            foreach (Transform childTransform in childTransforms)
            {
                if (childTransform.gameObject.name == "left rail" || childTransform.gameObject.name == "right rail")
                {
                    UnityEngine.Object.DestroyImmediate(childTransform.gameObject.GetComponent<MeshFilter>().sharedMesh);
                }
                UnityEngine.Object.DestroyImmediate(childTransform.gameObject); //Destroy cannot be called in edit mode
            }


            // Build a list of affine transformation matricies that represent the track sections
            List<Matrix4x4> leftTrackPolyline = new List<Matrix4x4>();
            List<Matrix4x4> rightTrackPolyline = new List<Matrix4x4>();


            //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube.transform.parent = transform;
            //cube.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            //cube.transform.position = bezierSpline.GetPoint(0);
            //cube.transform.localRotation = Quaternion.LookRotation(bezierSpline.GetTangent(0));

            //GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube2.transform.parent = transform;
            //cube2.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            //cube2.transform.position = bezierSpline.GetPoint(1);
            //cube2.transform.localRotation = Quaternion.LookRotation(bezierSpline.GetTangent(1));


            for (float t = 0; t <= 1; t += resolution)
            {

                //print("T: " + t);
                Transform trans = new GameObject().transform;

                trans.position = bezierSpline.GetPoint(t);
                trans.rotation = Quaternion.LookRotation(bezierSpline.GetTangent(t));

                leftTrackPolyline.Add(trans.localToWorldMatrix * leftRailPrefab.transform.localToWorldMatrix);
                rightTrackPolyline.Add(trans.localToWorldMatrix * rightRailPrefab.transform.localToWorldMatrix);
                UnityEngine.Object.DestroyImmediate(trans.gameObject);

                //Debug.Log(trans.localToWorldMatrix);

                float t2 = t + resolution + resolution;
                if (bezierSpline.loop && t2 >= 1)
                {
                    Transform trans1 = new GameObject().transform;

                    trans1.position = bezierSpline.GetPoint(0);
                    trans1.rotation = Quaternion.LookRotation(bezierSpline.GetTangent(0));

                    leftTrackPolyline.Add(trans1.localToWorldMatrix * leftRailPrefab.transform.localToWorldMatrix);
                    rightTrackPolyline.Add(trans1.localToWorldMatrix * rightRailPrefab.transform.localToWorldMatrix);
                    UnityEngine.Object.DestroyImmediate(trans1.gameObject);

                    break;
                }
            }


            // Generate the rails
            GameObject leftRail = new GameObject();
            Mesh leftMesh = new Mesh();
            leftRail.name = "left rail";
            leftRail.transform.parent = trackGameObject.transform;
            leftRail.AddComponent<MeshFilter>();
            leftRail.GetComponent<MeshFilter>().sharedMesh = leftMesh; // Not allowed to access MeshFilter.mesh on prefab object. So Use MeshFilter.sharedMesh
            leftRail.AddComponent<MeshRenderer>();
            leftRail.GetComponent<MeshRenderer>().sharedMaterial = leftRailPrefab.GetComponent<MeshRenderer>().sharedMaterial; // Not allowed to access MeshRenderer.material on prefab object. So Use MeshRenderer.sharedMaterial
            MeshExtrusion.ExtrudeMesh(leftRailPrefab.GetComponent<MeshFilter>().sharedMesh, leftRail.GetComponent<MeshFilter>().sharedMesh, leftTrackPolyline.ToArray(), false);

            GameObject rightRail = new GameObject();
            Mesh rightMesh = new Mesh();
            rightRail.name = "right rail";
            rightRail.transform.parent = trackGameObject.transform;
            rightRail.AddComponent<MeshFilter>();
            rightRail.GetComponent<MeshFilter>().sharedMesh = rightMesh;
            rightRail.AddComponent<MeshRenderer>();
            rightRail.GetComponent<MeshRenderer>().sharedMaterial = rightRailPrefab.GetComponent<MeshRenderer>().sharedMaterial;
            MeshExtrusion.ExtrudeMesh(rightRailPrefab.GetComponent<MeshFilter>().sharedMesh, rightRail.GetComponent<MeshFilter>().sharedMesh, rightTrackPolyline.ToArray(), false);


            // generateCrossBeams

            if (crossBeamPrefab != null)
            {
                // Generate the cross bars
                float distSinceLastCrossbar = 0;
                float cbRes = resolution / 5.0f;
                float beamDistance = 2.0f;
                for (float t = 0; t <= 1; t += cbRes)
                {
                    Vector3 dP = bezierSpline.GetPoint(t) - bezierSpline.GetPoint(t - cbRes);
                    distSinceLastCrossbar += dP.magnitude;
                    if (distSinceLastCrossbar >= beamDistance)
                    {
                        GameObject crossbar = UnityEngine.Object.Instantiate(crossBeamPrefab); //Creates a gameObject from the prefab
                        crossbar.transform.parent = trackGameObject.transform;
                        crossbar.transform.position = bezierSpline.GetPoint(t);
                        crossbar.transform.rotation = Quaternion.LookRotation(bezierSpline.GetTangent(t));

                        crossbar.transform.position += crossbar.transform.right * crossBeamPrefab.transform.position.x;
                        crossbar.transform.position += crossbar.transform.up * crossBeamPrefab.transform.position.y;
                        crossbar.transform.position += crossbar.transform.forward * crossBeamPrefab.transform.position.z;

                        crossbar.transform.rotation *= crossBeamPrefab.transform.rotation;

                        distSinceLastCrossbar -= beamDistance;
                    }
                }
            }
        }


        public void MoveAlongSpline(GameObject splineGameObject, GameObject objectToMove, float speed, int travelMode)
        {
            BezierSpline spline = splineGameObject.GetComponent<BezierSpline>();
            float targetSpeed = (isGoingForward) ? speed : -speed;

            Vector3 targetPos;
            // Code below uses the obsolete MoveAlongSpline function
            //float absSpeed = Mathf.Abs( speed );
            //if( absSpeed <= 2f )
            //	targetPos = spline.MoveAlongSpline( ref progress, targetSpeed * Time.deltaTime, maximumError: 0f );
            //else if( absSpeed >= 40f )
            //	targetPos = spline.MoveAlongSpline( ref progress, targetSpeed * Time.deltaTime, increasedAccuracy: true );
            //else
            //	targetPos = spline.MoveAlongSpline( ref progress, targetSpeed * Time.deltaTime );

            targetPos = spline.MoveAlongSpline(ref progress, targetSpeed * Time.deltaTime);

            objectToMove.transform.position = targetPos;
            //cachedTransform.position = Vector3.Lerp( cachedTransform.position, targetPos, movementLerpModifier * Time.deltaTime );

            bool movingForward = (speed > 0f) == isGoingForward;

            if (lookForward)
            {
                Quaternion targetRotation;
                if (movingForward)
                    targetRotation = Quaternion.LookRotation(spline.GetTangent(progress));
                else
                    targetRotation = Quaternion.LookRotation(-spline.GetTangent(progress));

                objectToMove.transform.rotation = Quaternion.Lerp(objectToMove.transform.rotation, targetRotation, rotationLerpModifier * Time.deltaTime);
            }

            if (movingForward)
            {
                if (progress >= 1f - relaxationAtEndPoints)
                {
                    if (!onPathCompletedCalledAt1)
                    {
                        onPathCompleted.Invoke();
                        onPathCompletedCalledAt1 = true;
                    }

                    if (travelMode == TRAVEL_MODE_ONCE)
                        progress = 1f;
                    else if (travelMode == TRAVEL_MODE_LOOP)
                        progress -= 1f;
                    else
                    {
                        progress = 2f - progress;
                        isGoingForward = !isGoingForward;
                    }
                }
                else
                {
                    onPathCompletedCalledAt1 = false;
                }
            }
            else
            {
                if (progress <= relaxationAtEndPoints)
                {
                    if (!onPathCompletedCalledAt0)
                    {
                        onPathCompleted.Invoke();
                        onPathCompletedCalledAt0 = true;
                    }

                    if (travelMode == TRAVEL_MODE_ONCE)
                        progress = 0f;
                    else if (travelMode == TRAVEL_MODE_LOOP)
                        progress += 1f;
                    else
                    {
                        progress = -progress;
                        isGoingForward = !isGoingForward;
                    }
                }
                else
                {
                    onPathCompletedCalledAt0 = false;
                }
            }
        }

        public void UseOculusPlayerController()
        {
            if (!initialisedPlayerController)
            {
                InitialisePlayerController();
                initialisedPlayerController = true;
            }
            if (useProfileData)
            {
                if (InitialPose_ == null)
                {
                    InitialPose_ = new OVRPose()
                    {
                        position = CameraRig.transform.localPosition,
                        orientation = CameraRig.transform.localRotation
                    };
                }

                var p = CameraRig.transform.localPosition;
                p.y = OVRManager.profile.eyeHeight - 0.5f * Controller.height;
                p.z = OVRManager.profile.eyeDepth;
                CameraRig.transform.localPosition = p;
            }
            else if (InitialPose_ != null)
            {
                CameraRig.transform.localPosition = InitialPose_.Value.position;
                CameraRig.transform.localRotation = InitialPose_.Value.orientation;
                InitialPose_ = null;
            }

            UpdateMovement();

            Vector3 moveDirection = Vector3.zero;

            float motorDamp = (1.0f + (Damping * SimulationRate_ * Time.deltaTime));

            MoveThrottle_.x /= motorDamp;
            if (EnableFreeMovement)
            {
                MoveThrottle_.y /= motorDamp;

            }
            else
            {
                MoveThrottle_.y = (MoveThrottle_.y > 0.0f) ? (MoveThrottle_.y / motorDamp) : MoveThrottle_.y;

            }
            MoveThrottle_.z /= motorDamp;

            moveDirection += MoveThrottle_ * SimulationRate_ * Time.deltaTime;

            // Gravity
            if (Controller.isGrounded && FallSpeed_ <= 0)
                FallSpeed_ = ((Physics.gravity.y * (GravityModifier * 0.002f)));
            else
                FallSpeed_ += ((Physics.gravity.y * (GravityModifier * 0.002f)) * SimulationRate_ * Time.deltaTime);

            moveDirection.y += FallSpeed_ * SimulationRate_ * Time.deltaTime;

            // Offset correction for uneven ground
            float bumpUpOffset = 0.0f;

            if (Controller.isGrounded && MoveThrottle_.y <= 0.001f)
            {
                bumpUpOffset = Mathf.Max(Controller.stepOffset, new Vector3(moveDirection.x, 0, moveDirection.z).magnitude);
                moveDirection -= bumpUpOffset * Vector3.up;
            }

            Vector3 predictedXZ = Vector3.Scale((Controller.transform.localPosition + moveDirection), new Vector3(1, 0, 1));

            // Move contoller
            Controller.Move(moveDirection);

            Vector3 actualXZ = Vector3.Scale(Controller.transform.localPosition, new Vector3(1, 0, 1));

            if (predictedXZ != actualXZ)
                MoveThrottle_ += (actualXZ - predictedXZ) / (SimulationRate_ * Time.deltaTime);
        }
        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }
        private float AngleDifference(float a, float b)
        {
            float diff = (360 + a - b) % 360;
            if (diff > 180)
                diff -= 360;
            return diff;
        }
        private void GetHaltUpdateMovement(ref bool haltUpdateMovement)
        {
            haltUpdateMovement = HaltUpdateMovement;
        }

        /// <summary>
        /// Gets the move scale multiplier.
        /// </summary>
        /// <param name="moveScaleMultiplier">Move scale multiplier.</param>
        private void GetMoveScaleMultiplier(ref float moveScaleMultiplier)
        {
            moveScaleMultiplier = MoveScaleMultiplier;
        }

        /// <summary>
        /// Gets the rotation scale multiplier.
        /// </summary>
        /// <param name="rotationScaleMultiplier">Rotation scale multiplier.</param>
        private void GetRotationScaleMultiplier(ref float rotationScaleMultiplier)
        {
            rotationScaleMultiplier = RotationScaleMultiplier;
        }

        /// <summary>
        /// Gets the allow mouse rotation.
        /// </summary>
        /// <param name="skipMouseRotation">Allow mouse rotation.</param>
        private void GetSkipMouseRotation(ref bool skipMouseRotation)
        {
            skipMouseRotation = SkipMouseRotation;
        }


        private void UpdateMovement()
        {
            bool HaltUpdateMovement = false;
            GetHaltUpdateMovement(ref HaltUpdateMovement);
            if (HaltUpdateMovement)
                return;

            float MoveScaleMultiplier = 1;
            GetMoveScaleMultiplier(ref MoveScaleMultiplier);

            float RotationScaleMultiplier = 1;
            GetRotationScaleMultiplier(ref RotationScaleMultiplier);

            bool SkipMouseRotation = false;
            GetSkipMouseRotation(ref SkipMouseRotation);

            float MoveScale = 1.0f;
            // No positional movement if we are in the air
            if (!EnableFreeMovement && !Controller.isGrounded)
                MoveScale = 0.0f;

            MoveScale *= SimulationRate_ * Time.deltaTime;



            Quaternion playerDirection = ((HmdRotatesY) ? CameraRig.centerEyeAnchor.rotation : _playerControllerGameObject.transform.rotation);
            
            if (EnableFreeMovement)
            {
                playerDirection = Quaternion.Euler(playerDirection.eulerAngles.x, playerDirection.eulerAngles.y, playerDirection.eulerAngles.z);

            }
            else
            {
                //remove any pitch + yaw components
                playerDirection = Quaternion.Euler(0, playerDirection.eulerAngles.y, 0);

            }

            Vector3 euler = _playerControllerGameObject.transform.rotation.eulerAngles;

            Vector2 touchDir = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad);

            bool stepLeft = false;
            bool stepRight = false;
            stepLeft = OVRInput.GetDown(OVRInput.Button.PrimaryShoulder) || Input.GetKeyDown(KeyCode.Q);
            stepRight = OVRInput.GetDown(OVRInput.Button.SecondaryShoulder) || Input.GetKeyDown(KeyCode.E);

            OVRInput.Controller activeController = OVRInput.GetActiveController();
            if ((activeController == OVRInput.Controller.Touchpad)
                || (activeController == OVRInput.Controller.Remote))
            {
                stepLeft |= OVRInput.GetDown(OVRInput.Button.DpadLeft);
                stepRight |= OVRInput.GetDown(OVRInput.Button.DpadRight);
            }
            else if ((activeController == OVRInput.Controller.LTrackedRemote)
                || (activeController == OVRInput.Controller.RTrackedRemote))
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryTouchpad))
                {
                    if ((touchDir.magnitude > 0.3f)
                        && (Mathf.Abs(touchDir.x) > Mathf.Abs(touchDir.y)))
                    {
                        stepLeft |= (touchDir.x < 0.0f);
                        stepRight |= (touchDir.x > 0.0f);
                    }
                }
            }

            float rotateInfluence = SimulationRate_ * Time.deltaTime * RotationAmount * RotationScaleMultiplier;

#if !UNITY_ANDROID
        if (!SkipMouseRotation)
        {
            PendingRotation += Input.GetAxis("Mouse X") * rotateInfluence * 3.25f;
        }
#endif
            float rightAxisX = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;
            if (Mathf.Abs(rightAxisX) < axisDeadZone)
                rightAxisX = 0;

            PendingRotation += rightAxisX * rotateInfluence;


            if (rotationSnap)
            {
                if (Mathf.Abs(PendingRotation) > RotationRatchet)
                {
                    if (PendingRotation > 0)
                        stepRight = true;
                    else
                        stepLeft = true;
                    PendingRotation -= Mathf.Sign(PendingRotation) * RotationRatchet;
                }
            }
            else
            {
                euler.y += PendingRotation;
                PendingRotation = 0;
            }



            if (rotationAnimation > 0 && animating)
            {
                float speed = Mathf.Max(rotationAnimation, 3);

                float diff = AngleDifference(targetYaw, euler.y);
                // float done = AngleDifference(euler.y, animationStartAngle);

                euler.y += Mathf.Sign(diff) * speed * Time.deltaTime;

                if ((AngleDifference(targetYaw, euler.y) < 0) != (diff < 0))
                {
                    animating = false;
                    euler.y = targetYaw;
                }
            }
            if (stepLeft ^ stepRight)
            {
                float change = stepRight ? RotationRatchet : -RotationRatchet;

                if (rotationAnimation > 0)
                {
                    targetYaw = (euler.y + change) % 360;
                    animating = true;
                    // animationStartAngle = euler.y;
                }
                else
                {
                    euler.y += change;
                }
            }

            float moveInfluence = SimulationRate_ * Time.deltaTime * Acceleration * 0.1f * MoveScale * MoveScaleMultiplier;

            // Run!
            if (OVRInput.Get(runButton) || OVRInput.Get(alternateRunButton) || Input.GetKey(KeyCode.LeftShift))
                moveInfluence *= 2.0f;


            float leftAxisX = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x;
            float leftAxisY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;

            if (activeController == OVRInput.Controller.Touchpad)
            {
                leftAxisY = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad).y;
            }
            else if ((activeController == OVRInput.Controller.LTrackedRemote)
                || (activeController == OVRInput.Controller.RTrackedRemote))
            {
                if (OVRInput.Get(OVRInput.Button.PrimaryTouchpad))
                {
                    if ((touchDir.magnitude > 0.3f)
                        && (Mathf.Abs(touchDir.y) > Mathf.Abs(touchDir.x)))
                    {
                        leftAxisY = (touchDir.y > 0.0f) ? 1 : -1;
                    }
                }
            }

            if (Mathf.Abs(leftAxisX) < axisDeadZone)
                leftAxisX = 0;
            if (Mathf.Abs(leftAxisY) < axisDeadZone)
                leftAxisY = 0;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                leftAxisY = 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                leftAxisX = -1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                leftAxisX = 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                leftAxisY = -1;

            if (activeController == OVRInput.Controller.Remote)
            {
                if (OVRInput.Get(OVRInput.Button.DpadUp))
                    leftAxisY = 1;
                else if (OVRInput.Get(OVRInput.Button.DpadDown))
                    leftAxisY = -1;
            }

            if (leftAxisY > 0.0f)
                MoveThrottle_ += leftAxisY
                * (playerDirection * (Vector3.forward * moveInfluence));

            if (leftAxisY < 0.0f)
                MoveThrottle_ += Mathf.Abs(leftAxisY)
                * (playerDirection * (Vector3.back * moveInfluence));

            if (leftAxisX < 0.0f)
                MoveThrottle_ += Mathf.Abs(leftAxisX)
                * (playerDirection * (Vector3.left * moveInfluence));

            if (leftAxisX > 0.0f)
                MoveThrottle_ += leftAxisX
                * (playerDirection * (Vector3.right * moveInfluence));

            _playerControllerGameObject.transform.rotation = Quaternion.Euler(euler);
        }


        private void SetRotationSnap(bool value)
        {
            rotationSnap = value;
            PendingRotation = 0;
        }

        private void SetRotationAnimation(float value)
        {
            rotationAnimation = value;
            PendingRotation = 0;
        }

        /// <summary>
        /// Resets the player look rotation when the device orientation is reset.
        /// </summary>
        private void ResetOrientation()
        {
            if (HmdResetsY)
            {
                Vector3 euler = _playerControllerGameObject.transform.rotation.eulerAngles;
                euler.y = InitialYRotation_;
                _playerControllerGameObject.transform.rotation = Quaternion.Euler(euler);
            }
        }

        private void Reset()
        {
            // Prefer to not reset Y when HMD position reset
            HmdResetsY = false;
            Acceleration = 0.1f;
            useProfileData = false;
        }
    }


}

