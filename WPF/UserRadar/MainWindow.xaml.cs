using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Zig;

namespace UserRadar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<int, Image> UserToRadarIcon;
        public MainWindow()
        {
            UserToRadarIcon = new Dictionary<int, Image>();
            InitializeComponent();           
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            // listen to any status change for Kinects.
            KinectSensor.KinectSensors.StatusChanged += this.KinectsStatusChanged;

            // show status for each sensor that is found now.
            if (KinectSensor.KinectSensors.Count == 0)
            {
                label1.Content = "No Sensor Connected!";
            }
            else
            {
                label1.Content = "Detected Sensors: " + KinectSensor.KinectSensors.Count.ToString();
            }
            foreach (KinectSensor kinect in KinectSensor.KinectSensors)
            {
                this.ShowStatus(kinect, kinect.Status);
            }
            
        }
        private void KinectsStatusChanged(object sender, StatusChangedEventArgs e)
        {
            this.ShowStatus(e.Sensor, e.Status);
        }


        private void ShowStatus(KinectSensor kinectSensor, KinectStatus kinectStatus)
        {
            
            if (KinectStatus.Disconnected == kinectStatus)
            {
                label1.Content = "Device ID: " + kinectSensor.DeviceConnectionId + " disconnected at " + DateTime.Now.ToString();
            }

            else if (KinectStatus.Connected == kinectStatus)
            {
                label1.Content = "Device ID: " + kinectSensor.DeviceConnectionId + "\n connected at " + DateTime.Now.ToString();
                this.SensorConnected(kinectSensor);
            }
        }
        private void SensorConnected(KinectSensor kinectSensor)
        {
            
            ZigInput zig = new ZigInput(kinectSensor);
            zig.UserFound += new EventHandler<UserEventArgs>(zig_UserFound);
            zig.UserLost += new EventHandler<UserEventArgs>(zig_UserLost);
            //kinectSensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinectSensor_SkeletonFrameReady);
        }

        
        void zig_UserFound(object sender, UserEventArgs e)
        {
            label1.Content += "new user found, id: " + e.User.Id + "\n";
            Image im = new Image();
            im.Source = image2.Source;
            im.Width = image2.Width;
            im.Height = image2.Height;
            canvas1.Children.Add(im);
            UserToRadarIcon.Add(e.User.Id, im);            
            e.User.UpdateUser += new EventHandler<EventArgs<ZigTrackedUser>>(User_UpdateUser);         
        }
        float RealWorldToCanvasLeft(float x)
        {
            return x/4.0f + .5f;
        }
        float RealWorldToCanvasTop(float z)
        {
            return z/4.0f;
        }

        void User_UpdateUser(object sender, EventArgs<ZigTrackedUser> e)
        {
            Image item = this.UserToRadarIcon[e.Item.Id];
            Canvas.SetLeft(item, canvas1.Width * RealWorldToCanvasLeft(e.Item.Position.X) - item.Width / 2);
            Canvas.SetTop(item, canvas1.Height * RealWorldToCanvasTop(e.Item.Position.Z) - item.Height / 2);
        }
        void zig_UserLost(object sender, UserEventArgs e)
        {
            label1.Content += "user lost, id: " + e.User.Id + "\n";
            //add event to user (later TODO)
            Image item = this.UserToRadarIcon[e.User.Id];
            canvas1.Children.Remove(item);
            UserToRadarIcon.Remove(e.User.Id);
        }

        //void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        //{
        //    SkeletonFrame sf = e.OpenSkeletonFrame();
        //    if (sf.SkeletonArrayLength == 0)
        //    {
        //        return;
        //    }
        //    Skeleton[] userList = new Skeleton[sf.SkeletonArrayLength];
        //    sf.CopySkeletonDataTo(userList);
        //    string s = "";
        //    foreach (Skeleton skel in userList)
        //    {
        //        if (skel.TrackingState != SkeletonTrackingState.NotTracked)
        //        {
        //            s += "Skeleton: " + skel.TrackingId.ToString() + " at " + string.Format("x: {0}, y: {1}, z: {2}", skel.Position.X,skel.Position.Y,skel.Position.Z) + "\n";
        //        }
        //    }
        //    label1.Content = s;
            
        //}
    }
}
