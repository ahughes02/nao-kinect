/*
 * This file was created by Austin Hughes and Stetson Gafford
 * Last Modified: 2014-09-10
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
        //private Thread kinectThread;
        
        /// <summary>
        /// Variables
        /// </summary>
        private bool allowNaoUpdates = false;
        private bool invert = true;
        private string rHandStatus = "unknown";
        private string lHandStatus = "unkown";
        private readonly string[] invertedJointNames = {"LShoulderRoll", "RShoulderRoll", "LElbowRoll", "RElbowRoll", "LShoulderPitch", "RShoulderPitch"};
        private readonly string[] jointNames = { "RShoulderRoll", "LShoulderRoll", "RElbowRoll", "LElbowRoll", "RShoulderPitch", "LShoulderPitch" };
        private float[] offset = {0.4f, 0.4f, 0.2f, 0.2f, -1.6f, -1.6f};
        private float[] oldAngles = new float[6];
        private float[] finalAngles = new float[6];

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
            debug1.Text = "";
            debug2.Text = "";
            debug3.Text = "";

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
            motionTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)Math.Ceiling(1000.0 / 7));
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
                //kinectThread.Abort();
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

            allowNaoUpdates = true;

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
            allowNaoUpdates = false;

            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
        }

        /// <summary>
        /// Event handler for invert check box being checked
        /// </summary>
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void invertCheck_Checked(object sender, RoutedEventArgs e)
        {
            invert = true;
        }

        /// <summary>
        /// Event handler for invert check box being unchecked
        /// </summary>
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void invertCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            invert = false;
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
            }
            else // Else say that it was rejected and confidence
            {
                debug2.Text = "Rejected " + " \nConfidence: " + confidence;
            }
        }

        /// <summary>
        /// Timer to rate limit NAO joint updates
        /// </summary>
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void motionTimer_Tick(object sender, EventArgs e)
        {
            // Gets array of info from bodyProcessing
            info = bodyProcessing.getInfo();

            if (!info.noTrackedBody)
            {

                // Generate calibrated angles
                for (var x = 0; x < 6; x++)
                {
                    finalAngles[x] = info.angles[x] - offset[x]; // adjustment to work with NAO robot angles
                }

                // Show angle informationin the UI
                debug1.Text = "RShoulder Roll:\t " + info.angles[0] + "\n";
                debug1.Text += "LShoulder Roll:\t " + info.angles[1] + "\n";
                debug1.Text += "RElbow Roll:\t " + info.angles[2] + "\n";
                debug1.Text += "LElbow Roll:\t " + info.angles[3] + "\n";
                debug1.Text += "RShoulder Pitch:\t " + info.angles[4] + "\n";
                debug1.Text += "LShoulder Pitch:\t " + info.angles[5] + "\n";
                debug1.Text += "----------------------\n";
                debug1.Text += "Calibrated RSR:\t " + finalAngles[0] + "\n";
                debug1.Text += "Calibrated LSR:\t " + finalAngles[1] + "\n";
                debug1.Text += "Calibrated RER:\t " + finalAngles[2] + "\n";
                debug1.Text += "Calibrated LER:\t " + finalAngles[3] + "\n";
                debug1.Text += "Calibrated RSP:\t " + finalAngles[4] + "\n";
                debug1.Text += "Calibrated LSP:\t " + finalAngles[5] + "\n";
                debug1.Text += "----------------------\n";
                debug1.Text += "RHand Status:\t " + info.RHandOpen + "\n";
                debug1.Text += "LHand Status:\t " + info.LHandOpen + "\n";

                // Check if updates should be sent to NAO
                if (allowNaoUpdates)
                {
                    // Check to make sure that angle has changed enough to send new angle and update angle if it has
                    for (var x = 0; x < 6; x++)
                    {
                        if ((Math.Abs(oldAngles[x]) - Math.Abs(finalAngles[x]) > .1 || Math.Abs(oldAngles[x]) - Math.Abs(finalAngles[x]) < .1))
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
                        }
                    }

                    // update right hand
                    switch (info.RHandOpen)
                    {
                        case true:
                            if (rHandStatus == "open")
                            {
                                break;
                            }
                            rHandStatus = "open";
                            if(invert)
                            {
                                naoMotion.openHand("LHand");
                                break;
                            }
                            naoMotion.openHand("RHand");
                            break;
                        case false:
                            if (rHandStatus == "closed")
                            {
                                break;
                            }
                            rHandStatus = "closed";
                            if(invert)
                            {
                                naoMotion.closeHand("LHand");
                            
                                break;
                            }
                            naoMotion.closeHand("RHand");
                            break;
                    }

                    // update left hand
                    switch (info.LHandOpen)
                    {
                        case true:
                            if (lHandStatus == "open")
                            {
                                break;
                            }
                            lHandStatus = "open";
                            if(invert)
                            {
                                naoMotion.openHand("RHand");
                                break;
                            }
                            naoMotion.openHand("LHand");
                            break;
                        case false:
                            if (lHandStatus == "closed")
                            {
                                break;
                            }
                            lHandStatus = "closed";
                            if(invert)
                            {
                                naoMotion.closeHand("RHand");
                            
                                break;
                            }
                            naoMotion.closeHand("LHand");
                            break;
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
            // RShoulderRoll and RElbowRoll require inverted angles
            if (joint == "RShoulderRoll" || joint == "LElbowRoll")
            {
                // Invert the angle
                angle = (0 - angle);
            }

            // Check for error when moving joint
            if (!naoMotion.moveJoint(angle, joint))
            {
                debug3.Text = "Exception occured when communicating with NAO check C:\\NAO Motion\\ for details";
            }
        }
    }
}
