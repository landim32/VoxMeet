using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoxMeet.Services;

public class AudioService : IDisposable
{
    private IWaveIn? _capture;
    private MemoryStream _rawPcmBuffer = new();
    private readonly object _lock = new();
    private readonly WaveFormat _targetFormat = new(16000, 16, 1);

    public event Action<byte[]>? AudioChunkReady;

    public int BufferSeconds { get; set; } = 5;

    public static List<(string Id, string Name)> GetOutputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(d => (d.ID, d.FriendlyName))
            .ToList();
    }

    public static List<(string Id, string Name)> GetInputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => (d.ID, d.FriendlyName))
            .ToList();
    }

    public void Start(string mode, string? deviceId)
    {
        Stop();

        if (mode == "loopback")
        {
            if (deviceId != null)
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDevice(deviceId);
                _capture = new WasapiLoopbackCapture(device);
            }
            else
            {
                _capture = new WasapiLoopbackCapture();
            }
        }
        else
        {
            if (deviceId != null)
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDevice(deviceId);
                _capture = new WasapiCapture(device);
            }
            else
            {
                _capture = new WasapiCapture();
            }
        }

        lock (_lock)
        {
            _rawPcmBuffer = new MemoryStream();
        }

        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
        _capture.StartRecording();
    }

    public void Stop()
    {
        if (_capture != null)
        {
            _capture.StopRecording();
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            _capture.Dispose();
            _capture = null;
        }

        FlushBuffer();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0 || _capture == null)
            return;

        var capturedFormat = _capture.WaveFormat;
        byte[] resampled = ResampleAudio(e.Buffer, e.BytesRecorded, capturedFormat);

        lock (_lock)
        {
            _rawPcmBuffer.Write(resampled, 0, resampled.Length);

            if (_rawPcmBuffer.Length >= _targetFormat.AverageBytesPerSecond * BufferSeconds)
            {
                var pcmBytes = _rawPcmBuffer.ToArray();
                _rawPcmBuffer = new MemoryStream();

                var wavBytes = BuildWav(pcmBytes);
                AudioChunkReady?.Invoke(wavBytes);
            }
        }
    }

    private byte[] ResampleAudio(byte[] buffer, int bytesRecorded, WaveFormat sourceFormat)
    {
        using var sourceStream = new RawSourceWaveStream(
            new MemoryStream(buffer, 0, bytesRecorded), sourceFormat);

        var sampleProvider = sourceStream.ToSampleProvider();

        // Convert to mono first if needed
        ISampleProvider monoProvider;
        if (sampleProvider.WaveFormat.Channels > 2)
        {
            // For surround sound, use MultiplexingSampleProvider to mix down to mono
            var mono = new MultiplexingSampleProvider(new[] { sampleProvider }, 1);
            for (int ch = 0; ch < sampleProvider.WaveFormat.Channels; ch++)
            {
                mono.ConnectInputToOutput(ch, 0);
            }
            monoProvider = mono;
        }
        else if (sampleProvider.WaveFormat.Channels == 2)
        {
            monoProvider = new StereoToMonoSampleProvider(sampleProvider)
            {
                LeftVolume = 0.5f,
                RightVolume = 0.5f
            };
        }
        else
        {
            monoProvider = sampleProvider;
        }

        // Resample to 16kHz
        var resampler = new WdlResamplingSampleProvider(monoProvider, 16000);

        // Convert to 16-bit PCM
        var waveProvider16 = resampler.ToWaveProvider16();
        using var outputStream = new MemoryStream();
        var readBuffer = new byte[4096];
        int read;
        while ((read = waveProvider16.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            outputStream.Write(readBuffer, 0, read);
        }

        return outputStream.ToArray();
    }

    private byte[] BuildWav(byte[] pcmData)
    {
        using var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(ms, _targetFormat))
        {
            writer.Write(pcmData, 0, pcmData.Length);
        }
        return ms.ToArray();
    }

    private void FlushBuffer()
    {
        byte[]? pcmBytes = null;

        lock (_lock)
        {
            if (_rawPcmBuffer.Length > 0)
            {
                pcmBytes = _rawPcmBuffer.ToArray();
                _rawPcmBuffer = new MemoryStream();
            }
        }

        if (pcmBytes != null && pcmBytes.Length > 0)
        {
            var wavBytes = BuildWav(pcmBytes);
            AudioChunkReady?.Invoke(wavBytes);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        // Could log e.Exception if non-null
    }

    public void Dispose()
    {
        Stop();
    }
}
