﻿using System;
using System.Linq;
using System.Collections.Generic;
using BeatLeader.Replayer.Movement;
using ICameraPoseProvider = BeatLeader.Models.ICameraPoseProvider;
using CombinedCameraMovementData = BeatLeader.Models.CombinedCameraMovementData;
using UnityEngine;
using Zenject;
using BeatLeader.Interop;

namespace BeatLeader.Replayer.Camera
{
    public class ReplayerCameraController : MonoBehaviour
    {
        public class InitData
        {
            public readonly ICameraPoseProvider[] poseProviders;
            public readonly string cameraStartPose;

            public InitData(string cameraStartPose = null)
            {
                this.cameraStartPose = cameraStartPose;
                poseProviders = new ICameraPoseProvider[0];
            }
            public InitData(string cameraStartPose = null, params ICameraPoseProvider[] poseProviders)
            {
                this.cameraStartPose = cameraStartPose;
                this.poseProviders = poseProviders;
            }
            public InitData(params ICameraPoseProvider[] poseProviders)
            {
                this.poseProviders = poseProviders;
            }
        }

        [Inject] protected readonly InputManager _inputManager;
        [Inject] protected readonly VRControllersManager _vrControllersManager;
        [Inject] protected readonly InitData _data;
        [Inject] protected readonly Models.ReplayLaunchData _replayData;

        public List<ICameraPoseProvider> PoseProviders { get; protected set; }
        public CombinedCameraMovementData CombinedMovementData
        {
            get => new CombinedCameraMovementData(transform, _vrControllersManager.HeadContainer.transform, _vrControllersManager.OriginTransform);
            protected set
            {
                transform.localPosition = value.cameraPose.position;
                transform.localRotation = value.cameraPose.rotation;

                _vrControllersManager.HeadContainer.transform.localPosition = value.headPose.position;
                _vrControllersManager.HeadContainer.transform.localRotation = value.headPose.rotation;

                _vrControllersManager.OriginTransform.position = value.originPose.position;
                _vrControllersManager.OriginTransform.rotation = value.originPose.rotation;

                if (InputManager.IsInFPFC) return;

                _vrControllersManager.MenuHandsContainerTransform.localPosition = value.cameraPose.position;
                _vrControllersManager.MenuHandsContainerTransform.localRotation = value.cameraPose.rotation;
            }
        }
        public ICameraPoseProvider CurrentPose => _currentPose;
        public string CurrentPoseName => _currentPose != null ? _currentPose.Name : "NaN";
        public bool IsInitialized { get; private set; }
        public int CullingMask
        {
            get => _camera.cullingMask;
            set => _camera.cullingMask = value;
        }
        public int FieldOfView
        {
            get => (int)_camera.fieldOfView;
            set
            {
                if (_fieldOfView == value) return;
                _fieldOfView = value;
                RefreshCamera();
                OnCameraFOVChanged?.Invoke(value);
            }
        }

        public event Action<ICameraPoseProvider> OnCameraPoseChanged;
        public event Action<int> OnCameraFOVChanged;

        protected ICameraPoseProvider _currentPose;
        protected UnityEngine.Camera _camera;

        private int _fieldOfView;
        private bool _wasRequestedLastTime;
        private string _requestedPose;

        private void Awake()
        {
            if (_data == null || IsInitialized) return;
            SmoothCamera smoothCamera = Resources.FindObjectsOfTypeAll<SmoothCamera>()
                .First(x => x.transform.parent.name == "LocalPlayerGameCore");
            smoothCamera.gameObject.SetActive(false);
            _camera = Instantiate(smoothCamera.GetComponent<UnityEngine.Camera>(), gameObject.transform, true);

            _camera.gameObject.SetActive(false);
            _camera.name = "ReplayerViewCamera";
            DestroyImmediate(_camera.GetComponent<SmoothCameraController>());
            DestroyImmediate(_camera.GetComponent<SmoothCamera>());
            _camera.gameObject.SetActive(true);
            _camera.nearClipPlane = 0.01f;
            //_diContainer.Bind<Camera>().FromInstance(_camera).WithConcreteId("ReplayerCamera").NonLazy();
            transform.SetParent(_vrControllersManager.OriginTransform, false);

            PoseProviders = _data.poseProviders.Where(x => InputManager.MatchesCurrentInput(x.AvailableInputs)).ToList();
            RequestCameraPose(_data.cameraStartPose);

            bool useReplayerCamera = !Cam2Interop.Detected || !InputManager.IsInFPFC ||
                 (_replayData.overrideSettings && _replayData.settings.forceUseReplayerCamera);
            SetEnabled(useReplayerCamera);
            IsInitialized = true;
        }
        private void LateUpdate()
        {
            if (IsInitialized && _wasRequestedLastTime)
            {
                SetCameraPose(_requestedPose);
                _wasRequestedLastTime = false;
            }
            if (_currentPose != null && _currentPose.UpdateEveryFrame)
            {
                CombinedMovementData = _currentPose.GetPose(CombinedMovementData);
            }
        }
        public void SetCameraPose(string name)
        {
            if (_camera == null || string.IsNullOrEmpty(name) || name == CurrentPoseName) return;

            ICameraPoseProvider cameraPose = PoseProviders.FirstOrDefault(x => x.Name == name);
            if (cameraPose == null) return;

            _currentPose = cameraPose;
            CombinedMovementData = _currentPose.GetPose(CombinedMovementData);
            RefreshCamera();
            OnCameraPoseChanged?.Invoke(cameraPose);
        }
        public void SetCameraPose(ICameraPoseProvider provider)
        {
            if (!PoseProviders.Contains(provider))
                PoseProviders.Add(provider);
            SetCameraPose(provider.Name);
        }
        public void SetEnabled(bool enabled)
        {
            if (_camera != null)
            {
                _camera.gameObject.SetActive(enabled);
                _camera.enabled = enabled;
            }
            gameObject.SetActive(enabled);
        }

        protected void RefreshCamera()
        {
            _camera.stereoTargetEye = InputManager.IsInFPFC ? StereoTargetEyeMask.None : StereoTargetEyeMask.Both;
            if (InputManager.IsInFPFC) _camera.fieldOfView = _fieldOfView;
        }
        protected void RequestCameraPose(string name)
        {
            if (name == string.Empty) return;
            _requestedPose = name;
            _wasRequestedLastTime = true;
        }
    }
}
