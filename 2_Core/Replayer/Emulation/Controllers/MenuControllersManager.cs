﻿using BeatLeader.Utils;
using IPA.Utilities;
using UnityEngine;
using VRUIControls;
using Zenject;

namespace BeatLeader.Replayer {
    internal class MenuControllersManager : MonoBehaviour {
        [Inject] private readonly PauseMenuManager _pauseMenuManager;
        [Inject] private readonly DiContainer _diContainer;
        [Inject] private readonly VRInputModule _vrInputModule;

        [FirstResource] private readonly MainSettingsModelSO _mainSettingsModel;

        public Transform HandsContainer { get; private set; }
        public VRController LeftHand { get; private set; }
        public VRController RightHand { get; private set; }

        [FirstResource("VRGameCore", requireActiveInHierarchy: true)]
        private readonly Transform Origin;

        public void ShowHands(bool show = true) {
            LeftHand.gameObject.SetActive(show);
            RightHand.gameObject.SetActive(show);
            HandsContainer.gameObject.SetActive(show);
        }

        private void Awake() {
            this.LoadResources();

            var menuHandsTransform = _pauseMenuManager.transform.Find("MenuControllers");
            LeftHand = Instantiate(menuHandsTransform.Find("ControllerLeft")).GetComponent<VRController>();
            RightHand = Instantiate(menuHandsTransform.Find("ControllerRight")).GetComponent<VRController>();

            _diContainer.InjectComponentsInChildren(LeftHand.gameObject);
            _diContainer.InjectComponentsInChildren(RightHand.gameObject);

            HandsContainer = new GameObject("ReplayerHands").transform;
            HandsContainer.SetParent(Origin, true);
            HandsContainer.transform.localPosition = _mainSettingsModel.roomCenter;
            HandsContainer.transform.localEulerAngles = new(0, _mainSettingsModel.roomRotation, 0);

            LeftHand.transform.SetParent(HandsContainer, false);
            RightHand.transform.SetParent(HandsContainer, false);
            SetInputControllers(LeftHand, RightHand);
        }

        private void SetInputControllers(VRController left, VRController right) {
            var pointer = _vrInputModule.GetField<VRPointer, VRInputModule>("_vrPointer");
            pointer.SetField("_leftVRController", left);
            pointer.SetField("_rightVRController", right);
        }
    }
}
