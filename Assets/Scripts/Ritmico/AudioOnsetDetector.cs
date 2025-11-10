// Archivo: AudioOnsetDetector.cs
using System.Collections.Generic;
using UnityEngine;

public static class AudioOnsetDetector
{
    public static void DetectOnsets(float[] samples, int sampleRate,
        out List<float> onsetTimes, out List<float> onsetEnergies)
    {
        onsetTimes = new List<float>();
        onsetEnergies = new List<float>();

        int hop = 1024; // tamaño chunk
        int fftSize = 1024;

        List<float[]> mags = new List<float[]>();

        // dividir en ventanas
        for (int i = 0; i + fftSize < samples.Length; i += hop)
        {
            float[] buf = new float[fftSize];
            System.Array.Copy(samples, i, buf, 0, fftSize);
            AudioUtils.ApplyHann(buf);
            mags.Add(AudioUtils.RealFFTMag(buf));
        }

        float[] flux = AudioUtils.SpectralFlux(mags);
        float[] thr = AudioUtils.AdaptiveThreshold(flux, 16);

        List<int> peaks = AudioUtils.PickPeaks(flux, thr);

        foreach (int p in peaks)
        {
            float t = (p * hop) / (float)sampleRate;
            onsetTimes.Add(t);
            onsetEnergies.Add(flux[p]);
        }
    }
}
