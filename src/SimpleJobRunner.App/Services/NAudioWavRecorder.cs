using System.IO;
using NAudio.Wave;
using SimpleJobRunner.Core;

namespace SimpleJobRunner.App.Services;

public sealed class NAudioWavRecorder : IAudioRecorder, IDisposable
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _path;

    public bool IsRecording => _waveIn is not null;

    public Task<string> StartAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (IsRecording)
        {
            throw new InvalidOperationException("Recording is already active.");
        }

        Directory.CreateDirectory(AppPaths.DefaultAudioTempRoot);
        _path = Path.Combine(AppPaths.DefaultAudioTempRoot, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1)
        };
        _writer = new WaveFileWriter(_path, _waveIn.WaveFormat);
        _waveIn.DataAvailable += (_, args) => _writer?.Write(args.Buffer, 0, args.BytesRecorded);
        _waveIn.RecordingStopped += (_, _) => DisposeRecordingObjects();
        _waveIn.StartRecording();
        return Task.FromResult(_path);
    }

    public Task<string> StopAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsRecording || string.IsNullOrWhiteSpace(_path))
        {
            throw new InvalidOperationException("Recording is not active.");
        }

        var path = _path;
        _waveIn?.StopRecording();
        DisposeRecordingObjects();
        return Task.FromResult(path);
    }

    public void Dispose()
    {
        DisposeRecordingObjects();
    }

    private void DisposeRecordingObjects()
    {
        _waveIn?.Dispose();
        _writer?.Dispose();
        _waveIn = null;
        _writer = null;
    }
}
