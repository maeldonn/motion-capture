﻿using CERV.MouvementRecognition.Recognition;
using System.Collections.Generic;
using UniHumanoid;
using UnityEngine;

namespace CERV.MouvementRecognition.Main
{
    public enum Mode
    {
        Empty,
        Training,
        Recognition
    }

    public class Store : MonoBehaviour
    {
        private bool m_usingArm = false;
        private string m_path = null;
        private Bvh m_bvh = null;
        private Mode m_mode = Mode.Empty;
        private int m_margin = 20;
        private List<ScoreItem> m_scores = new List<ScoreItem>() { new ScoreItem("closing_door", 0), new ScoreItem("left_step", 0), new ScoreItem("opening_door", 0), new ScoreItem("right_step", 0), new ScoreItem("saluting", 0), new ScoreItem("sitting_down", 0), new ScoreItem("standing_up", 0) };

        public bool UsingArm
        {
            get { return m_usingArm; }
            private set
            {
                if (m_usingArm == value) return;
                m_usingArm = value;
            }
        }

        public string Path
        {
            get { return m_path; }
            set
            {
                if (m_path == value) return;
                m_path = value;
                Bvh = new Bvh().GetBvhFromPath(m_path);
                Mode = Mode.Training;
            }
        }

        public Bvh Bvh
        {
            get { return m_bvh; }
            private set
            {
                if (m_bvh == value) return;
                m_bvh = value;
            }
        }

        public Mode Mode
        {
            get { return m_mode; }
            set
            {
                if (m_mode == value) return;
                m_mode = value;
            }
        }

        public int Margin
        {
            get { return m_margin; }
            set
            {
                if (m_margin == value) return;
                m_margin = value;
            }
        }

        public List<ScoreItem> Scores
        {
            get { return m_scores; }
            set
            {
                if (m_scores == value) return;
                m_scores = value;
            }
        }


        public void toggleUsingArm()
        {
            UsingArm = !UsingArm;
        }

        public void changeModeToRecognition()
        {
            Mode = Mode.Recognition;
        }

        public void RemoveScores()
        {
            Scores.ForEach((score) => score.Score = 0);
        }

        public bool EmptyScore()
        {
            for (int i = 0; i < Scores.Count; i++)
            {
                if (Scores[i].Score != 0) return false; 
            }
            return true;
        }
    }
}