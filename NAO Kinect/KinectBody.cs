/*
 * This software was developed by Austin Hughes
 * Last Modified: 2014-08-22
 */

// System imports
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

// Microsoft imports
using Microsoft.Kinect;

namespace NAO_Kinect
{
    class KinectBody
    {
        /// <summary>
        /// Variables used for drawing skeleton on screen
        /// </summary>
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;
        private const double JointThickness = 3;
        private const double BodyCenterThickness = 10;
        private const double ClipBoundsThickness = 10;
        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor.
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Array of data for vody joints.
        /// </summary>
        private Body[] bodyData;

        /// <summary>
        /// The currently tracked body
        /// </summary>
        private Body trackedBody = null;

        /// <summary>
        /// event handler for updated body image
        /// </summary>
        public event EventHandler NewFrame;

        /// <summary>
        /// Variables for tracking closest body
        /// </summary>
        private float closestDistance = 10000f; // Start with a far enough distance
        private int closestID;

        /// <summary>
        /// Class constructor
        /// </summary>
        public KinectBody(KinectSensor kinect)
        {
            // set kinect sensor
            sensor = kinect;

            // Create the drawing group we'll use for drawing
            drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            imageSource = new DrawingImage(drawingGroup);

            // start the skeleton stream
            startSkeletonStream();
        }

        /// <summary>
        /// Starts the Skeleton Stream
        /// If audio was started, restart it after starting skeleton stream
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void startSkeletonStream()
        {
            try
            {
                // paramaters to smooth input
                var smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.5f;
                    smoothingParam.Correction = 0.1f;
                    smoothingParam.Prediction = 0.5f;
                    smoothingParam.JitterRadius = 0.1f;
                    smoothingParam.MaxDeviationRadius = 0.1f;
                };

                // Enable skeletal tracking
                sensor.SkeletonStream.Enable(smoothingParam);

                // Allocate Skeleton Stream data
                skeletonData = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];

                // Get Ready for Skeleton Ready Events
                sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);

                // Ensure AppChoosesSkeletons is set, lets us only track the skeleton we want
                if (!sensor.SkeletonStream.AppChoosesSkeletons)
                {
                    sensor.SkeletonStream.AppChoosesSkeletons = true; 
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error starting skeleton stream.");
            }
        }

        /// <summary>
        /// Disables the skeleton stream
        /// </summary>
        public void stopSkeletonStream()
        {
            if (null != sensor)
            {
                sensor.SkeletonStream.Disable();
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
