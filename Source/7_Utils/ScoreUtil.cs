﻿using System;
using BeatLeader.API.Methods;
using BeatLeader.Core.Managers.ReplayEnhancer;
using BeatLeader.Models.Activity;
using BeatLeader.Models.Replay;

namespace BeatLeader.Utils {
    internal static class ScoreUtil {
        #region Submission

        internal static bool Submission = true;
        internal static bool SiraSubmission = true;
        internal static bool BS_UtilsSubmission = true;

        internal static void EnableSubmission() {
            Submission = true;
            SiraSubmission = true;
            BS_UtilsSubmission = true;
        }

        private static bool ShouldSubmit() {
            return Submission && SiraSubmission && BS_UtilsSubmission;
        }

        #endregion

        #region ProcessReplay

        public static Action<Replay>? ReplayUploadStartedEvent;

        public static void ProcessReplay(Replay replay, PlayEndData data) {
            if (replay.info.speed != 0) {
                Plugin.Log.Debug("Practice, replay won't be submitted");
                goto SaveReplay;
            }

            if (!ShouldSubmit()) {
                Plugin.Log.Debug("Score submission was disabled");
                goto SaveReplay;
            }

            Plugin.Log.Debug("Uploading replay");
            switch (data.EndType) {
                case PlayEndData.LevelEndType.Clear:
                    UploadReplay(replay);
                    break;
                case PlayEndData.LevelEndType.Unknown:
                    Plugin.Log.Debug("Unknown level end Type");
                    break;
                default:
                    UploadPlay(replay, data);
                    break;
            }

            SaveReplay: ;
            var replayManager = ReplayManager.Instance;
            var isOstLevel = !MapEnhancer.previewBeatmapLevel
                .levelID.StartsWith(CustomLevelLoader.kCustomLevelPrefixId);
            if (replayManager.ValidatePlay(replay, data, isOstLevel)) {
                Plugin.Log.Debug("Validation completed, replay will be saved");
                _ = replayManager.SaveReplayAsync(replay, data, default);
            } else {
                Plugin.Log.Warn("Validation failed, replay will not be saved!");
                replayManager.ResetLastReplay();
            }
        }

        public static void UploadReplay(Replay replay) {
            ReplayUploadStartedEvent?.Invoke(replay);
            UploadReplayRequest.SendRequest(replay);
        }

        public static void UploadPlay(Replay replay, PlayEndData data) {
            UploadPlayRequest.SendRequest(replay, data);
        }

        #endregion
    }
}