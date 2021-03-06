﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniHumanoid
{
    public class BvhException : Exception
    {
        public BvhException(string msg) : base(msg)
        {
        }
    }

    public enum Channel
    {
        Xposition,
        Yposition,
        Zposition,
        Xrotation,
        Yrotation,
        Zrotation,
    }

    public static class ChannelExtensions
    {
        public static string ToProperty(this Channel ch)
        {
            switch (ch)
            {
                case Channel.Xposition: return "localPosition.x";
                case Channel.Yposition: return "localPosition.y";
                case Channel.Zposition: return "localPosition.z";
                case Channel.Xrotation: return "localEulerAnglesBaked.x";
                case Channel.Yrotation: return "localEulerAnglesBaked.y";
                case Channel.Zrotation: return "localEulerAnglesBaked.z";
                default: break;
            }
            throw new BvhException("no property for " + ch);
        }

        public static bool IsLocation(this Channel ch)
        {
            switch (ch)
            {
                case Channel.Xposition:
                case Channel.Yposition:
                case Channel.Zposition: return true;
                case Channel.Xrotation:
                case Channel.Yrotation:
                case Channel.Zrotation: return false;
            }
            throw new BvhException("no property for " + ch);
        }
    }

    public struct Single3
    {
        public float x;
        public float y;
        public float z;

        public Single3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }
    }

    public class BvhNode
    {
        public string Name
        {
            get;
            private set;
        }

        public Single3 Offset
        {
            get;
            set;
        }

        public Channel[] Channels
        {
            get;
            set;
        }

        public List<BvhNode> Children
        {
            get;
            private set;
        }

        public BvhNode(string name)
        {
            Name = name;
            Children = new List<BvhNode>();
        }

        public virtual void Parse(StringReader r)
        {
            Offset = ParseOffset(r.ReadLine());
            Channels = ParseChannel(r.ReadLine());
        }

        private static Single3 ParseOffset(string line)
        {
            var splited = line.Trim().Split();
            if (splited[0] != "OFFSET") throw new BvhException("OFFSET is not found");
            var offset = splited.Skip(1).Where(x => !string.IsNullOrEmpty(x)).Select(x => float.Parse(x, CultureInfo.InvariantCulture.NumberFormat)).ToArray();
            return new Single3(offset[0], offset[1], offset[2]);
        }

        private static Channel[] ParseChannel(string line)
        {
            var splited = line.Trim().Split();
            if (splited[0] != "CHANNELS") throw new BvhException("CHANNELS is not found");
            var count = int.Parse(splited[1]);
            if (count + 2 != splited.Length) throw new BvhException("channel count is not match with splited count");
            return splited.Skip(2).Select(x => (Channel)Enum.Parse(typeof(Channel), x)).ToArray();
        }

        public IEnumerable<BvhNode> Traverse()
        {
            yield return this;

            foreach (var child in Children)
            {
                foreach (var descentant in child.Traverse())
                {
                    yield return descentant;
                }
            }
        }
    }

    public class EndSite : BvhNode
    {
        public EndSite() : base("")
        {
            // Do nothing
        }

        public override void Parse(StringReader r)
        {
            r.ReadLine();
        }
    }

    public class ChannelCurve
    {
        public float[] Keys
        {
            get;
            private set;
        }

        public ChannelCurve(int frameCount)
        {
            Keys = new float[frameCount];
        }

        public void SetKey(int frame, float value)
        {
            Keys[frame] = value;
        }
    }

    public class Bvh
    {
        public Bvh()
        {
            // Do nothing
        }

        public Bvh GetBvhFromPath(string path) => Parse(File.ReadAllText(path, Encoding.UTF8));

        public BvhNode Root
        {
            get;
            private set;
        }

        public TimeSpan FrameTime
        {
            get;
            private set;
        }

        public ChannelCurve[] Channels
        {
            get;
            private set;
        }

        public int FrameCount { get; }

        public struct PathWithProperty
        {
            public string Path;
            public string Property;
            public bool IsLocation;
        }

        public int getIndexFromNode(string wantedNode)
        {
            var index = 0;
            foreach(var node in Root.Traverse())
            {
                if (node.Name == wantedNode) break;
                index++;
            }
            return index;
        }

        public Vector3 GetReceivedPosition(string boneName, int frame, bool rotation)
        {
            Vector3 temp = new Vector3(0f, 0f, 0f);
            float NeuronUnityLinearScale = 0.01f;
            var index = 0;
            bool boneFound = false;

            foreach (var node in Root.Traverse())
            {
                for (int i = 0; i < node.Channels.Length; ++i, ++index)
                {
                    if (node.Name == boneName)
                    {
                        boneFound = true;

                        if (rotation)
                        {
                            switch (node.Channels[i])
                            {
                                case Channel.Xrotation:
                                    temp.x = Channels[index].Keys[frame];
                                    break;

                                case Channel.Yrotation:
                                    temp.y = -Channels[index].Keys[frame];
                                    break;

                                case Channel.Zrotation:
                                    temp.z = -Channels[index].Keys[frame];
                                    break;

                                default:
                                    break;
                            }
                        }
                        else
                        {
                            switch (node.Channels[i])
                            {
                                case Channel.Xposition:
                                    temp.x = -NeuronUnityLinearScale * Channels[index].Keys[frame];
                                    break;

                                case Channel.Yposition:
                                    temp.y = NeuronUnityLinearScale * Channels[index].Keys[frame];
                                    break;

                                case Channel.Zposition:
                                    temp.z = NeuronUnityLinearScale * Channels[index].Keys[frame];
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }

                if (boneFound) break;
            }

            return temp;
        }

        public Vector3[] GetPositionOffset()
        {
            var bonePositionOffsets = new List<Vector3>();
            return bonePositionOffsets.ToArray();
        }

        public bool TryGetPathWithPropertyFromChannel(ChannelCurve channel, out PathWithProperty pathWithProp)
        {
            var index = Channels.ToList().IndexOf(channel);

            if (index == -1)
            {
                pathWithProp = default;
                return false;
            }

            foreach (var node in Root.Traverse())
            {
                for (int i = 0; i < node.Channels.Length; ++i, --index)
                {
                    if (index == 0)
                    {
                        pathWithProp = new PathWithProperty
                        {
                            Path = GetPath(node),
                            Property = node.Channels[i].ToProperty(),
                            IsLocation = node.Channels[i].IsLocation(),
                        };
                        return true;
                    }
                }
            }

            throw new BvhException("channel is not found");
        }

        public string GetPath(BvhNode node)
        {
            var list = new List<string>() { node.Name };
            var current = node;

            while (current != null)
            {
                current = GetParent(current);

                if (current != null)
                {
                    list.Insert(0, current.Name);
                }
            }

            return string.Join("/", list.ToArray());
        }

        public BvhNode GetParent(BvhNode node)
        {
            foreach (var x in Root.Traverse())
            {
                if (x.Children.Contains(node))
                {
                    return x;
                }
            }

            return null;
        }

        public ChannelCurve GetChannel(BvhNode target, Channel channel)
        {
            var index = 0;
            foreach (var node in Root.Traverse())
            {
                for (int i = 0; i < node.Channels.Length; ++i, ++index)
                {
                    if (node == target && node.Channels[i] == channel)
                    {
                        return Channels[index];
                    }
                }
            }

            throw new BvhException("channel is not found");
        }

        public override string ToString()
        {
            return string.Format("{0}nodes, {1}channels, {2}frames, {3:0.00}seconds"
                , Root.Traverse().Count()
                , Channels.Length
                , FrameCount
                , FrameCount * FrameTime.TotalSeconds);
        }

        public Bvh(BvhNode root, int frames, float seconds)
        {
            Root = root;
            FrameTime = TimeSpan.FromSeconds(seconds);
            FrameCount = frames;
            var channelCount = Root.Traverse()
                .Where(x => x.Channels != null)
                .Select(x => x.Channels.Length)
                .Sum();
            Channels = Enumerable.Range(0, channelCount)
                .Select(x => new ChannelCurve(frames))
                .ToArray()
                ;
        }

        public void ParseFrame(int frame, string line)
        {
            var splited = line.Trim().Split().Where(x => !string.IsNullOrEmpty(x)).ToArray();

            if (splited.Length != Channels.Length)
            {
                throw new BvhException("frame key count is not match channel count");
            }

            for (int i = 0; i < Channels.Length; ++i)
            {
                Channels[i].SetKey(frame, float.Parse(splited[i], CultureInfo.InvariantCulture.NumberFormat));
            }
        }

        public static Bvh Parse(string src)
        {
            using (var r = new StringReader(src))
            {
                if (r.ReadLine() != "HIERARCHY")
                {
                    throw new BvhException("not start with HIERARCHY");
                }

                var root = ParseNode(r);

                if (root == null)
                {
                    return null;
                }

                var frames = 0;
                var frameTime = 0.0f;
                if (r.ReadLine() == "MOTION")
                {
                    var tmp = r.ReadLine();
                    string[] frameSplited;
                    if (tmp.Split(':')[0] != "Frames") frameSplited = r.ReadLine().Split(':');
                    else frameSplited = tmp.Split(':');

                    if (frameSplited[0] != "Frames")
                    {
                        throw new BvhException("Frames is not found");
                    }

                    frames = int.Parse(frameSplited[1]);

                    var frameTimeSplited = r.ReadLine().Split(':');

                    if (frameTimeSplited[0] != "Frame Time")
                    {
                        throw new BvhException("Frame Time is not found");
                    }

                    frameTime = float.Parse(frameTimeSplited[1], CultureInfo.InvariantCulture.NumberFormat);
                }

                var bvh = new Bvh(root, frames, frameTime);

                for (int i = 0; i < frames; ++i)
                {
                    var line = r.ReadLine();
                    bvh.ParseFrame(i, line);
                }

                return bvh;
            }
        }

        private static BvhNode ParseNode(StringReader r, int level = 0)
        {
            var firstline = r.ReadLine().Trim();
            var splited = firstline.Split();

            if (splited.Length != 2)
            {
                if (splited.Length == 1)
                {
                    if (splited[0] == "}")
                    {
                        return null;
                    }
                }

                throw new BvhException(String.Format("splited to {0}({1})", splited.Length, firstline));
            }

            BvhNode node = null;
            if (splited[0] == "ROOT")
            {
                if (level != 0)
                {
                    throw new BvhException("nested ROOT");
                }
                node = new BvhNode(splited[1]);
            }
            else if (splited[0] == "JOINT")
            {
                if (level == 0)
                {
                    throw new BvhException("should ROOT, but JOINT");
                }
                node = new BvhNode(splited[1]);
            }
            else if (splited[0] == "End")
            {
                if (level == 0)
                {
                    throw new BvhException("End in level 0");
                }
                node = new EndSite();
            }
            else
            {
                throw new BvhException("unknown type: " + splited[0]);
            }

            if (r.ReadLine().Trim() != "{")
            {
                throw new BvhException("'{' is not found");
            }

            node.Parse(r);

            // child nodes
            while (true)
            {
                var child = ParseNode(r, level + 1);

                if (child == null)
                {
                    break;
                }

                if (!(child is EndSite))
                {
                    node.Children.Add(child);
                }
            }

            return node;
        }
    }
}