/*
 * This software was developed by Austin Hughes
 * Last Modified: 2014-09-04
 */

// System Imports
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace NAO_Kinect
{
    /// <summary>
    /// This class handles all the UI logic for the application
    /// and provides communication between the other classes
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Classes
        /// </summary>
        private Motion naoMotion;
        private BodyProcessing bodyProcessing;
        private KinectInterface kinectInterface;
        private Thread kinectThread;
        
        /// <summary>
        /// Variables
        /// </summary>
        private bool calibrated;
        private bool changeAngles;
        private bool invert;
        private string rHandStatus = "unknown";
        private string lHandStatus = "unkown";
        private readonly string[] invertedJointNames = { "LShoulderRoll", "RShoulderRoll", "LElbowRoll", "RElbowRoll" };
        private readonly string[] jointNames = {"RShoulderRoll", "LShoulderRoll", "RElbowRoll", "LElbowRoll"};
        private float[] calibrationAngles = new float[6];
        private float[] oldAngles = new float[6];
        private float[] finalAngles = new float[4];

        /// <summary>
        /// Data structures
        /// </summary>
        private NAO_Kinect.BodyProcessing.BodyInfo info;

        /// <summary>
        /// Timer 
        /// </summary>
        private DispatcherTimer motionTimer = new DispatcherTimer();
        
        /// <summary>
        /// Class constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            invert = true;

            // Call the motion constructor
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
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Creates the kinectInterface class and registers event handlers
            kinectInterface = new KinectInterface();
            kinectInterface.start();
           // kinectThread = new Thread(kinectInterface.start);

            //kinectThread.Start();

            kinectInterface.NewFrame += kinectInterface_NewFrame;
            kinectInterface.NewSpeech += kinectInterface_NewSpeech;

            // Creates the bodyProcessing class and sends to kinectSkeleton reference to it
            bodyProcessing = new BodyProcessing(kinectInterface);

            // Create a timer for event based NAO update. 
            motionTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)Math.Ceiling(1000.0 / 3));
            motionTimer.Start();

            motionTimer.Tick += motionTimer_Tick;
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
                kinectThread.Abort();
            }
        }

        /// <summary>
        /// Event handler for start button click, enables NAO connection
        /// </summary>
        /// <param name="sender"> Object that sent the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            naoMotion.connect(ipBox.Text);

            changeAngles = true;

            stopButton.IsEnabled = true;
            startButton.IsEnabled = false;
        }

        /// <summary>
        /// Event handler for stop button click, disables NAO connection
        /// </summary>
        /// <param name="sender"> Object that sent the event </param>
        /// <param name="e"> Any additional arguments </param>
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
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void kinectInterface_NewFrame(object sender, EventArgs e)
        {
            // Gets the image from kinectInterface class and updates the image in the UI
            Image.Source = kinectInterface.getImage();
        }

        /// <summary>
        /// Event handler for new frames created by the kinectBody class
        /// </summary>
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void kinectInterface_NewSpeech(object sender, EventArgs e)
        {
            var result = kinectInterface.getResult();
            var semanticResult = kinectInterface.getSemanticResult();
            var confidence = kinectInterface.getConfidence();

            // If confidence of recognized speech is greater than 60%
            if (confidence > 0.6) 
            {
                // Debug output, tells what phrase was recongnized and the confidence
                debug2.Text = "Recognized: " + result + " \nConfidence: " + confidence;

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

        private void motionTimer_Tick(object sender, EventArgs e)
        {
            // Gets array of info from bodyProcessing
            info = bodyProcessing.getInfo();

            if (!info.noTrackedBody)
            {
                // Checks for calibration flag and then updates calibration if it is set to true
                if (calibrated == false)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        calibrationAngles[x] = info.angles[x];
                    }
                    calibrated = true;
                }

                // Generate calibrated angles
                for (var x = 0; x < 4; x++)
                {
                    finalAngles[x] = info.angles[x] - calibrationAngles[x]; // adjustment to work with NAO robot angles
                }

                // Debug output, displays angles in radians
                debug1.Text = "RShoulder Roll:\t " + info.angles[0] + "\n";
                debug1.Text += "LShoulder Roll:\t " + info.angles[1] + "\n";
                debug1.Text += "RElbow Roll:\t " + info.angles[2] + "\n";
                debug1.Text += "LElbow Roll:\t " + info.angles[3] + "\n";
                debug1.Text += "----------------------\n";
                debug1.Text += "Calibrated RSR:\t " + finalAngles[0] + "\n";
                debug1.Text += "Calibrated LSR:\t " + finalAngles[1] + "\n";
                debug1.Text += "Calibrated RER:\t " + finalAngles[2] + "\n";
                debug1.Text += "Calibrated LER:\t " + finalAngles[3] + "\n";
                debug1.Text += "----------------------\n";
                debug1.Text += "RHand Status:\t " + info.RHandOpen + "\n";
                debug1.Text += "LHand Status:\t " + info.LHandOpen + "\n";

                // Check to make sure that angle has changed enough to send new angle and update angle if it has
                debug3.Text = "";
                for (var x = 0; x < 4; x++)
                {
                    if (changeAngles && (Math.Abs(oldAngles[x]) - Math.Abs(finalAngles[x]) > .05 || Math.Abs(oldAngles[x]) - Math.Abs(finalAngles[x]) < .05))
                    {
                        oldAngles[x] = finalAngles[x];
                        if (invert)
                        {
                            updateNAO(finalAngles[x], invertedJointNames[x]);
                        }
                        else
                        {
                            updateNAO(finalAngles[x], jointNames[x]);
                        }

                        debug3.Text += "I sent: " + finalAngles[x] + "\n";
                    }
                }

                // Update right hand
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (changeAngles && info.RHandOpen && rHandStatus != "open")
                {
                    rHandStatus = "open";

                    if (invert)
                    {
                        if (!naoMotion.openHand("LHand"))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                            rHandStatus = "unknown";
                        }
                    }
                    else
                    {
                        if (!naoMotion.openHand("RHand"))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                            rHandStatus = "unknown";
                        }
                    }
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (changeAngles && !info.RHandOpen && rHandStatus != "closed")
                {
                    rHandStatus = "closed";

                    if (invert)
                    {
                        if (!naoMotion.closeHand("LHand"))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                            rHandStatus = "unknown";
                        }
                    }
                    else
                    {
                        if (!naoMotion.closeHand("RHand"))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                            rHandStatus = "unknown";
                        }
                    }
                }

                // Update left hand
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (changeAngles && info.LHandOpen && lHandStatus != "open")
                {
                    lHandStatus = "open";

                    if (invert)
                    {
                        if (!naoMotion.openHand("RHand"))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                            rHandStatus = "unknown";
                        }
                    }
                    else
                    {
                        if (!naoMotion.openHand("LHand"))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                            rHandStatus = "unknown";
                        }
                    }
                }

                if (changeAngles && !info.LHandOpen && lHandStatus != "closed")
                {
                    lHandStatus = "closed";

                    if (invert)
                    {
                        if (!naoMotion.closeHand("RHand"))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                            rHandStatus = "unknown";
                        }
                    }
                    else
                    {
                        if (!naoMotion.closeHand("LHand"))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                            rHandStatus = "unknown";
                        }
                    }
                }
            }
            else
            {
                debug1.Text = "No currently tracked body";
            }
        }

        /// ********************************************************
        /// 
        ///                    NAO METHODS
        /// 
        /// ********************************************************

        private void updateNAO(float angle, string joint)
        {
            if (joint == "RShoulderRoll")
            {
                angle = 0 - angle;

                if (!naoMotion.moveJoint(angle, joint))
                {
                    debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                }
            }

            if (joint == "LRshoulderRoll")
            {
                if (!naoMotion.moveJoint(angle, joint))
                {
                    debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                }
            }

            if (joint == "RElbowRoll" || joint == "LElbowRoll")
            {
                if (angle < -1.5)
                {
                    try
                    {
                        if (!naoMotion.moveJoint(-1.5f, joint))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                        }
                    }
                    catch (Exception)
                    { }
                }
                else if (angle > -.03)
                {
                    try
                    {
                        if (!naoMotion.moveJoint(-.03f, joint))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                        }
                    }
                    catch (Exception)
                    { }
                }
                else
                {
                    try
                    {
                        if (!naoMotion.moveJoint(angle, joint))
                        {
                            debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
                        }
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        private void invertCheck_Checked(object sender, RoutedEventArgs e)
        {
            invert = true;
        }

        private void invertCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            invert = false;
        }
    }
}
