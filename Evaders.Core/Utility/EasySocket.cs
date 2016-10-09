using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvadersServer
{
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Queues results of async operations, and fires events in the thread that calls Work() later
    /// </summary>
    public class EasySocket : IDisposable
    {
        [Flags]
        public enum SocketTasks
        {
            None = 0, Accept = 1, Receive = 2, ReceiveFrom = 4, ReceiveMessageFrom = 8
        }
        
        public bool Connected => Socket.Connected;
        public bool IsBound => Socket.IsBound;
        public readonly Socket Socket;
        private uint _bufferSize = ushort.MaxValue;

        public uint BufferSize
        {
            get { return _bufferSize; }
            set
            {
                _bufferSize = value;

                byte[] oldBuffer;
                while (!_bufferPool.IsEmpty)
                {
                    _bufferPool.TryTake(out oldBuffer);
                }
            }
        }


        private readonly ConcurrentQueue<SocketAsyncEventArgs> _acceptedSockets = new ConcurrentQueue<SocketAsyncEventArgs>();
        private readonly ConcurrentQueue<SocketAsyncEventArgs> _receivedData = new ConcurrentQueue<SocketAsyncEventArgs>();
        private readonly ConcurrentQueue<SocketAsyncEventArgs> _receivedFromData = new ConcurrentQueue<SocketAsyncEventArgs>();
        private readonly ConcurrentQueue<SocketAsyncEventArgs> _receivedMessageFromData = new ConcurrentQueue<SocketAsyncEventArgs>();
        private readonly ConcurrentQueue<SocketAsyncEventArgs> _sendComplete = new ConcurrentQueue<SocketAsyncEventArgs>();
        private readonly ConcurrentQueue<SocketAsyncEventArgs> _sendToComplete = new ConcurrentQueue<SocketAsyncEventArgs>();
        private readonly ConcurrentBag<byte[]> _bufferPool = new ConcurrentBag<byte[]>(); 

        public event EventHandler<SocketAsyncEventArgs> OnAccepted;
        public event EventHandler<SocketAsyncEventArgs> OnReceived;
        public event EventHandler<SocketAsyncEventArgs> OnReceivedFrom;
        public event EventHandler<SocketAsyncEventArgs> OnReceivedMessageFrom;
        public event EventHandler<SocketAsyncEventArgs> OnSent;
        public event EventHandler<SocketAsyncEventArgs> OnSentTo;
        public bool Started { get; private set; }
        private bool _stopped;

        public bool HasWork => !(_acceptedSockets.IsEmpty && _receivedData.IsEmpty && _receivedFromData.IsEmpty && _receivedMessageFromData.IsEmpty);

        public EasySocket(Socket socket)
        {
            Socket = socket;
        }

        public bool StartJobs(SocketTasks tasks)
        {
            if (_stopped || Started) return false;
            Started = true;

            if (tasks.HasFlag(SocketTasks.Accept))
                SetupAccept();
            if (tasks.HasFlag(SocketTasks.Receive))
                SetupReceive();
            if (tasks.HasFlag(SocketTasks.ReceiveFrom))
                SetupReceiveFrom();
            if (tasks.HasFlag(SocketTasks.ReceiveMessageFrom))
                SetupReceiveMessageFrom();

            return true;
        }

        /// <summary>
        /// Stops all activity from this wrapper class. The underlying socket is still valid
        /// </summary>
        public void StopJobs()
        {
            _stopped = true;
        }

        /// <summary>
        /// Will fire all events according to the queued messages
        /// </summary>
        public void Work()
        {
            if (_stopped) throw new InvalidOperationException($"{nameof(EasySocket)} was stopped");
            SocketAsyncEventArgs result;

            while (!_acceptedSockets.IsEmpty)
                if (_acceptedSockets.TryDequeue(out result))
                    OnAccepted?.Invoke(Socket, result);

            while (!_receivedData.IsEmpty)
                if (_receivedData.TryDequeue(out result))
                    OnReceived?.Invoke(Socket, result);

            while (!_receivedFromData.IsEmpty)
                if (_receivedFromData.TryDequeue(out result))
                    OnReceivedFrom?.Invoke(Socket, result);

            while (!_receivedMessageFromData.IsEmpty)
                if (_receivedMessageFromData.TryDequeue(out result))
                    OnReceivedMessageFrom?.Invoke(Socket, result);

            while (!_sendComplete.IsEmpty)
                if (_sendComplete.TryDequeue(out result))
                    OnSent?.Invoke(Socket, result);

            while (!_sendToComplete.IsEmpty)
                if (_sendToComplete.TryDequeue(out result))
                    OnSentTo?.Invoke(Socket, result);
        }

        /// <summary>
        /// You may give the buffer back if you do not need it anymore
        /// </summary>
        /// <param name="buffer"></param>
        public void GiveBack(byte[] buffer)
        {
            _bufferPool.Add(buffer);
        }

        private byte[] RentBuffer()
        {
            while (!_bufferPool.IsEmpty)
            {
                byte[] result;
                if (_bufferPool.TryTake(out result))
                    return result;
            }
            return new byte[BufferSize];
        }

        public void SendAsync(byte[] buffer, int offset, int count)
        {
            if (_stopped) throw new InvalidOperationException($"{nameof(EasySocket)} was stopped");
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, offset, count);
            args.Completed += OnSendComplete;
            Socket.SendAsync(args);
        }

        public void SendAsync(byte[] buffer)
        {
            SendAsync(buffer, 0, buffer.Length);
        }

        public void SendToAsync(byte[] buffer, EndPoint endPoint, int offset, int count)
        {
            if (_stopped) throw new InvalidOperationException($"{nameof(EasySocket)} was stopped");
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, offset, count);
            args.RemoteEndPoint = endPoint;
            args.Completed += OnSendToComplete;
            Socket.SendToAsync(args);
        }

        public void SendToAsync(byte[] buffer, EndPoint endPoint)
        {
            SendToAsync(buffer, endPoint, 0, buffer.Length);
        }

        private void OnSendComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            _sendComplete.Enqueue(socketAsyncEventArgs);
        }

        private void OnSendToComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            _sendToComplete.Enqueue(socketAsyncEventArgs);
        }

        private void OnAcceptedComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            _acceptedSockets.Enqueue(socketAsyncEventArgs);
            SetupAccept();
        }

        private void OnReceivedComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            _receivedData.Enqueue(socketAsyncEventArgs);
            SetupReceive();
        }

        private void OnReceivedFromComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            _receivedFromData.Enqueue(socketAsyncEventArgs);
            SetupReceiveFrom();
        }

        private void OnReceivedMessageFromComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            _receivedMessageFromData.Enqueue(socketAsyncEventArgs);
            SetupReceiveMessageFrom();
        }

        private void SetupAccept()
        {
            if (_stopped) return;
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += OnAcceptedComplete;
            Socket.AcceptAsync(socketAsyncEventArgs);
        }

        private void SetupReceive()
        {
            if (_stopped) return;
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += OnReceivedComplete;
            var buffer = RentBuffer();
            socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
            Socket.ReceiveAsync(socketAsyncEventArgs);
        }

        private void SetupReceiveFrom()
        {
            if (_stopped) return;
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += OnReceivedFromComplete;
            var buffer = RentBuffer();
            socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
            Socket.ReceiveFromAsync(socketAsyncEventArgs);
        }

        private void SetupReceiveMessageFrom()
        {
            if (_stopped) return;
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += OnReceivedComplete;
            var buffer = RentBuffer();
            socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
            Socket.ReceiveMessageFromAsync(socketAsyncEventArgs);
        }

        public void Dispose()
        {
            StopJobs();
            Socket.Dispose();
        }

        public override string ToString()
        {
            return $"{nameof(EasySocket)} wrapping {Socket}";
        }

        public override int GetHashCode()
        {
            return Socket.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Socket.Equals(obj);
        }

        public static bool operator ==(EasySocket @this, EasySocket other)
        {
            return @this?.Socket == other?.Socket;
        }

        public static bool operator !=(EasySocket @this, EasySocket other)
        {
            return @this?.Socket != other?.Socket;
        }
    }
}
