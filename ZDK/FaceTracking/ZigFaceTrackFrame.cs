namespace Zigfu.FaceTracking
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class represent face tracking results for a frame
    /// </summary>
    public sealed class ZigFaceTrackFrame : IDisposable, ICloneable
    {
        bool _disposed;
        FTResult _faceTrackingResultPtr;
        WeakReference _parentFaceTracker;


        #region Init and Destroy

        internal ZigFaceTrackFrame(FTResult faceTrackResultPtr, ZigFaceTracker parentTracker)
        {
            if (faceTrackResultPtr == null)
            {
                throw new InvalidOperationException("Cannot associate with a null native frame pointer");
            }

            _faceTrackingResultPtr = faceTrackResultPtr;
            _parentFaceTracker = new WeakReference(parentTracker, false);
        }

        private ZigFaceTrackFrame()
        {
        }

        /// <summary>
        /// Creates a deep copy clone. Copies all data from this instance to another instance of FaceTrackFrame. 
        /// Both instances must be created by the same face tracker instance.
        /// </summary>
        /// <returns>
        /// The clone.
        /// </returns>
        public object Clone()
        {
            this.CheckPtrAndThrow();
            var faceTracker = this._parentFaceTracker.Target as ZigFaceTracker;
            if (faceTracker == null)
            {
                throw new ObjectDisposedException("FaceTracker", "Underlying face object has been garbage collected. Cannot clone.");
            }

            ZigFaceTrackFrame faceTrackFrame;
            try
            {
                faceTrackFrame = faceTracker.CreateResult();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, "Failed to create face tracking frame.", e));
            }

            try
            {
                this._faceTrackingResultPtr.CopyTo(faceTrackFrame.ResultPtr);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, "Failed to clone the source face tracking frame."), e);
            }

            return faceTrackFrame;
        }


        ~ZigFaceTrackFrame()
        {
            this.InternalDispose();
        }

        /// <summary>
        /// Disposes this instance and clears the native resources allocated
        /// </summary>
        public void Dispose()
        {
            this.InternalDispose();
            GC.SuppressFinalize(this);
        }

        void InternalDispose()
        {
            if (!this._disposed)
            {
                if (this._faceTrackingResultPtr != null)
                {
                    Marshal.FinalReleaseComObject(this._faceTrackingResultPtr);
                    this._faceTrackingResultPtr = null;
                }

                // do not dispose parentFaceTracker
                this._parentFaceTracker = null;

                this._disposed = true;
            }
        }

        #endregion


        /// <summary>
        /// Face rectangle in video frame coordinates
        /// </summary>
        public Rect FaceRect
        {
            get
            {
                this.CheckPtrAndThrow();
                Rect faceRect;
                this._faceTrackingResultPtr.GetFaceRect(out faceRect);
                return faceRect;
            }
        }

        /// <summary>
        /// Rotation around X, Y, Z axes
        /// </summary>
        public Vector3DF Rotation
        {
            get
            {
                this.CheckPtrAndThrow();
                float scale;
                Vector3DF rotationXyz;
                Vector3DF translationXyz;
                this._faceTrackingResultPtr.Get3DPose(out scale, out rotationXyz, out translationXyz);
                return rotationXyz;
            }
        }

        /// <summary>
        /// Returns a flag if the tracking was successful or not on last tracking call
        /// </summary>
        public bool TrackSuccessful
        {
            get
            {
                return this.Status == ErrorCode.Success;
            }
        }

        /// <summary>
        /// Translation in X, Y, Z axes
        /// </summary>
        public Vector3DF Translation
        {
            get
            {
                this.CheckPtrAndThrow();
                float scale;
                Vector3DF rotationXYZ;
                Vector3DF translationXYZ;
                this._faceTrackingResultPtr.Get3DPose(out scale, out rotationXYZ, out translationXYZ);
                return translationXYZ;
            }
        }


        internal FTResult ResultPtr
        {
            get
            {
                return this._faceTrackingResultPtr;
            }
        }

        /// <summary>
        /// Returns face scale where 1.0 scale means that it is equal in size 
        /// to the loaded 3D model (in the model space)
        /// </summary>
        internal float Scale
        {
            get
            {
                this.CheckPtrAndThrow();
                float scale;
                Vector3DF rotationXyz;
                Vector3DF translationXyz;
                this._faceTrackingResultPtr.Get3DPose(out scale, out rotationXyz, out translationXyz);
                return scale;
            }
        }

        /// <summary>
        /// Error code associated with the frame if the tracking failed
        /// </summary>
        internal ErrorCode Status
        {
            get
            {
                this.CheckPtrAndThrow();
                return (ErrorCode)this._faceTrackingResultPtr.GetStatus();
            }
        }


        /// <summary>
        /// Returns the 3D face model vertices transformed by the passed Shape Units, Animation Units, scale stretch, rotation and translation
        /// </summary>
        /// <returns>
        /// Returns 3D shape
        /// </returns>
        public EnumIndexableCollection<FeaturePoint, Vector3DF> Get3DShape()
        {
            var faceTracker = this._parentFaceTracker.Target as ZigFaceTracker;
            if (faceTracker == null)
            {
                throw new ObjectDisposedException("FaceTracker", "Underlying face object has been garbage collected. Cannot copy.");
            }

            return new EnumIndexableCollection<FeaturePoint, Vector3DF>(faceTracker.FaceModel.Get3DShape(this));
        }

        /// <summary>
        /// Returns Animation Units (AUs) coefficients. These coefficients represent deformations 
        /// of the 3D mask caused by the moving parts of the face (mouth, eyebrows, etc). Use the 
        /// AnimationUnit enum to index these co-efficients
        /// </summary>
        /// <returns>
        /// The get animation unit coefficients.
        /// </returns>
        public EnumIndexableCollection<AnimationUnit, float> GetAnimationUnitCoefficients()
        {
            this.CheckPtrAndThrow();
            IntPtr animUnitCoeffPtr;
            uint pointsCount;
            this._faceTrackingResultPtr.GetAUCoefficients(out animUnitCoeffPtr, out pointsCount);
            float[] animUnitCoeff = null;
            if (pointsCount > 0)
            {
                animUnitCoeff = new float[pointsCount];
                Marshal.Copy(animUnitCoeffPtr, animUnitCoeff, 0, animUnitCoeff.Length);
            }

            return new EnumIndexableCollection<AnimationUnit, float>(animUnitCoeff);
        }

        /// <summary>
        /// Returns the 3D face model vertices transformed by the passed Shape Units, Animation Units, scale stretch, rotation and translation and
        /// projected to the video frame
        /// </summary>
        /// <returns>
        /// Returns projected 3D shape
        /// </returns>
        public EnumIndexableCollection<FeaturePoint, PointF> GetProjected3DShape()
        {
            var faceTracker = _parentFaceTracker.Target as ZigFaceTracker;
            if (faceTracker == null)
            {
                throw new ObjectDisposedException("FaceTracker", "Underlying face object has been garbage collected. Cannot copy.");
            }

            return
                new EnumIndexableCollection<FeaturePoint, PointF>(
                    faceTracker.FaceModel.GetProjected3DShape(ZigFaceTracker.DefaultZoomFactor, Point.Empty, this));
        }


        public FaceTriangle[] GetTriangles()
        {
            var faceTracker = this._parentFaceTracker.Target as ZigFaceTracker;
            if (faceTracker == null)
            {
                throw new ObjectDisposedException("FaceTracker", "Underlying face object has been garbage collected. Cannot copy.");
            }

            return faceTracker.FaceModel.GetTriangles();
        }

        void CheckPtrAndThrow()
        {
            if (this._faceTrackingResultPtr == null)
            {
                throw new InvalidOperationException("Native frame pointer in invalid state.");
            }
        }
    }
}