namespace Evaders.CommonNetworking
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    ///     Queues results of async operations, and fires events in the thread that calls Work() later
    /// </summary>
    public class EasyTaskSocket : IDisposable
    {
        [Flags]
        public enum SocketTasks
        {
            None = 0,
            Accept = 1,
            Receive = 2,
            ReceiveFrom = 4,
            ReceiveMessageFrom = 8
        }

        public event EventHandler<SocketAsyncEventArgs> OnAccepted;
        public event EventHandler<SocketAsyncEventArgs> OnReceived;
        public event EventHandler<SocketAsyncEventArgs> OnReceivedFrom;
        public event EventHandler<SocketAsyncEventArgs> OnReceivedMessageFrom;
        public event EventHandler<SocketAsyncEventArgs> OnSent;
        public event EventHandler<SocketAsyncEventArgs> OnSentTo;

        public bool Connected => Socket.Connected;
        public bool IsBound => Socket.IsBound;

        public uint BufferSize
        {
            get
            {
                return _bufferSize;
            }
            set
            {
                _bufferSize = value;

                byte[] oldBuffer;
                while (!_bufferPool.IsEmpty)
                    _bufferPool.TryTake(out oldBuffer);
            }
        }

        public bool WasStarted { get; private set; }
        public bool Stopped { get; private set; }

        private readonly ConcurrentBag<byte[]> _bufferPool = new ConcurrentBag<byte[]>();

        public readonly Socket Socket;
        private uint _bufferSize = ushort.MaxValue;

        public EasyTaskSocket(Socket socket)
        {
            Socket = socket;
        }

        public void Dispose()
        {
            StopJobs();
            Socket.Dispose();
        }

        public bool StartJobs(SocketTasks tasks)
        {
            if (Stopped || WasStarted)
                return false;
            WasStarted = true;

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
        ///     Stops all activity from this wrapper class. The underlying socket is still valid
        /// </summary>
        public void StopJobs()
        {
            Stopped = true;
        }


        /// <summary>
        ///     You may give the buffer back if you do not need it anymore
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
            if (Stopped)
                throw new InvalidOperationException($"{nameof(EasySocket)} was stopped");
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
            if (Stopped)
                throw new InvalidOperationException($"{nameof(EasySocket)} was stopped");
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
            Task.Run(() => OnSent?.Invoke(this, socketAsyncEventArgs));
        }

        private void OnSendToComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Task.Run(() => OnSentTo?.Invoke(this, socketAsyncEventArgs));
        }

        private void OnAcceptedComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Task.Run(() => OnAccepted?.Invoke(this, socketAsyncEventArgs));
            SetupAccept();
        }

        private void OnReceivedComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Task.Run(() => OnReceived?.Invoke(this, socketAsyncEventArgs));
            SetupReceive();
        }

        private void OnReceivedFromComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Task.Run(() => OnReceivedFrom?.Invoke(this, socketAsyncEventArgs));
            SetupReceiveFrom();
        }

        private void OnReceivedMessageFromComplete(object sender, SocketAsyncEventArgs socketAsyncEventArgs)
        {
            Task.Run(() => OnReceivedMessageFrom?.Invoke(this, socketAsyncEventArgs));
            SetupReceiveMessageFrom();
        }

        private void SetupAccept()
        {
            if (Stopped)
                return;
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += OnAcceptedComplete;
            Socket.AcceptAsync(socketAsyncEventArgs);
        }

        private void SetupReceive()
        {
            if (Stopped)
                return;
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += OnReceivedComplete;
            var buffer = RentBuffer();
            socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
            Socket.ReceiveAsync(socketAsyncEventArgs);
        }

        private void SetupReceiveFrom()
        {
            if (Stopped)
                return;
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += OnReceivedFromComplete;
            var buffer = RentBuffer();
            socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
            Socket.ReceiveFromAsync(socketAsyncEventArgs);
        }

        private void SetupReceiveMessageFrom()
        {
            if (Stopped)
                return;
            var socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += OnReceivedMessageFromComplete;
            var buffer = RentBuffer();
            socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
            Socket.ReceiveMessageFromAsync(socketAsyncEventArgs);
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

        public static bool operator ==(EasyTaskSocket @this, EasyTaskSocket other)
        {
            return @this?.Socket == other?.Socket;
        }

        public static bool operator !=(EasyTaskSocket @this, EasyTaskSocket other)
        {
            return @this?.Socket != other?.Socket;
        }
    }
}