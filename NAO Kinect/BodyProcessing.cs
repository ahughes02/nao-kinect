/*
 * This file was created by Austin Hughes and Stetson Gafford
 * Last Modified: 2014-09-10
 */

// System imports
using System;
using System.Windows.Media.Media3D; // For 3D vectors

// Microsoft imports
using Microsoft.Kinect;



namespace NAO_Kinect
{
    /// <summary>
    /// This class takes a tracked body and generates useful data from it
    /// </summary>
    class BodyProcessing
    {
        /// <summary>
        /// Struct to return all relevant data to other classes
        /// </summary>
        internal struct BodyInfo
        {
            public float[] angles;
            public bool RHandOpen;
            public bool LHandOpen;
            public bool noTrackedBody;
        };

        /// <summary>
        /// Holds the KinectInterface class and the body we want angles for
        /// </summary>
        private static KinectInterface kinectInterface;
        private static Body trackedBody;
        BodyInfo bodyInfo;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="interfaceClass"> Reference to current kinect interface </param>
        public BodyProcessing(KinectInterface interfaceClass)
        {
            kinectInterface = interfaceClass;

            bodyInfo = new BodyInfo();
            bodyInfo.angles = new float[6];
        }

        /// <summary>
        /// Gets the usable angles of joints for sending to NAO
        /// </summary>
        /// <returns> struct of tupe BodyInfo </returns>
        public BodyInfo getInfo()
        {
            trackedBody = kinectInterface.getBody();

            if (trackedBody != null)
            {
                bodyInfo.noTrackedBody = false;

                var wristLeft = trackedBody.Joints[JointType.WristLeft].Position;
                var wristRight = trackedBody.Joints[JointType.WristRight].Position;

                var spineShoulder = trackedBody.Joints[JointType.SpineShoulder].Position;
                var spineBase = trackedBody.Joints[JointType.SpineBase].Position;

                var elbowLeft = trackedBody.Joints[JointType.ElbowLeft].Position;
                var elbowRight = trackedBody.Joints[JointType.ElbowRight].Position;

                var shoulderLeft = trackedBody.Joints[JointType.ShoulderLeft].Position;
                var shoulderRight = trackedBody.Joints[JointType.ShoulderRight].Position;

                var hipLeft = trackedBody.Joints[JointType.HipLeft].Position;
                var hipRight = trackedBody.Joints[JointType.HipRight].Position;

                switch (trackedBody.HandRightState)
                {
                    case HandState.Open:
                        bodyInfo.RHandOpen = true;
                        break;
                    case HandState.Closed:
                        bodyInfo.RHandOpen = false;
                        break;
                    case HandState.Lasso:
                        bodyInfo.RHandOpen = false;
                        break;
                }

                switch (trackedBody.HandLeftState)
                {
                    case HandState.Open:
                        bodyInfo.LHandOpen = true;
                        break;
                    case HandState.Closed:
                        bodyInfo.LHandOpen = false;
                        break;
                    case HandState.Lasso:
                        bodyInfo.LHandOpen = false;
                        break;
                }

                // Stores the right shoulder roll in radians
                bodyInfo.angles[0] = angleCalcXY(hipRight, shoulderRight, elbowRight);
                // Stores the left shoulder roll in radians
                bodyInfo.angles[1] = angleCalcXY(hipLeft, shoulderLeft, elbowLeft);

                // Stores the right elbow roll in radians
                bodyInfo.angles[2] = angleCalc3D(shoulderRight, elbowRight, wristRight);
                // Stores the left elbow roll in radians
                bodyInfo.angles[3] = angleCalc3D(shoulderLeft, elbowLeft, wristLeft);

                // Shoulder pitch should be same as shoulder roll but with angleCalcYZ
                // Stores the right shoulder pitch in radians
                bodyInfo.angles[4] = angleCalcYZ(hipRight, shoulderRight, elbowRight);
                // Stores the left shoulder pitch in radians
                bodyInfo.angles[5] = angleCalcYZ(hipLeft, shoulderLeft, elbowLeft);
            }
            else
            {
                bodyInfo.noTrackedBody = true;
            }
            return bodyInfo;
        }

        /// <summary>
        /// Calculates angle between three points with P1 as vertex
        /// </summary>
        /// <param name="p1X"> X coordinate of point 1 </param>
        /// <param name="p1Y"> Y coordinate of point 1 </param>
        /// <param name="p2X"> X coordinate of point 2 </param>
        /// <param name="p2Y"> Y coordinate of point 2 </param>
        /// <param name="p3X"> X coordinate of point 3 </param>
        /// <param name="p3Y"> Y coordinate of point 3 </param>
        /// <returns> Angle calculated </returns>
        private static float angleCalc(float p1X, float p1Y, float p2X, float p2Y, float p3X, float p3Y)
        {
            var p12 = (float)Math.Sqrt(((p1X - p2X) * (p1X - p2X)) + ((p1Y - p2Y) * (p1Y - p2Y)));
            var p13 = (float)Math.Sqrt(((p1X - p3X) * (p1X - p3X)) + ((p1Y - p3Y) * (p1Y - p3Y)));
            var p23 = (float)Math.Sqrt(((p2X - p3X) * (p2X - p3X)) + ((p2Y - p3Y) * (p2Y - p3Y)));

            return (float)Math.Acos(((p12 * p12) + (p13 * p13) - (p23 * p23)) / (2 * p12 * p13));
        }

        // Calculates angle between a<-b and b->c for situation a->b->c
        private static float angleCalc3D(CameraSpacePoint a, CameraSpacePoint b, CameraSpacePoint c)
        {
            Vector3D ba = new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            Vector3D bc = new Vector3D(c.X - b.X, c.Y - b.Y, c.Z - b.Z);

            float angle = (float)Vector3D.AngleBetween(ba, bc); // degrees
            return (float)(Math.PI / 180) * angle; // radians
        }

        // Calculates angle between a<-b and b->c for situation a->b->c on XY plane only
        private static float angleCalcXY(CameraSpacePoint a, CameraSpacePoint b, CameraSpacePoint c)
        {
            Vector3D ba = new Vector3D(a.X - b.X, a.Y - b.Y, 0);
            Vector3D bc = new Vector3D(c.X - b.X, c.Y - b.Y, 0);

            float angle = (float)Vector3D.AngleBetween(ba, bc); // degrees
            return (float)(Math.PI / 180) * angle; // radians
        }

        // Calculates angle between a<-b and b->c for situation a->b->c on YZ plane only
        private static float angleCalcYZ(CameraSpacePoint a, CameraSpacePoint b, CameraSpacePoint c)
        {
            Vector3D ba = new Vector3D(0, a.Y - b.Y, a.Z - b.Z);
            Vector3D bc = new Vector3D(0, c.Y - b.Y, c.Z - b.Z);

            float angle = (float)Vector3D.AngleBetween(ba, bc); // degrees
            return (float)(Math.PI / 180) * angle; // radians
        }
    }
}
