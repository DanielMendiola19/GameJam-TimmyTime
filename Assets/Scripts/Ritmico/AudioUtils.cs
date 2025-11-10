// Archivo: AudioUtils.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AudioUtils
{
    // Aplica ventana Hann in-place
    public static void ApplyHann(float[] buffer)
    {
        int n = buffer.Length;
        for (int i = 0; i < n; i++)
        {
            float w = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (n - 1)));
            buffer[i] *= w;
        }
    }

    // Real FFT magnitudes (simple Cooley-Tukey radix-2). Input: real signal length N (power of two).
    // Returns magnitudes length N/2.
    public static float[] RealFFTMag(float[] buffer)
    {
        int n = buffer.Length;
        // prepare complex arrays
        Complex[] c = new Complex[n];
        for (int i = 0; i < n; i++) c[i] = new Complex(buffer[i], 0);
        FFT(c);
        int half = n / 2;
        float[] mag = new float[half];
        for (int i = 0; i < half; i++) mag[i] = (float)Math.Sqrt(c[i].Real * c[i].Real + c[i].Imag * c[i].Imag);
        return mag;
    }

    // Spectral flux between consecutive spectra (sum of positive increases)
    public static float[] SpectralFlux(List<float[]> mags)
    {
        int m = mags.Count;
        float[] flux = new float[m];
        for (int i = 1; i < m; i++)
        {
            float sum = 0f;
            var a = mags[i - 1];
            var b = mags[i];
            int len = Mathf.Min(a.Length, b.Length);
            for (int k = 0; k < len; k++)
            {
                float diff = b[k] - a[k];
                if (diff > 0f) sum += diff;
            }
            flux[i] = sum;
        }
        return flux;
    }

    // Median filter (simple)
    public static float[] MedianFilter(float[] data, int radius)
    {
        int n = data.Length;
        float[] outp = new float[n];
        for (int i = 0; i < n; i++)
        {
            List<float> win = new List<float>();
            for (int j = Mathf.Max(0, i - radius); j <= Mathf.Min(n - 1, i + radius); j++) win.Add(data[j]);
            win.Sort();
            outp[i] = win[win.Count / 2];
        }
        return outp;
    }

    // Adaptive threshold: median filter scaled
    public static float[] AdaptiveThreshold(float[] flux, int medianWindowFrames)
    {
        int n = flux.Length;
        float[] thresh = new float[n];
        int r = medianWindowFrames / 2;
        for (int i = 0; i < n; i++)
        {
            int s = Mathf.Max(0, i - r);
            int e = Mathf.Min(n - 1, i + r);
            float[] buf = new float[e - s + 1];
            for (int k = s; k <= e; k++) buf[k - s] = flux[k];
            Array.Sort(buf);
            float med = buf[buf.Length / 2];
            thresh[i] = med * 0.7f; // multiplicador fijo, puedes exponer como param
        }
        return thresh;
    }

    // Peak picking: local maxima above threshold
    public static List<int> PickPeaks(float[] flux, float[] threshold)
    {
        List<int> peaks = new List<int>();
        for (int i = 1; i < flux.Length - 1; i++)
        {
            if (flux[i] > threshold[i] && flux[i] > flux[i - 1] && flux[i] >= flux[i + 1])
            {
                peaks.Add(i);
            }
        }
        return peaks;
    }

    // ----- FFT impl (complex) -----
    public struct Complex { public double Real, Imag; public Complex(double r, double i) { Real = r; Imag = i; } }
    public static void FFT(Complex[] buffer)
    {
        int n = buffer.Length;
        int bits = (int)Math.Log(n, 2);
        // bit reversal
        for (int j = 1, i = 0; j < n; j++)
        {
            int bit = n >> 1;
            for (; (i & bit) != 0; bit >>= 1) i ^= bit;
            i ^= bit;
            if (j < i) { var tmp = buffer[j]; buffer[j] = buffer[i]; buffer[i] = tmp; }
        }
        // Cooley-Tukey
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2.0 * Math.PI / len;
            double wlen_r = Math.Cos(ang);
            double wlen_i = Math.Sin(ang);
            for (int i = 0; i < n; i += len)
            {
                double wr = 1.0, wi = 0.0;
                for (int j = 0; j < len / 2; j++)
                {
                    int u = i + j;
                    int v = i + j + len / 2;
                    double vr = buffer[v].Real * wr - buffer[v].Imag * wi;
                    double vi = buffer[v].Real * wi + buffer[v].Imag * wr;
                    double ur = buffer[u].Real;
                    double ui = buffer[u].Imag;
                    buffer[u].Real = ur + vr;
                    buffer[u].Imag = ui + vi;
                    buffer[v].Real = ur - vr;
                    buffer[v].Imag = ui - vi;
                    double nextWr = wr * wlen_r - wi * wlen_i;
                    double nextWi = wr * wlen_i + wi * wlen_r;
                    wr = nextWr; wi = nextWi;
                }
            }
        }
    }
}