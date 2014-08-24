/*
 * This software was developed by Austin Hughes
 * Last Modified: 2014-08-24
 */

// System imports
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

// Microsoft imports
using Microsoft.Kinect;

namespace NAO_Kinect
{
    /// <summary>
    /// This class handles processing the Kinect Body frames
    /// and drawing the body skeleton on an image for display
    /// </summary>
    class KinectBody
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        private int displayWidth;
        private int displayHeight;

        private BodyFrameReader bodyFrameReader = null;
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Arrays
        /// </summary>
        private Body[] bodies = null;
        
        /// <summary>
        /// Drawing variables
        /// </summary>
        private const double HandSize = 30;
        private const double JointThickness = 3;
        private const double ClipBoundsThickness = 10;
        private const float InferredZPositionClamp = 0.1f;
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));  
        private readonly Brush inferredJointBrush = Brushes.Yellow;   
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;

        /// <summary>
        /// Lists
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;
        private List<Pen> bodyColors;

        /// <summary>
        /// Event handler for new Kinect frames
        /// </summary>
        public event EventHandler NewFrame;

        /// <summary>
        /// Class constructor
        /// </summary>
        public KinectBody(KinectSensor kinect)
        {
            // set kinect sensor
            sensor = kinect;

            // get the coordinate mapper
            coordinateMapper = sensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = sensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            displayWidth = frameDescription.Width;
            displayHeight = frameDescription.Height;

            // a bone defined as a line between two joints
            bones = new List<Tuple<JointType, JointType>>
                {
                    // Torso
                    new Tuple<JointType, JointType>(JointType.Head, JointType.Neck),
                    new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder),
                    new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid),
                    new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase),
                    new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight),
                    new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft),
                    new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight),
                    new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft),

                    // Right Arm
                    new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight),
                    new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight),
                    new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight),
                    new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight),
                    new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight),

                    // Left Arm
                    new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft),
                    new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft),
                    new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft),
                    new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft),
                    new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft),

                    // Right Leg
                    new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight),
                    new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight),
                    new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight),

                    // Left Leg
                    new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft),
                    new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft),
                    new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft)
                };

            // populate body colors, one for each BodyIndex
            bodyColors = new List<Pen>();

            bodyColors.Add(new Pen(Brushes.Red, 6));
            bodyColors.Add(new Pen(Brushes.Orange, 6));
            bodyColors.Add(new Pen(Brushes.Green, 6));
            bodyColors.Add(new Pen(Brushes.Blue, 6));
            bodyColors.Add(new Pen(Brushes.Indigo, 6));
            bodyColors.Add(new Pen(Brushes.Violet, 6));

            // Create the drawing group we'll use for drawing
            drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            imageSource = new DrawingImage(drawingGroup);


            // start the skeleton stream
            startBodyStream();
        }

        /// <summary>
        /// Starts the Skeleton Stream
        /// If audio was started, restart it after starting skeleton stream
        /// </summary>
        public void startBodyStream()
        {
            try
            {
                // open the reader for the body frames
                bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            }
            catch (Exception)
            {
                MessageBox.Show("Error starting body stream.");
            }
        }

        /// <summary>
        /// Disables the skeleton stream
        /// </summary>
        public void stopBodyStream()
        {
            if (null != sensor)
            {
                bodyFrameReader.Dispose();
            }
        }

        /// <summary>
        /// Open Skeleton Frame for use
        /// Creates points for all relevant joints and old joint positions
        /// 
        /// Modified based on code provided by Microsoft
        /// 
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            // Open the Skeleton frame
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) 
            {
                // check that a frame is available
                if (skeletonFrame != null && skeletonData != null) 
                {
                    // get the skeletal information in this frame
                    skeletonFrame.CopySkeletonDataTo(skeletonData); 
                }
            }

            // Start with a far enough distance
            closestDistance = 10000f; 

            // draws the skeleton on the screen
            using (var dc = drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletonData.Length != 0)
                {
                    foreach (var skeleton in skeletonData.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked))
                    {
                        if (skeleton.Position.Z < closestDistance)
                        {
                            closestID = skeleton.TrackingId;
                            closestDistance = skeleton.Position.Z;
                        }
                    }

                    if (closestID > 0)
                    {
                        sensor.SkeletonStream.ChooseSkeletons(closestID); // Track this skeleton
                    }

                    foreach (var skeleton in skeletonData)
                    {
                        RenderClippedEdges(skeleton, dc);

                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            DrawBonesAndJoints(skeleton, dc);
                            trackedSkeleton = skeleton;
                        }
                        else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            centerPointBrush,
                            null,
                            SkeletonPointToScreen(skeleton.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }
                else
                {
                    trackedSkeleton = null;
                }
            }

            // prevent drawing outside of our render area
            drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));

            // calls our event
            OnNewFrame();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// 
        /// This code provided by Microsoft
        /// 
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            try
            {
                if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
                {
                    drawingContext.DrawRectangle(
                        Brushes.Red,
                        null,
                        new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
                }

                if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
                {
                    drawingContext.DrawRectangle(
                        Brushes.Red,
                        null,
                        new Rect(0, 0, RenderWidth, ClipBoundsThickness));
                }

                if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
                {
                    drawingContext.DrawRectangle(
                        Brushes.Red,
                        null,
                        new Rect(0, 0, ClipBoundsThickness, RenderHeight));
                }

                if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
                {
                    drawingContext.DrawRectangle(
                        Brushes.Red,
                        null,
                        new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// 
        /// This code provided by Microsoft
        ///
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// 
        /// This code provided by Microsoft
        ///
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// 
        /// This code provided by Microsoft
        ///
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            var drawPen = inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, SkeletonPointToScreen(joint0.Position), SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// triggers image updated event
        /// </summary>
        private void OnNewFrame()
        {
            var handler = NewFrame;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// used to get the current frame
        /// </summary>
        /// <returns> current frame </returns>
        public DrawingImage getImage()
        {
            return imageSource;
        }

        /// <summary>
        /// returns the currently tracked skeleton
        /// </summary>
        /// <returns> tracked skeleton </returns>
        public Body getBody()
        {
            return trackedBody;
        }
    }
}
