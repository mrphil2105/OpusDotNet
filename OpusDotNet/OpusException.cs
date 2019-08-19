using System;

namespace OpusDotNet
{
    /// <summary>
    /// The exception that is thrown when an Opus error occurs.
    /// </summary>
    public class OpusException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="OpusException"/> instance, with the specified Opus error code.
        /// </summary>
        /// <param name="errorCode">The Opus error code.</param>
        public OpusException(int errorCode) : base(GetMessage((EOpusError)errorCode))
        {
            Error = (EOpusError)errorCode;
        }

        /// <summary>
        /// The Opus error.
        /// </summary>
        public EOpusError Error { get; }

        private static string GetMessage(EOpusError error)
        {
            switch (error)
            {
                case EOpusError.BadArg:
                    return "One or more invalid/out of range arguments.";
                case EOpusError.BufferTooSmall:
                    return "Not enough bytes allocated in the buffer.";
                case EOpusError.InternalError:
                    return "An internal error was detected.";
                case EOpusError.InvalidPacket:
                    return "The compressed data passed is corrupted.";
                case EOpusError.Unimplemented:
                    return "Invalid/unsupported request number.";
                case EOpusError.InvalidState:
                    return "An encoder or decoder structure is invalid or already freed.";
                case EOpusError.AllocFail:
                    return "Memory allocation has failed.";
                default:
                    return "An unknown error has occurred.";
            }
        }
    }
}
