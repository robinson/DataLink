using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DataLink.Driver.S7
{
    /// <summary>
    /// Socket client for async socket connection.
    /// </summary>
    internal class SocketClient : IDisposable
    {
        private Socket _socket = null;
        private int _receiveTimeout = TIMEOUT_MILLISECONDS;
        private int _sendTimeout = TIMEOUT_MILLISECONDS;

        private static ManualResetEvent _clientDone = new ManualResetEvent(false);

        private const int TIMEOUT_MILLISECONDS = 5000;//set receive timeout
        public bool Connected { get; private set; }

        public SocketClient(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _socket = new Socket(addressFamily, socketType, protocolType);
        }
        /// <summary>
        /// Connect
        /// </summary>
        /// <param name="server"></param>
        public void Connect(IPEndPoint server)
        {
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

            socketEventArg.RemoteEndPoint = server;

            socketEventArg.Completed += delegate (object s, SocketAsyncEventArgs e)
            {
                if (e.SocketError == SocketError.Success)
                {
                    Connected = true;
                }
                else
                {
                    throw new SocketException((int)e.SocketError);
                }

                _clientDone.Set();
            };

            _clientDone.Reset();

            _socket.ConnectAsync(socketEventArg);

            _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
        }
        /// <summary>
        /// Set receive timeout.
        /// </summary>
        /// <param name="milis"></param>
        public void SetReceiveTimeout(int milis)
        {
            _receiveTimeout = milis;
        }
        /// <summary>
        /// Set send timeout.
        /// </summary>
        /// <param name="milis"></param>
        public void SetSendTimeout(int milis)
        {
            _sendTimeout = milis;
        }
        /// <summary>
        /// Send the package
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public int Send(byte[] buffer, int start, int size)
        {
            var response = 0;

            if (_socket != null)
            {
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                socketEventArg.UserToken = null;

                socketEventArg.Completed += delegate (object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        response = e.BytesTransferred;
                    }
                    else
                    {
                        throw new SocketException((int)e.SocketError);
                    }

                    _clientDone.Set();
                };

                socketEventArg.SetBuffer(buffer, start, size);

                _clientDone.Reset();

                _socket.SendAsync(socketEventArg);

                _clientDone.WaitOne(_sendTimeout);
            }
            else
            {
                throw new SocketException((int)SocketError.NotInitialized);
            }

            return response;
        }
        /// <summary>
        /// Receive the package
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public int Receive(byte[] buffer, int start, int size)
        {
            var response = 0;

            if (_socket != null)
            {
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                socketEventArg.SetBuffer(buffer, start, size);

                socketEventArg.Completed += delegate (object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        response = e.BytesTransferred;
                    }
                    else
                    {
                        throw new SocketException((int)e.SocketError);
                    }

                    _clientDone.Set();
                };

                _clientDone.Reset();

                _socket.ReceiveAsync(socketEventArg);

                _clientDone.WaitOne(_receiveTimeout);
            }
            else
            {
                throw new SocketException((int)SocketError.NotInitialized);
            }

            return response;
        }
        /// <summary>
        /// Close the connection, 
        /// </summary>
        public void Close()
        {
            Connected = false;

            if (_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
        }

        public void Dispose()
        {
            if (Connected)
            {
                this.Close();
            }

        }



    }
}
