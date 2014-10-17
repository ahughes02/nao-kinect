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

                var shoulderCenter = trackedBody.Joints[JointType.SpineShoulder].Position;

                var wristLeft = trackedBody.Joints[JointType.WristLeft].Position;
                var wristRight = trackedBody.Joints[JointType.WristRight].Position;

                //var spineShoulder = trackedBody.Joints[JointType.SpineShoulder].Position;
                //var spineBase = trackedBody.Joints[JointType.SpineBase].Position;

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

                var rollRefRight = new CameraSpacePoint();
                rollRefRight.X = shoulderRight.X;
                rollRefRight.Y = elbowRight.Y;
                rollRefRight.Z = elbowRight.Z;

                var rollRefLeft = new CameraSpacePoint();
                rollRefLeft.X = shoulderLeft.X;
                rollRefLeft.Y = elbowLeft.Y;
                rollRefLeft.Z = elbowLeft.Z;



                /*if (elbowRight.Y < shoulderRight.Y)
                {
                    // Stores the right shoulder roll in radians
                    bodyInfo.angles[0] = angleCalc3D(rollRefRight, shoulderRight, elbowRight);
                }

                if (elbowLeft.Y < elbowRight.Y)
                {
                    // Stores the left shoulder roll in radians
                    bodyInfo.angles[1] = angleCalc3D(rollRefLeft, shoulderLeft, elbowLeft);
                }*/

                bodyInfo.angles[0] = angleCalc3D(hipRight, shoulderRight, elbowRight);
                bodyInfo.angles[1] = angleCalc3D(hipLeft, shoulderLeft, elbowLeft);

                // Stores the right elbow roll in radians
                bodyInfo.angles[2] = 3.0f - angleCalc3D(shoulderRight, elbowRight, wristRight);
                // Stores the left elbow roll in radians
                bodyInfo.angles[3] = 3.0f - angleCalc3D(shoulderLeft, elbowLeft, wristLeft);

                // Shoulder pitch should be same as shoulder roll but with angleCalcYZ
                // Stores the right shoulder pitch in radians
                bodyInfo.angles[4] = 0 - angleCalcYZ(hipRight, shoulderRight, elbowRight);
                // Stores the left shoulder pitch in radians
                bodyInfo.angles[5] = 0 - angleCalcYZ(hipLeft, shoulderLeft, elbowLeft);
            }
            else
            {
                bodyInfo.noTrackedBody = true;
            }
            return bodyInfo;
        }



        // Calculates angle between a<-b and b->c for situation a->b->c
        private static float angleCalc3D(CameraSpacePoint a, CameraSpacePoint b, CameraSpacePoint c)
        {
            var ba = new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            var bc = new Vector3D(c.X - b.X, c.Y - b.Y, c.Z - b.Z);

            var angle = (float)Vector3D.AngleBetween(ba, bc); // degrees
            return (float)(Math.PI / 180) * angle; // radians
        }

        // Calculates angle between a<-b and b->c for situation a->b->c
        private static float angleCalcXZ(CameraSpacePoint a, CameraSpacePoint b, CameraSpacePoint c)
        {
            var ba = new Vector3D(a.X - b.X, 0, a.Z - b.Z);
            var bc = new Vector3D(c.X - b.X, 0, c.Z - b.Z);

            var angle = (float)Vector3D.AngleBetween(ba, bc); // degrees
            return (float)(Math.PI / 180) * angle; // radians
        }

        // Calculates angle between a<-b and b->c for situation a->b->c on XY plane only
        private static float angleCalcXY(CameraSpacePoint a, CameraSpacePoint b, CameraSpacePoint c)
        {
            var ba = new Vector3D(a.X - b.X, a.Y - b.Y, 0);
            var bc = new Vector3D(c.X - b.X, c.Y - b.Y, 0);

            var angle = (float)Vector3D.AngleBetween(ba, bc); // degrees

            return (float)(Math.PI / 180) * angle; // radians
        }

        // Calculates angle between a<-b and b->c for situation a->b->c on YZ plane only
        private static float angleCalcYZ(CameraSpacePoint a, CameraSpacePoint b, CameraSpacePoint c)
        {
            var ba = new Vector3D(0, a.Y - b.Y, a.Z - b.Z);
            var bc = new Vector3D(0, c.Y - b.Y, c.Z - b.Z);

            var angle = (float)Vector3D.AngleBetween(ba, bc); // degrees
            return (float)(Math.PI / 180) * angle; // radians
        }
    }
}
