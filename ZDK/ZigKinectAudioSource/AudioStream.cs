﻿using System;
using System.IO;
using System.Diagnostics;

namespace ZDK.ZigKinectAudioSource
{
    class AudioStream : MemoryStream
    {
        const UInt32 MaxCapacity = 32000 * 60 * 10;     // (10 minutes of 32 kb/s audio)


        DateTime _timeOfLastSuccessfulRead = DateTime.Now;
        public TimeSpan ReadStaleThreshold { get; private set; }

        public override bool CanRead    { get { return true; } }
        public override bool CanWrite   { get { return false; } }
        public override bool CanSeek    { get { return false; } }


        #region Unsupported Overrides

        public override long Length { get { return -1; } }
        public override long Position {
            get { throw CreateNotSupportedException("seeking"); }
            set { throw CreateNotSupportedException("seeking"); } 
        }

        public override void SetLength(long value) {
            throw CreateNotSupportedException("both writing and seeking");
        }
        public override void Write(byte[] buffer, int offset, int count) {
            throw CreateNotSupportedException("writing");
        }
        public override long Seek(long offset, SeekOrigin origin) {
            throw CreateNotSupportedException("seeking");
        }

        NotSupportedException CreateNotSupportedException(String typeStr)
        {
            return new NotSupportedException("AudioStream does not support " + typeStr + ".");
        }

        #endregion


        #region Init

        public AudioStream() : this(TimeSpan.MaxValue) { }
        public AudioStream(TimeSpan readStaleThreshold)
        {
            ReadStaleThreshold = readStaleThreshold;
            _timeOfLastSuccessfulRead = DateTime.Now;
        }

        #endregion


        public override int Read(byte[] buffer, int offset, int count)
        {
            int readCount = base.Read(buffer, offset, count);
            _timeOfLastSuccessfulRead = DateTime.Now;
            return readCount;
        }


        public void AppendBytes(byte[] buffer, int offset, uint count)
        {
            EnforceReadStaleThreshold();

            long storedPosition = base.Position;

            // Write new bytes to end, then return to storedPosition
            base.Position = base.Length;
            base.Write(buffer, 0, (int)count);
            base.Position = storedPosition;

            EnforceMaxCapacity();
        }

        void EnforceReadStaleThreshold()
        {
            TimeSpan timeSinceLastSuccessfulRead = DateTime.Now.Subtract(_timeOfLastSuccessfulRead);
            if (timeSinceLastSuccessfulRead.CompareTo(ReadStaleThreshold) >= 0)
            {
                Reset();
            }
        }

        void EnforceMaxCapacity()
        {
            // If we've reached MaxCapacity, then discard the oldest half of the buffer, and transfer the remaining bytes to start of stream
            if (base.Length > MaxCapacity)
            {
                long cutoffPoint = (long)(0.5f * MaxCapacity);
                DiscardOldSamples(cutoffPoint);
            }
        }

        // Summary:
        //      Overwrites the bytes from index [0 to cutoffPoint] with the bytes from [cutoffPoint to Length],
        //       and adjusts Position and Length accordingly. 
        //      If Position was before cutoffPoint, it will be set to 0.
        void DiscardOldSamples(long cutoffPoint)
        {
            long newLength   = (long)Math.Max(0, base.Length   - (int)cutoffPoint);
            long newPosition = (long)Math.Max(0, base.Position - (int)cutoffPoint);
            Byte[] tempBuffer = new Byte[newLength];

            base.Position = cutoffPoint;
            base.Read(tempBuffer, 0, (int)newLength);

            base.Position = 0;
            base.Write(tempBuffer, 0, (int)newLength);
            base.SetLength(newLength);

            base.Position = newPosition;
        }

        void Reset()
        {
            base.Position = 0;
            base.SetLength(0);
            _timeOfLastSuccessfulRead = DateTime.Now;
        }
    }
}