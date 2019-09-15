using System;

namespace OpusDotNet
{
    /// <summary>
    /// Provides audio encoding with Opus.
    /// </summary>
    public class OpusEncoder : IDisposable
    {
        private readonly SafeEncoderHandle _handle;

        private int _bitrate;

        /// <summary>
        /// Initializes a new <see cref="OpusEncoder"/> instance, with the specified intended application, 48000 Hz sample rate and 2 channels.
        /// </summary>
        /// <param name="application">The intended application.</param>
        public OpusEncoder(Application application) : this(application, 48000, 2)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpusEncoder"/> instance, with the specified intended application, sample rate and channels.
        /// </summary>
        /// <param name="application">The intended application.</param>
        /// <param name="sampleRate">The sample rate in the input audio, 48000, 24000, 16000, 12000 or 8000 Hz.</param>
        /// <param name="channels">The channels in the input audio, mono or stereo.</param>
        public OpusEncoder(Application application, int sampleRate, int channels)
        {
            if (!Enum.IsDefined(typeof(Application), application))
            {
                throw new ArgumentException("Value is not defined in the enumeration.", nameof(application));
            }

            switch (sampleRate)
            {
                case 8000:
                case 12000:
                case 16000:
                case 24000:
                case 48000:
                    break;
                default:
                    throw new ArgumentException("Value must be one of the following: 8000, 12000, 16000, 24000 or 48000.", nameof(sampleRate));
            }

            if (channels < 1 || channels > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), "Value must be between 1 and 2.");
            }

            Application = application;
            SampleRate = sampleRate;
            Channels = channels;
            Bitrate = 128000;

            _handle = API.opus_encoder_create(sampleRate, channels, (int)application, out int error);
            API.ThrowIfError(error);

            // Setting to -1 (OPUS_BITRATE_MAX) enables bitrate to be regulated by the output buffer length.
            int result = API.opus_encoder_ctl(_handle, (int)Control.SetBitrate, -1);
            API.ThrowIfError(result);
        }

        /// <summary>
        /// Gets the intended application.
        /// </summary>
        public Application Application { get; }

        /// <summary>
        /// Gets the sample rate, 48000, 24000, 16000, 12000 or 8000 Hz.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Gets the channels, mono or stereo.
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// Gets or sets the bitrate, 8000 - 512000 bps.
        /// </summary>
        public int Bitrate
        {
            get => _bitrate;
            set
            {
                if (value < 8000 || value > 512000)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 8000 and 512000.");
                }

                _bitrate = value;
            }
        }

        /// <summary>
        /// Gets or sets whether VBR (variable bitrate) is enabled.
        /// </summary>
        public bool VBR
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.GetVBR, out int value);
                API.ThrowIfError(result);

                return value == 1;
            }
            set
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.SetVBR, value ? 1 : 0);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets the maximum bandpass.
        /// </summary>
        public Bandwidth MaxBandwidth
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.GetMaxBandwidth, out int value);
                API.ThrowIfError(result);

                return (Bandwidth)value;
            }
            set
            {
                if (!Enum.IsDefined(typeof(Bandwidth), value))
                {
                    throw new ArgumentException("Value is not defined in the enumeration.", nameof(value));
                }

                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.SetMaxBandwidth, (int)value);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets the computational complexity, 0 - 10. Decreasing this will decrease CPU time, at the expense of quality.
        /// </summary>
        public int Complexity
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.GetComplexity, out int value);
                API.ThrowIfError(result);

                return value;
            }
            set
            {
                if (value < 0 || value > 10)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 10.");
                }

                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.SetComplexity, value);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets whether to use FEC (forward error correction). You need to adjust <see cref="ExpectedPacketLoss"/>
        /// before FEC takes effect.
        /// </summary>
        public bool FEC
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.GetInbandFEC, out int value);
                API.ThrowIfError(result);

                return value == 1;
            }
            set
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.SetInbandFEC, value ? 1 : 0);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets the expected packet loss percentage when using FEC (forward error correction). Increasing this will
        /// improve quality under loss, at the expense of quality in the absence of packet loss.
        /// </summary>
        public int ExpectedPacketLoss
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.GetPacketLossPerc, out int value);
                API.ThrowIfError(result);

                return value;
            }
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 100.");
                }

                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.SetPacketLossPerc, value);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets whether to use DTX (discontinuous transmission). When enabled the encoder will produce
        /// packets with a length of 2 bytes or less during periods of no voice activity.
        /// </summary>
        public bool DTX
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.GetDTX, out int value);
                API.ThrowIfError(result);

                return value == 1;
            }
            set
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.SetDTX, value ? 1 : 0);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets the forced mono/stereo mode.
        /// </summary>
        public ForceChannels ForceChannels
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.GetForceChannels, out int value);
                API.ThrowIfError(result);

                return (ForceChannels)value;
            }
            set
            {
                if (!Enum.IsDefined(typeof(ForceChannels), value))
                {
                    throw new ArgumentException("Value is not defined in the enumeration.", nameof(value));
                }

                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)Control.SetForceChannels, (int)value);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Encodes an Opus frame, the frame size must be one of the following: 2.5, 5, 10, 20, 40 or 60 ms.
        /// </summary>
        /// <param name="pcmBytes">The Opus frame.</param>
        /// <param name="length">The maximum number of bytes to use from <paramref name="pcmBytes"/>.</param>
        /// <param name="encodedLength">The length of the encoded audio.</param>
        /// <returns>A byte array containing the encoded audio.</returns>
        public unsafe byte[] Encode(byte[] pcmBytes, int length, out int encodedLength)
        {
            if (pcmBytes == null)
            {
                throw new ArgumentNullException(nameof(pcmBytes));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Value cannot be negative.");
            }

            if (pcmBytes.Length < length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Value cannot be greater than the length of {nameof(pcmBytes)}.");
            }

            double frameSize = API.GetFrameSize(length, SampleRate, Channels);

            switch (frameSize)
            {
                case 2.5:
                case 5:
                case 10:
                case 20:
                case 40:
                case 60:
                    break;
                default:
                    throw new ArgumentException("The frame size must be one of the following: 2.5, 5, 10, 20, 40 or 60.", nameof(length));
            }

            ThrowIfDisposed();

            byte[] opusBytes = new byte[(int)(frameSize * Bitrate / 8 / 1000)];
            int result;

            int samples = API.GetSampleCount(frameSize, SampleRate);

            fixed (byte* input = pcmBytes)
            fixed (byte* output = opusBytes)
            {
                var inputPtr = (IntPtr)input;
                var outputPtr = (IntPtr)output;
                result = API.opus_encode(_handle, inputPtr, samples, outputPtr, opusBytes.Length);
            }

            API.ThrowIfError(result);

            encodedLength = result;
            return opusBytes;
        }

        /// <summary>
        /// Releases all resources used by the current instance.
        /// </summary>
        public void Dispose()
        {
            _handle?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_handle.IsClosed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
