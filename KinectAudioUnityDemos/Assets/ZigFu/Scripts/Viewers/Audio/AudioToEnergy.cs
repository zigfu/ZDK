using System;

// Summary:
//      AudioToEnergy encapsulates a circular buffer for storing the "energy" of an audio signal
//      Energy values range between 0 and 1.
//
public class AudioToEnergy {

    const string ClassName = "AudioToEnergy";

    const float EnergyNoiseFloor = 0.2f;            // Bottom portion of computed energy signal that will be discarded as noise.
    const int AudioSamplesPerEnergySample = 40;     // Number of audio samples represented by each element in _energyBuffer

    public static bool verbose = true;


    readonly float[] _energyBuffer;                 // A circular buffer for storing energy values
    int _energyIndex = 0;                           // Index of next element available in audio energy buffer.


    public AudioToEnergy(uint energyBufferSize)
    {
        _energyBuffer = new float[energyBufferSize];
    }

    public void ConvertAudioToEnergy(byte[] audioBuffer, int numSamplesToProcess, ref float[] energyBuffer, out uint startIndex)
    {
        if (numSamplesToProcess > audioBuffer.Length)
        {
            numSamplesToProcess = audioBuffer.Length;
        }

        double accumulatedSquareSum = 0;           // Sum of squares of audio samples being accumulated to compute the next energy value.
        int accumulatedSampleCount = 0;            // Number of audio samples accumulated so far to compute the next energy value.

        startIndex = (uint)_energyIndex;

        for (int i = 0; i < numSamplesToProcess; i += 2)
        {
            // Compute the sum of squares of audio samples that will get accumulated into a single energy value.
            short audioSample = BitConverter.ToInt16(audioBuffer, i);
            accumulatedSquareSum += audioSample * audioSample;
            ++accumulatedSampleCount;

            if (accumulatedSampleCount < AudioSamplesPerEnergySample)
            {
                continue;
            }

            // Each energy value will represent the logarithm of the mean of the sum of squares of a group of audio samples.
            double meanSquare = accumulatedSquareSum / AudioSamplesPerEnergySample;
            double amplitude = Math.Log(meanSquare) / Math.Log(int.MaxValue);

            // Truncate portion of signal below noise floor
            float amplitudeAboveNoise = (float)Math.Max(0, amplitude - EnergyNoiseFloor);

            // Renormalize signal above noise floor to [0,1] range.
            _energyBuffer[_energyIndex] = amplitudeAboveNoise / (1 - EnergyNoiseFloor);
            _energyIndex = (_energyIndex + 1) % _energyBuffer.Length;

            accumulatedSquareSum = 0;
            accumulatedSampleCount = 0;
        }

        energyBuffer = _energyBuffer;
    }
}
