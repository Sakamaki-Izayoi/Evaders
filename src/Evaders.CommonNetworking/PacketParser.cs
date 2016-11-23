namespace Evaders.CommonNetworking
{
    using System;
    using System.Net.Sockets;
    using System.Text;
    using Core.Utility;
    using Microsoft.Extensions.Logging;

    public class PacketParser
    {
        public event Action<string> OnReceivedJson;
        public event Action<SocketError> OnReceivingFailed;
        private readonly StringBuilder _jsonBuilder = new StringBuilder();
        private readonly Encoding _jsonEncoding;

        private readonly ILogger _logger;
        private int _builderByteLength;
        private uint? _waitingMsgLength;

        public PacketParser(ILogger logger, Encoding jsonEncoding)
        {
            _logger = logger;
            _jsonEncoding = jsonEncoding;
        }

        public void Continue(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            if (socketAsyncEventArgs.BytesTransferred < 5 && _jsonBuilder.Length == 0)
            {
                if (socketAsyncEventArgs.BytesTransferred == 4)
                {
                    _waitingMsgLength = BitConverter.ToUInt32(socketAsyncEventArgs.Buffer, socketAsyncEventArgs.Offset);
                    return;
                }

                _logger.LogDebug("Received empty/wrong message: " + socketAsyncEventArgs.SocketError); // anti flood
                OnReceivingFailed?.Invoke(socketAsyncEventArgs.SocketError);
                return;
            }

            var currentOffset = socketAsyncEventArgs.Offset;

            while (socketAsyncEventArgs.BytesTransferred - (currentOffset - socketAsyncEventArgs.Offset) > 5)
            {
                if (_waitingMsgLength == null)
                {
                    _waitingMsgLength = BitConverter.ToUInt32(socketAsyncEventArgs.Buffer, currentOffset);
                    currentOffset += sizeof (uint);
                }

                var count = (int) Math.Min(socketAsyncEventArgs.BytesTransferred - (currentOffset - socketAsyncEventArgs.Offset), _waitingMsgLength.Value - _builderByteLength);
                var str = _jsonEncoding.GetString(socketAsyncEventArgs.Buffer, currentOffset, count);
                _jsonBuilder.Append(str);

                _builderByteLength += count;
                currentOffset += count;

                if (_waitingMsgLength == _builderByteLength)
                    try
                    {
                        OnReceivedJson?.Invoke(_jsonBuilder.ToString());
                    }
                    finally
                    {
                        _builderByteLength = 0;
                        _waitingMsgLength = null;
                        _jsonBuilder.Clear();
                    }
            }
        }
    }
}