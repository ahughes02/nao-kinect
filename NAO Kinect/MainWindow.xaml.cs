/*
 * This software was developed by Austin Hughes
 * Last Modified: 2014-09-04
 */

// System Imports
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace NAO_Kinect
{
    /// <summary>
    /// This class handles all the UI logic for the application
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Class declarations
        /// </summary>
        private Motion naoMotion;
        private BodyProcessing bodyProcessing;
        private KinectInterface kinectInterface;
        
        /// <summary>
        /// Variables for calibrating and sending angles to NAO
        /// </summary>
        private bool calibrated;
        private bool changeAngles;
        private string rHandStatus = "none";
        private string lHandStatus = "none";
        private readonly string[] jointNames = {"RShoulderRoll", "LShoulderRoll", "RElbowRoll", "LElbowRoll"};
        private float[] calibrationAngles = new float[6];
        private float[] oldAngles = new float[6];

        /// <summary>
        /// Class constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // call the motion constructor
            naoMotion = new Motion();
        }

        /// ********************************************************
        /// 
        ///                     UI EVENTS
        /// 
        /// ********************************************************

        /// <summary>
        /// Event handler for the main UI being loaded
        /// </summary>
        /// <param name="sender"> object that generated the event </param>
        /// <param name="e"> any additional arguments </param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            kinectInterface = new KinectInterface();
            kinectInterface.NewFrame += kinectInterface_NewFrame;
            kinectInterface.NewSpeech += kinectInterface_NewSpeech;

            // Starts the skeletonAngles class and sends to kinectSkeleton reference to it
            bodyProcessing = new BodyProcessing(kinectInterface);
        }

        /// <summary>
        /// Called when the window is unloaded, cleans up anything that needs to be cleaned before the program exits
        /// </summary>
        /// <param name="sender"> object that created the event </param>
        /// <param name="e"> any additional arguments </param>
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinectInterface != null)
            {
                kinectInterface.end();
            }
        }

        /// <summary>
        /// event handler for start button click, enables NAO connection
        /// </summary>
        /// <param name="sender"> object that sent the event </param>
        /// <param name="e"> any additional arguments </param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            naoMotion.connect(ipBox.Text);

            changeAngles = true;

            stopButton.IsEnabled = true;
            startButton.IsEnabled = false;
        }

        /// <summary>
        /// event handler for stop button click, disables NAO connection
        /// </summary>
        /// <param name="sender"> object that sent the event </param>
        /// <param name="e"> any additional arguments </param>
        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            changeAngles = false;

            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
        }

        private void calibrateButton_Click(object sender, RoutedEventArgs e)
        {
            calibrated = false;
        }

        /// ********************************************************
        /// 
        ///                     KINECT EVENTS
        /// 
        /// ********************************************************

        /// <summary>
        /// Event handler for new frames created by the kinectBody class
        /// </summary>
        /// <param name="sender"> object that generated the event </param>
        /// <param name="e"> any additional arguments </param>
        private void kinectInterface_NewFrame(object sender, EventArgs e)
        {
            // gets the image from kinectIntraface class and updates the image in the UI
            Image.Source = kinectInterface.getImage();

            // gets array of info from bodyProcessing
            var info = bodyProcessing.getInfo();

            // array to store angles with calibration
            var finalAngles = new float[4];

            // checks for calibration flag and then updates calibration
            if (calibrated == false)
            {
                for (var x = 0; x < 4; x++)
                {
                    calibrationAngles[x] = info[x];
                }
                calibrated = true;
            }
  
            // generate calibrated angles
            for (var x = 0; x < 4; x++)
            {
                finalAngles[x] = info[x] - calibrationAngles[x]; // adjustment to work with NAO robot angles
            }

            // debug output, displays angles in radians
            debug1.Text = "Angle 1: " + info[0] + "\n";
            debug1.Text += "Angle 2: " + info[1] + "\n";
            debug1.Text += "Angle 3: " + info[2] + "\n";
            debug1.Text += "Angle 4: " + info[3] + "\n";
            debug1.Text += "------------------------\n";
            debug1.Text += "Calibrated Angle 1: " + finalAngles[0] + "\n";
            debug1.Text += "Calibrated Angle 2: " + finalAngles[1] + "\n";
            debug1.Text += "Calibrated Angle 3: " + finalAngles[2] + "\n";
            debug1.Text += "Calibrated Angle 4: " + finalAngles[3] + "\n";

            // Check to make sure that angle has changed enough to send new angle and update angle if it has
            for (var x = 0; x < 4; x++)
            {
                if (changeAngles &&
                    (Math.Abs(oldAngles[x]) - Math.Abs(finalAngles[x]) > .1 ||
                     Math.Abs(oldAngles[x]) - Math.Abs(finalAngles[x]) < .1))
                {
                    oldAngles[0] = finalAngles[0];
                    updateNAO(finalAngles[x], jointNames[x]);
                }
            }

            // Update right hand
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (info[4] == 1 && rHandStatus != "open")
            {
                naoMotion.openHand("RHand");
                rHandStatus = "open";
            }
            else if(rHandStatus != "closed")
            {
                naoMotion.closeHand("RHand");
                rHandStatus = "closed";
            }

            // Update left hand
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (info[5] == 1 && lHandStatus != "open")
            {
                naoMotion.openHand("LHand");
                lHandStatus = "open";
            }
            else if (lHandStatus != "closed")
            {
                naoMotion.closeHand("LHand");
                lHandStatus = "closed";
            }
        }

        /// <summary>
        /// Event handler for new frames created by the kinectBody class
        /// </summary>
        /// <param name="sender"> object that generated the event </param>
        /// <param name="e"> any additional arguments </param>
        private void kinectInterface_NewSpeech(object sender, EventArgs e)
        {
            var result = kinectInterface.getResult();
            var semanticResult = kinectInterface.getSemanticResult();
            var confidence = kinectInterface.getConfidence();

            if (confidence > 0.6) //If confidence of recognized speech is greater than 60%
            {
                // Debug output, tells what phrase was recongnized and the confidence
                debug2.Text = "Recognized: " + result + " \nConfidence: " + confidence;

                // if statements to preform actions based on results
                if (semanticResult == "on" && startButton.IsEnabled)
                {
                    startButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }

                if (semanticResult == "off" && stopButton.IsEnabled)
                {
                    stopButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }

                if (semanticResult == "calibrate")
                {
                    calibrated = false;
                }
            }
            else // Else say that it was rejected and confidence
            {
                debug2.Text = "Rejected " + " \nConfidence: " + confidence;
            }
        }

        /// ********************************************************
        /// 
        ///                    NAO METHODS
        /// 
        /// ********************************************************

        private void updateNAO(float angle, string joint)
        {
            if (joint == "RShoulderRoll" || joint == "LShoulderRoll")
            {
                if (angle > 1.3)
                {
                    try
                    {
                        naoMotion.moveJoint(1.3f, joint);
                    }
                    catch (Exception)
                    { }
                }
                else if (angle < -.3)
                {
                    try
                    {
                        naoMotion.moveJoint(-.3f, joint);
                    }
                    catch (Exception)
                    { }
                }
                else
                {
                    try
                    {
                        naoMotion.moveJoint(angle, joint);
                    }
                    catch (Exception)
                    { }
                }
            }

            if (joint == "RElbowRoll" || joint == "LElbowRoll")
            {
                if (angle < -1.5)
                {
                    try
                    {
                        naoMotion.moveJoint(-1.5f, joint);
                    }
                    catch (Exception)
                    { }
                }
                else if (angle > -.03)
                {
                    try
                    {
                        naoMotion.moveJoint(-.03f, joint);
                    }
                    catch (Exception)
                    { }
                }
                else
                {
                    try
                    {
                        naoMotion.moveJoint(angle, joint);
                    }
                    catch (Exception)
                    { }
                }
            }
        }
    }
}
