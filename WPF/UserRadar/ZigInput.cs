using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace Zig
{





    public enum ZigJointId
    {
        None = 0,
        Head,
        Neck,
        Torso,
        Waist,
        LeftCollar,
        LeftShoulder,
        LeftElbow,
        LeftWrist,
        LeftHand,
        LeftFingertip,
        RightCollar,
        RightShoulder,
        RightElbow,
        RightWrist,
        RightHand,
        RightFingertip,
        LeftHip,
        LeftKnee,
        LeftAnkle,
        LeftFoot,
        RightHip,
        RightKnee,
        RightAnkle,
        RightFoot
    }
    


    public class ZigInputJoint
    {
        public ZigJointId Id { get; private set; }
        public SkeletonPoint Position;
        public Vector4 Rotation;
        public bool GoodPosition;
        public bool GoodRotation;

        public ZigInputJoint(ZigJointId id) :
            this(id, new SkeletonPoint(), new Vector4())
        {
            GoodPosition = false;
            GoodRotation = false;
        }

        public ZigInputJoint(ZigJointId id, SkeletonPoint position, Vector4 rotation)
        {
            Id = id;
            Position = position;
            Rotation = rotation;
        }
    }


    public class ZigTrackedUser
    {
    
	    
        public int Id { get; private set; }
        public bool PositionTracked { get; private set; }
        public SkeletonPoint Position { get; private set; }
        public bool SkeletonTracked { get; private set; }
        public ZigInputJoint[] Skeleton { get; private set; }
        public int sensorID;

	    public ZigTrackedUser(ZigInputUser userData) {
            Skeleton = new ZigInputJoint[Enum.GetValues(typeof(ZigJointId)).Length];
            for (int i=0; i<Skeleton.Length; i++) {
                Skeleton[i] = new ZigInputJoint((ZigJointId)i);
            }
		    Update(userData);
	    }

        public event EventHandler<EventArgs<ZigTrackedUser>> UpdateUser;
        protected void OnUpdateUser()
        {
            if (null != UpdateUser)
            {
                UpdateUser.Invoke(this, new EventArgs<ZigTrackedUser>(this));
            }
        }

	
	    public void Update(ZigInputUser userData) {
		    Id = userData.Id;
            PositionTracked = true;
            Position = userData.CenterOfMass;
            SkeletonTracked = userData.Tracked;
            foreach (ZigInputJoint j in userData.SkeletonData) {
                Skeleton[(int)j.Id] = j;
            }
            OnUpdateUser();
	    }
    }

    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T item) {
            Item = item;
        }
        public T Item { get; private set; }
    }


    public class ZigInputUser
    {
        public int Id;
        public bool Tracked;
        public SkeletonPoint CenterOfMass;
        public List<ZigInputJoint> SkeletonData;
        public ZigInputUser(int id, SkeletonPoint com)
        {
            Id = id;
            CenterOfMass = com;
        }
        public int sensorID;
    }
   
    public class UserEventArgs : EventArgs
    {
        public UserEventArgs(ZigTrackedUser user)
        {
            User = user;
        }
        public ZigTrackedUser User { get; private set; }
    }


    public class ZigInput
    {

        ZigJointId NuiToZig(JointType nuiJoint)
        {
            switch (nuiJoint)
            {
                case JointType.HipCenter: return ZigJointId.Waist;
                case JointType.Spine: return ZigJointId.Torso;
                case JointType.ShoulderCenter: return ZigJointId.Neck;
                case JointType.Head: return ZigJointId.Head;
                case JointType.ShoulderLeft: return ZigJointId.LeftShoulder;
                case JointType.ElbowLeft: return ZigJointId.LeftElbow;
                case JointType.WristLeft: return ZigJointId.LeftWrist;
                case JointType.HandLeft: return ZigJointId.LeftHand;
                case JointType.ShoulderRight: return ZigJointId.RightShoulder;
                case JointType.ElbowRight: return ZigJointId.RightElbow;
                case JointType.WristRight: return ZigJointId.RightWrist;
                case JointType.HandRight: return ZigJointId.RightHand;
                case JointType.HipLeft: return ZigJointId.LeftHip;
                case JointType.KneeLeft: return ZigJointId.LeftKnee;
                case JointType.AnkleLeft: return ZigJointId.LeftAnkle;
                case JointType.FootLeft: return ZigJointId.LeftFoot;
                case JointType.HipRight: return ZigJointId.RightHip;
                case JointType.KneeRight: return ZigJointId.RightKnee;
                case JointType.AnkleRight: return ZigJointId.RightAnkle;
                case JointType.FootRight: return ZigJointId.RightFoot;
            }
            return 0;
        }

        public ZigInput(KinectSensor sensor)
        {
            sensor.Start();
            sensor.SkeletonStream.AppChoosesSkeletons = true;
            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(sensor_SkeletonFrameReady);
        }

        void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame sf = e.OpenSkeletonFrame();
            if ((sf == null) || (sf.SkeletonArrayLength == 0))
            {
                return;
            }

            
            // foreach user
            List<ZigInputUser> users = new List<ZigInputUser>();
            
            Skeleton[] userList = new Skeleton[sf.SkeletonArrayLength];
            sf.CopySkeletonDataTo(userList);            
            foreach (Skeleton skeleton in userList)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.NotTracked)
                {
                    continue;
                }

                // skeleton data
                List<ZigInputJoint> joints = new List<ZigInputJoint>();
                bool tracked = skeleton.TrackingState == SkeletonTrackingState.Tracked;
                if (tracked)
                {
                    
                    foreach (var input in skeleton.Joints.Zip(skeleton.BoneOrientations, (p1, p2) => new {joint = p1, rotation = p2}))
                    {
                        // skip joints that aren't tracked
                        if (input.joint.TrackingState == JointTrackingState.NotTracked)
                        {
                            continue;
                        }
                        ZigInputJoint jointOut = new ZigInputJoint(NuiToZig(input.joint.JointType));
                        jointOut.Position = input.joint.Position;
                        jointOut.Rotation = input.rotation.AbsoluteRotation.Quaternion;
                        jointOut.GoodRotation = true;
                        jointOut.GoodPosition = true;
                        joints.Add(jointOut);
                    }
                }

                ZigInputUser user = new ZigInputUser((int)skeleton.TrackingId, skeleton.Position);
                user.Tracked = tracked;
                user.SkeletonData = joints;
                users.Add(user);
            }

            HandleReaderNewUsersFrame(users);

        }



        Dictionary<int, ZigTrackedUser> trackedUsers = new Dictionary<int, ZigTrackedUser>();

        public event EventHandler<UserEventArgs> UserFound;
        protected void OnUserFound(ZigTrackedUser user)
        {
            if (null != user)
            {
                UserFound.Invoke(this, new UserEventArgs(user));
            }
        }
        public event EventHandler<UserEventArgs> UserLost;
        protected void OnUserLost(ZigTrackedUser user)
        {
            if (null != user)
            {
                UserLost.Invoke(this, new UserEventArgs(user));
            }
        }

        public event EventHandler<EventArgs<ZigInput>> Update;
        protected void OnUpdate()
        {
            if (null != Update)
            {
                Update.Invoke(this, new EventArgs<ZigInput>(this));
            }
        }


        public Dictionary<int, ZigTrackedUser> TrackedUsers
        {
            get
            {
                return trackedUsers;
            }
        }

        void HandleReaderNewUsersFrame(List<ZigInputUser> users)
        {
            // get rid of old users
            List<int> idsToRemove = new List<int>(trackedUsers.Keys);
            foreach (ZigInputUser user in users)
            {
                idsToRemove.Remove(user.Id);
            }
            foreach (int id in idsToRemove)
            {
                ZigTrackedUser user = trackedUsers[id];
                trackedUsers.Remove(id);                
                OnUserLost(user);
            }

            // add new & update existing users
            foreach (ZigInputUser user in users)
            {
                if (!trackedUsers.ContainsKey(user.Id))
                {
                    ZigTrackedUser trackedUser = new ZigTrackedUser(user);
                    trackedUsers.Add(user.Id, trackedUser);                    
                    OnUserFound(trackedUser);
                }
                else
                {
                    trackedUsers[user.Id].Update(user);
                }
            }

            OnUpdate();
        }
    }
}
