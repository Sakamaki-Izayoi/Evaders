namespace Evaders.CommonNetworking
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using Microsoft.Extensions.Logging;

    public abstract class PacketParser<T> where T : Packet
    {
        public event Action<SocketError> OnReceivingFailed;
        public event Action<T> OnReceived;
        private readonly MemoryStream _packetBuilder = new MemoryStream();

        protected readonly ILogger Logger;
        private int _builderByteLength;
        private uint? _waitingMsgLength;

        protected PacketParser(ILogger logger)
        {
            Logger = logger;
        }

        protected abstract void RunTask(Action task);
        public abstract T ToPacket(MemoryStream stream);
        public abstract byte[] FromPacket<TSource>(TSource packet) where TSource : Packet;

        public virtual void Continue(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            lock (_packetBuilder)
            {
                if ((socketAsyncEventArgs.BytesTransferred < 5) && (_packetBuilder.Length == 0))
                {
                    if (socketAsyncEventArgs.BytesTransferred == 4)
                    {
                        _waitingMsgLength = BitConverter.ToUInt32(socketAsyncEventArgs.Buffer, socketAsyncEventArgs.Offset);
                        return;
                    }

                    Logger.LogDebug("Received empty/wrong message: " + socketAsyncEventArgs.SocketError); // anti flood
                    RunTask(() => OnReceivingFailed?.Invoke(socketAsyncEventArgs.SocketError));
                    return;
                }

                var currentOffset = socketAsyncEventArgs.Offset;

                while (socketAsyncEventArgs.BytesTransferred - (currentOffset - socketAsyncEventArgs.Offset) > 5)
                {
                    if (_waitingMsgLength == null)
                    {
                        _waitingMsgLength = BitConverter.ToUInt32(socketAsyncEventArgs.Buffer, currentOffset);
                        currentOffset += sizeof(uint);
                    }

                    var count = (int) Math.Min(socketAsyncEventArgs.BytesTransferred - (currentOffset - socketAsyncEventArgs.Offset), _waitingMsgLength.Value - _builderByteLength);

                    _packetBuilder.Write(socketAsyncEventArgs.Buffer, currentOffset, count);

                    _builderByteLength += count;
                    currentOffset += count;

                    if (_waitingMsgLength == _builderByteLength)
                        try
                        {
                            var packet = ToPacket(_packetBuilder);
                            RunTask(() => OnReceived?.Invoke(packet));
                        }
                        finally
                        {
                            _builderByteLength = 0;
                            _waitingMsgLength = null;
                            _packetBuilder.SetLength(0);
                        }
                }
            }
        }
    }
}