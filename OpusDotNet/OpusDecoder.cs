using System;

namespace OpusDotNet
{
    /// <summary>
    /// Provides audio decoding with Opus.
    /// </summary>
    public class OpusDecoder : IDisposable
    {
        private readonly SafeDecoderHandle _handle;
        // Number of samples in the frame size, per channel.
        private readonly int _samples;
        private readonly int _pcmLength;

        private bool _fec;

        /// <summary>
        /// Initializes a new <see cref="OpusDecoder"/> instance, with 48000 Hz sample rate and 2 channels.
        /// </summary>
        public OpusDecoder() : this(60, 48000, 2, false)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpusDecoder"/> instance, with the specified frame size, 48000 Hz sample rate and 2 channels.
        /// </summary>
        /// <param name="frameSize">The frame size used when encoding, 2.5, 5, 10, 20, 40 or 60 ms.</param>
        [Obsolete("This constructor was used for the old decode method and is deprecated, please use the new decode method instead.")]
        public OpusDecoder(double frameSize) : this(frameSize, 48000, 2, true)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpusDecoder"/> instance, with the specified sample rate and channels.
        /// </summary>
        /// <param name="sampleRate">The sample rate to decode to, 48000, 24000, 16000, 12000 or 8000 Hz.</param>
        /// <param name="channels">The channels to decode to, mono or stereo.</param>
        public OpusDecoder(int sampleRate, int channels) : this(60, sampleRate, channels, false)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="OpusDecoder"/> instance, with the specified frame size, sample rate and channels.
        /// </summary>
        /// <param name="frameSize">The frame size used when encoding, 2.5, 5, 10, 20, 40 or 60 ms.</param>
        /// <param name="sampleRate">The sample rate to decode to, 48000, 24000, 16000, 12000 or 8000 Hz.</param>
        /// <param name="channels">The channels to decode to, mono or stereo.</param>
        [Obsolete("This constructor was used for the old decode method and is deprecated, please use the new decode method instead.")]
        public OpusDecoder(double frameSize, int sampleRate, int channels) : this(frameSize, sampleRate, channels, true)
        {
        }

        private OpusDecoder(double frameSize, int sampleRate, int channels, bool frameSizeWasSpecified)
        {
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
                    throw new ArgumentException("Value must be one of the following: 2.5, 5, 10, 20, 40 or 60.", nameof(frameSize));
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

            if (frameSizeWasSpecified)
            {
                FrameSize = frameSize;
            }

            SampleRate = sampleRate;
            Channels = channels;

            _samples = API.GetSampleCount(frameSize, sampleRate);
            _pcmLength = API.GetPCMLength(_samples, channels);

            _handle = API.opus_decoder_create(sampleRate, channels, out int error);
            API.ThrowIfError(error);
        }

        /// <summary>
        /// Gets the frame size, or null if not specified when constructing the current instance.
        /// </summary>
        [Obsolete("This property was used for the old decode method and is deprecated, please use the new decode method instead.")]
        public double? FrameSize { get; }

        /// <summary>
        /// Gets the sample rate, 48000, 24000, 16000, 12000 or 8000 Hz.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Gets the channels, mono or stereo.
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// Gets or sets whether to use FEC (forward error correction). NOTE: This can only be set if <see cref="FrameSize"/> is set,
        /// and only works if the encoder also uses FEC. You also need to indicate when a packet has been lost
        /// (by calling <see cref="Decode(byte[], int, out int)"/> with null and -1 as the arguments).
        /// </summary>
        [Obsolete("This property was used for the old decode method and is deprecated, please use the new decode method instead.")]
        public bool FEC
        {
            get => _fec;
            set
            {
                if (FrameSize == null)
                {
                    throw new InvalidOperationException("A frame size has to be specified in the constructor for FEC to work.");
                }

                _fec = value;
            }
        }

        /// <summary>
        /// Decodes an Opus packet, or indicates packet loss (if <see cref="FEC"/> is enabled).
        /// </summary>
        /// <param name="opusBytes">The Opus packet, or null to indicate packet loss (if <see cref="FEC"/> is enabled).</param>
        /// <param name="length">The maximum number of bytes to use from <paramref name="opusBytes"/>, or -1 to indicate packet loss
        /// (if <see cref="FEC"/> is enabled).</param>
        /// <param name="decodedLength">The length of the decoded audio.</param>
        /// <returns>A byte array containing the decoded audio.</returns>
        [Obsolete("This method is deprecated, please use the new decode method instead.")]
        public unsafe byte[] Decode(byte[] opusBytes, int length, out int decodedLength)
        {
            if (opusBytes == null && !FEC)
            {
                throw new ArgumentNullException(nameof(opusBytes), "Value cannot be null when FEC is disabled.");
            }

            if (length < 0 && (!FEC || opusBytes != null))
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Value cannot be negative when {nameof(opusBytes)} is not null or FEC is disabled.");
            }

            if (opusBytes != null && opusBytes.Length < length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Value cannot be greater than the length of {nameof(opusBytes)}.");
            }

            ThrowIfDisposed();

            byte[] pcmBytes = new byte[_pcmLength];
            int result;

            fixed (byte* input = opusBytes)
            fixed (byte* output = pcmBytes)
            {
                var inputPtr = (IntPtr)input;
                var outputPtr = (IntPtr)output;

                if (opusBytes != null)
                {
                    result = API.opus_decode(_handle, inputPtr, length, outputPtr, _samples, 0);
                }
                else
                {
                    // If forward error correction is enabled, this will indicate a packet loss.
                    result = API.opus_decode(_handle, IntPtr.Zero, 0, outputPtr, _samples, FEC ? 1 : 0);
                }
            }

            API.ThrowIfError(result);

            decodedLength = result * Channels * 2;
            return pcmBytes;
        }

        /// <summary>
        /// Decodes an Opus packet or any FEC (forward error correction) data.
        /// </summary>
        /// <param name="opusBytes">The Opus packet, or null to indicate packet loss.</param>
        /// <param name="opusLength">The maximum number of bytes to read from <paramref name="opusBytes"/>, or -1 to indicate packet loss.</param>
        /// <param name="pcmBytes">The buffer that the decoded audio will be stored in.</param>
        /// <param name="pcmLength">The maximum number of bytes to write to <paramref name="pcmBytes"/>.
        /// When using FEC (forward error correction) this must be a valid frame size that matches the duration of the missing audio.</param>
        /// <returns>The number of bytes written to <paramref name="pcmBytes"/>.</returns>
        public unsafe int Decode(byte[] opusBytes, int opusLength, byte[] pcmBytes, int pcmLength)
        {
            if (opusLength < 0 && opusBytes != null)
            {
                throw new ArgumentOutOfRangeException(nameof(opusLength), $"Value cannot be negative when {nameof(opusBytes)} is not null.");
            }

            if (opusBytes != null && opusBytes.Length < opusLength)
            {
                throw new ArgumentOutOfRangeException(nameof(opusLength), $"Value cannot be greater than the length of {nameof(opusBytes)}.");
            }

            if (pcmBytes == null)
            {
                throw new ArgumentNullException(nameof(pcmBytes));
            }

            if (pcmLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pcmLength), "Value cannot be negative.");
            }

            if (pcmBytes.Length < pcmLength)
            {
                throw new ArgumentOutOfRangeException(nameof(pcmLength), $"Value cannot be greater than the length of {nameof(pcmBytes)}.");
            }

            double frameSize = API.GetFrameSize(pcmLength, SampleRate, Channels);

            if (opusBytes == null)
            {
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
                        throw new ArgumentException("When using FEC the frame size must be one of the following: 2.5, 5, 10, 20, 40 or 60.", nameof(pcmLength));
                }
            }

            ThrowIfDisposed();

            int result;
            int samples = API.GetSampleCount(frameSize, SampleRate);

            fixed (byte* input = opusBytes)
            fixed (byte* output = pcmBytes)
            {
                var inputPtr = (IntPtr)input;
                var outputPtr = (IntPtr)output;

                if (opusBytes != null)
                {
                    result = API.opus_decode(_handle, inputPtr, opusLength, outputPtr, samples, 0);
                }
                else
                {
                    // If forward error correction is enabled, this will indicate a packet loss.
                    result = API.opus_decode(_handle, IntPtr.Zero, 0, outputPtr, samples, 1);
                }
            }

            API.ThrowIfError(result);
            return API.GetPCMLength(result, Channels);
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
