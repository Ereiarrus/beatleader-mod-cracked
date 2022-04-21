﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPA.Utilities;
using BeatLeader.Models;
using BeatLeader.Utils;
using UnityEngine;
using Zenject;

namespace BeatLeader.Replays.Scoring
{
    public class NoteEventCutScoringElement : ScoringElement, ICutScoreBufferDidFinishReceiver
    {
        public class Pool : Pool<NoteEventCutScoringElement> { }

        public override ScoreMultiplierCounter.MultiplierEventType wouldBeCorrectCutBestPossibleMultiplierEventType => _wouldBeCorrectCutBestPossibleMultiplierEventType;
        public override ScoreMultiplierCounter.MultiplierEventType multiplierEventType => _multiplierEventType;

        protected ScoreMultiplierCounter.MultiplierEventType _multiplierEventType;
        protected ScoreMultiplierCounter.MultiplierEventType _wouldBeCorrectCutBestPossibleMultiplierEventType;

        protected NoteEventCutScoreBuffer _cutScoreBuffer;
        protected override int executionOrder => 100000;
        public IReadonlyCutScoreBuffer cutScoreBuffer => _cutScoreBuffer;
        public override int cutScore => _cutScoreBuffer.cutScore;

        public virtual void Init(NoteCutInfo noteCutInfo, Replay replay)
        {
            noteData = noteCutInfo.noteData;
            switch (noteData.scoringType)
            {
                case NoteData.ScoringType.Ignore:
                    _multiplierEventType = ScoreMultiplierCounter.MultiplierEventType.Neutral;
                    _wouldBeCorrectCutBestPossibleMultiplierEventType = ScoreMultiplierCounter.MultiplierEventType.Neutral;
                    break;
                case NoteData.ScoringType.Normal:
                case NoteData.ScoringType.SliderHead:
                case NoteData.ScoringType.SliderTail:
                case NoteData.ScoringType.BurstSliderHead:
                case NoteData.ScoringType.BurstSliderElement:
                    _multiplierEventType = ScoreMultiplierCounter.MultiplierEventType.Positive;
                    _wouldBeCorrectCutBestPossibleMultiplierEventType = ScoreMultiplierCounter.MultiplierEventType.Positive;
                    break;
                case NoteData.ScoringType.NoScore:
                    _multiplierEventType = ScoreMultiplierCounter.MultiplierEventType.Neutral;
                    _wouldBeCorrectCutBestPossibleMultiplierEventType = ScoreMultiplierCounter.MultiplierEventType.Neutral;
                    break;
            }

            _cutScoreBuffer = new NoteEventCutScoreBuffer();
            if (_cutScoreBuffer.Init(in noteCutInfo, replay))
            {
                _cutScoreBuffer.RegisterDidFinishReceiver(this);
                isFinished = false;
            }
            else
            {
                isFinished = true;
            }
        }
        public virtual void HandleCutScoreBufferDidFinish(CutScoreBuffer cutScoreBuffer)
        {
            isFinished = true;
            _cutScoreBuffer.UnregisterDidFinishReceiver(this);
        }
    }
}
