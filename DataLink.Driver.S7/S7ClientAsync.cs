using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DataLink.Driver.S7
{
    /// <summary>
    /// S7 Async client, with high performance connection.
    /// </summary>
    public class S7ClientAsync : IDisposable
    {
        // WordLength
        private const byte S7WLByte = 0x02;
        private const byte S7WLCounter = 0x1C;
        private const byte S7WLTimer = 0x1D;
        // Error Codes
        public const int errTcpConnectionFailed = 0x0001;
        public const int errTcpDataSend = 0x0002;
        public const int errTcpDataRecv = 0x0003;
        public const int errTcpDataRecvTout = 0x0004;
        public const int errTcpConnectionReset = 0x0005;
        public const int errISOInvalidPDU = 0x0006;
        public const int errISOConnectionFailed = 0x0007;
        public const int errISONegotiatingPDU = 0x0008;
        public const int errS7InvalidPDU = 0x0009;
        public const int errS7DataRead = 0x000A;
        public const int errS7DataWrite = 0x000B;
        public const int errS7BufferTooSmall = 0x000C;
        public const int errS7FunctionError = 0x000D;
        public const int errS7InvalidParams = 0x000E;

        // Public fields
        public Boolean Connected = false;
        public int LastError = 0;
        public int RecvTimeout = 2000;
        private SocketClient _SocketClient;

        // Privates
        private const int ISOTCP = 102; // ISOTCP Port
        private const int MinPduSize = 16;
        private const int DefaultPduSizeRequested = 480;
        private const int IsoHSize = 7; // TPKT+COTP Header Size
        private const int MaxPduSize = DefaultPduSizeRequested + IsoHSize;


        //private Socket TcpSocket;
        private byte[] PDU = new byte[2048];

        private string IpAddr;

        private byte LocalTSAP_HI;
        private byte LocalTSAP_LO;
        private byte RemoteTSAP_HI;
        private byte RemoteTSAP_LO;
        private byte LastPDUType;

        private short ConnType = S7.PG;
        private int _PDULength = 0;

        // Telegrams
        // ISO Connection Request telegram (contains also ISO Header and COTP Header)
        private static byte[] ISO_CR = {
		// TPKT (RFC1006 Header)
		   0x03, // RFC 1006 ID (3) 
		   0x00, // Reserved, always 0
		   0x00, // High part of packet lenght (entire frame, payload and TPDU included)
		   0x16, // Low part of packet lenght (entire frame, payload and TPDU included)
			// COTP (ISO 8073 Header)
		   0x11, // PDU Size Length
		   0xE0, // CR - Connection Request ID
		   0x00, // Dst Reference HI
		   0x00, // Dst Reference LO
		   0x00, // Src Reference HI
		   0x01, // Src Reference LO
		   0x00, // Class + Options Flags
		   0xC0, // PDU Max Length ID
		   0x01, // PDU Max Length HI
		   0x0A, // PDU Max Length LO
		   0xC1, // Src TSAP Identifier
		   0x02, // Src TSAP Length (2 bytes)
		   0x01, // Src TSAP HI (will be overwritten)
		   0x00, // Src TSAP LO (will be overwritten)
		   0xC2, // Dst TSAP Identifier
		   0x02, // Dst TSAP Length (2 bytes)
		   0x01, // Dst TSAP HI (will be overwritten)
		   0x02  // Dst TSAP LO (will be overwritten)
		};

        // S7 PDU Negotiation Telegram (contains also ISO Header and COTP Header)
        private static byte[] S7_PN = {
           0x03,0x00,0x00,0x19,
           0x02,0xf0,0x80, // TPKT + COTP (see above for info)
		   0x32,0x01,0x00,0x00,
           0x04,0x00,0x00,0x08,
           0x00,0x00,0xf0,0x00,
           0x00,0x01,0x00,0x01,
           0x00,0x1e // PDU Length Requested = HI-LO 480 bytes
		};

        // S7 Read/Write Request Header (contains also ISO Header and COTP Header)
        private static byte[] S7_RW = { // 31-35 bytes
		   0x03, 0x00,
           0x00, 0x1f,  // Telegram Length (Data Size + 31 or 35)
		   0x02, 0xf0, 0x80, // COTP (see above for info)
		   0x32,             // S7 Protocol ID 
		   0x01,             // Job Type
		   0x00, 0x00,  // Redundancy identification
		   0x05, 0x00,  // PDU Reference
		   0x00, 0x0e,  // Parameters Length
		   0x00, 0x00,  // Data Length = Size(bytes) + 4      
		   0x04,             // Function 4 Read Var, 5 Write Var  
		   0x01,             // Items count
		   0x12,             // Var spec.
		   0x0a,             // Length of remaining bytes
		   0x10,             // Syntax ID 
			S7WLByte,               // Transport Size                        
		   0x00, 0x00,  // Num Elements                          
		   0x00, 0x00,  // DB Number (if any, else 0)            
		   0x84,             // Area Type                            
		   0x00, 0x00,(byte)0x00, // Area Offset                     
			// WR area
		   0x00,             // Reserved 
		   0x04,             // Transport size
		   0x00, 0x00,  // Data Length * 8 (if not timer or counter) 
		};
        private static int Size_RD = 31;
        private static int Size_WR = 35;

        // S7 Get Block Info Request Header (contains also ISO Header and COTP Header)
        private static byte[] S7_BI = {
           0x03,0x00,0x00,0x25,
           0x02,0xf0,0x80,0x32,
           0x07,0x00,0x00,0x05,
           0x00,0x00,0x08,0x00,
           0x0c,0x00,0x01,0x12,
           0x04,0x11,0x43,0x03,
           0x00,0xff,0x09,0x00,
           0x08,0x30,
           0x41, // Block Type
		   0x30,0x30,0x30,0x30,0x30, // ASCII Block Number
		   0x41
        };

        // SZL First telegram request   
        private static byte[] S7_SZL_FIRST = {
           0x03,0x00,0x00,0x21,
           0x02,0xf0,0x80,0x32,
           0x07,0x00,0x00,
           0x05,0x00, // Sequence out
		   0x00,0x08,0x00,
           0x08,0x00,0x01,0x12,
           0x04,0x11,0x44,0x01,
           0x00,0xff,0x09,0x00,
           0x04,
           0x00,0x00, // ID (29)
		   0x00,0x00  // Index (31)
		};

        // SZL Next telegram request 
        private static byte[] S7_SZL_NEXT = {
           0x03,0x00,0x00,0x21,
           0x02,0xf0,0x80,0x32,
           0x07,0x00,0x00,0x06,
           0x00,0x00,0x0c,0x00,
           0x04,0x00,0x01,0x12,
           0x08,0x12,0x44,0x01,
           0x01, // Sequence
		   0x00,0x00,0x00,0x00,
           0x0a,0x00,0x00,0x00
        };

        // Get Date/Time request
        private static byte[] S7_GET_DT = {
           0x03,0x00,0x00,0x1d,
           0x02,0xf0,0x80,0x32,
           0x07,0x00,0x00,0x38,
           0x00,0x00,0x08,0x00,
           0x04,0x00,0x01,0x12,
           0x04,0x11,0x47,0x01,
           0x00,0x0a,0x00,0x00,
           0x00
        };

        // Set Date/Time command
        private static byte[] S7_SET_DT = {
           0x03,0x00,0x00,0x27,
           0x02,0xf0,0x80,0x32,
           0x07,0x00,0x00,0x89,
           0x03,0x00,0x08,0x00,
           0x0e,0x00,0x01,0x12,
           0x04,0x11,0x47,0x02,
           0x00,0xff,0x09,0x00,
           0x0a,0x00,0x19, // Hi part of Year
		   0x13, // Lo part of Year
		   0x12, // Month
		   0x06, // Day
		   0x17, // Hour
		   0x37, // Min
		   0x13, // Sec
		   0x00,0x01 // ms + Day of week   
		};

        // S7 STOP request
        private static byte[] S7_STOP = {
           0x03,0x00,0x00,0x21,
           0x02,0xf0,0x80,0x32,
           0x01,0x00,0x00,0x0e,
           0x00,0x00,0x10,0x00,
           0x00,0x29,0x00,0x00,
           0x00,0x00,0x00,0x09,
           0x50,0x5f,0x50,0x52,
           0x4f,0x47,0x52,0x41,
           0x4d
        };

        // S7 HOT Start request
        private static byte[] S7_HOT_START = {
           0x03,0x00,0x00,0x25,
           0x02,0xf0,0x80,0x32,
           0x01,0x00,0x00,0x0c,
           0x00,0x00,0x14,0x00,
           0x00,0x28,0x00,0x00,
           0x00,0x00,0x00,0x00,
           0xfd,0x00,0x00,0x09,
           0x50,0x5f,0x50,0x52,
           0x4f,0x47,0x52,0x41,
           0x4d
        };

        // S7 COLD Start request
        private static byte[] S7_COLD_START = {
           0x03,0x00,0x00,0x27,
           0x02,0xf0,0x80,0x32,
           0x01,0x00,0x00,0x0f,
           0x00,0x00,0x16,0x00,
           0x00,0x28,0x00,0x00,
           0x00,0x00,0x00,0x00,
           0xfd,0x00,0x02,0x43,
           0x20,0x09,0x50,0x5f,
           0x50,0x52,0x4f,0x47,
           0x52,0x41,0x4d
        };

        // S7 Get PLC Status 
        private static byte[] S7_GET_STAT = {
           0x03,0x00,0x00,0x21,
           0x02,0xf0,0x80,0x32,
           0x07,0x00,0x00,0x2c,
           0x00,0x00,0x08,0x00,
           0x08,0x00,0x01,0x12,
           0x04,0x11,0x44,0x01,
           0x00,0xff,0x09,0x00,
           0x04,0x04,0x24,0x00,
           0x00
        };

        // S7 Set Session Password 
        private static byte[] S7_SET_PWD = {
           0x03,0x00,0x00,0x25,
           0x02,0xf0,0x80,0x32,
           0x07,0x00,0x00,0x27,
           0x00,0x00,0x08,0x00,
           0x0c,0x00,0x01,0x12,
           0x04,0x11,0x45,0x01,
           0x00,0xff,0x09,0x00,
           0x08, 
			// 8 Char Encoded Password
		   0x00,0x00,0x00,0x00,
           0x00,0x00,0x00,0x00
        };

        // S7 Clear Session Password 
        private static byte[] S7_CLR_PWD = {
           0x03,0x00,0x00,0x1d,
           0x02,0xf0,0x80,0x32,
           0x07,0x00,0x00,0x29,
           0x00,0x00,0x08,0x00,
           0x04,0x00,0x01,0x12,
           0x04,0x11,0x45,0x02,
           0x00,0x0a,0x00,0x00,
           0x00
        };

        public S7ClientAsync()
        {
            // Placeholder for future implementations
        }

        public static String ErrorText(int Error)
        {
            switch (Error)
            {
                case errTcpConnectionFailed:
                    return "Tcp Connection failed.";
                case errTcpDataSend:
                    return "Tcp Sending error.";
                case errTcpDataRecv:
                    return "Tcp Receiving error.";
                case errTcpDataRecvTout:
                    return "Data Receiving timeout.";
                case errTcpConnectionReset:
                    return "Connection reset by the peer.";
                case errISOInvalidPDU:
                    return "Invalid ISO PDU received.";
                case errISOConnectionFailed:
                    return "ISO connection refused by the CPU.";
                case errISONegotiatingPDU:
                    return "ISO error negotiating the PDU length.";
                case errS7InvalidPDU:
                    return "Invalid S7 PDU received.";
                case errS7DataRead:
                    return "S7 Error reading data from the CPU.";
                case errS7DataWrite:
                    return "S7 Error writing data to the CPU.";
                case errS7BufferTooSmall:
                    return "The Buffer supplied to the function is too small.";
                case errS7FunctionError:
                    return "S7 function refused by the CPU.";
                case errS7InvalidParams:
                    return "Invalid parameters supplied to the function.";
                default:
                    return "Unknown error : 0x" + Error.ToString("X");
            }
        }
        private static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length != 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
            int port;
            if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }
        private int TcpConnect()
        {
            LastError = 0;
            try
            {
                var hostEndPoint = CreateIPEndPoint(IpAddr + ":" + ISOTCP);

                _SocketClient = new SocketClient(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _SocketClient.Connect(hostEndPoint);
                Connected = _SocketClient.Connected;
            }
            catch (IOException ex)
            {
                LastError = errTcpConnectionFailed;
            }
            return LastError;
        }

        private int RecvPacket(byte[] Buffer, int Start, int Size)
        {
            int BytesRead = 0;
            if (LastError == 0)
            {
                try
                {
                    //lth
                    BytesRead = _SocketClient.Receive(Buffer, Start, Size);
                }
                catch (IOException ex)
                {
                    LastError = errTcpDataRecv;
                }
                if (BytesRead == 0)
                    LastError = errTcpConnectionReset;
            }
            return LastError;
        }

        private void SendPacket(byte[] Buffer, int Len)
        {
            LastError = 0;
            try
            {
                _SocketClient.Send(Buffer, 0, Len);
            }
            catch (IOException ex)
            {
                LastError = errTcpDataSend;
            }
        }
        private void SendPacket(byte[] Buffer)
        {
            SendPacket(Buffer, Buffer.Length);
        }

        private int RecvIsoPacket()
        {
            Boolean Done = false;
            int Size = 0;
            while ((LastError == 0) && !Done)
            {
                // Get TPKT (4 bytes)
                RecvPacket(PDU, 0, 4);
                if (LastError == 0)
                {
                    Size = S7.GetWordAt(PDU, 2);
                    // Check 0 bytes Data Packet (only TPKT+COTP = 7 bytes)
                    if (Size == IsoHSize)
                        RecvPacket(PDU, 4, 3); // Skip remaining 3 bytes and Done is still false
                    else
                    {
                        if ((Size > MaxPduSize) || (Size < MinPduSize))
                            LastError = errISOInvalidPDU;
                        else
                            Done = true; // a valid Length !=7 && >16 && <247
                    }
                }
            }
            if (LastError == 0)
            {
                RecvPacket(PDU, 4, 3); // Skip remaining 3 COTP bytes
                LastPDUType = PDU[5];   // Stores PDU Type, we need it 
                                        // Receives the S7 Payload          
                RecvPacket(PDU, 7, Size - IsoHSize);
            }
            if (LastError == 0)
                return Size;
            else
                return 0;
        }

        private int ISOConnect()
        {
            int Size;
            ISO_CR[16] = LocalTSAP_HI;
            ISO_CR[17] = LocalTSAP_LO;
            ISO_CR[20] = RemoteTSAP_HI;
            ISO_CR[21] = RemoteTSAP_LO;

            // Sends the connection request telegram      
            SendPacket(ISO_CR);
            if (LastError == 0)
            {
                // Gets the reply (if any)
                Size = RecvIsoPacket();
                if (LastError == 0)
                {
                    if (Size == 22)
                    {
                        if (LastPDUType != (byte)0xD0) // 0xD0 = CC Connection confirm
                            LastError = errISOConnectionFailed;
                    }
                    else
                        LastError = errISOInvalidPDU;
                }
            }
            return LastError;
        }

        private int NegotiatePduLength()
        {
            int Length;
            // Set PDU Size Requested
            S7.SetWordAt(S7_PN, 23, DefaultPduSizeRequested);
            // Sends the connection request telegram
            SendPacket(S7_PN);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (LastError == 0)
                {
                    // check S7 Error
                    if ((Length == 27) && (PDU[17] == 0) && (PDU[18] == 0))  // 20 = size of Negotiate Answer
                    {
                        // Get PDU Size Negotiated
                        _PDULength = S7.GetWordAt(PDU, 25);
                        if (_PDULength > 0)
                            return 0;
                        else
                            LastError = errISONegotiatingPDU;
                    }
                    else
                        LastError = errISONegotiatingPDU;
                }
            }
            return LastError;
        }

        public void SetConnectionType(short ConnectionType)
        {
            ConnType = ConnectionType;
        }
        /// <summary>
        /// Connect with 3 stage
        /// </summary>
        /// <returns></returns>
        public int Connect()
        {
            LastError = 0;
            if (!Connected)
            {
                TcpConnect();
                if (LastError == 0) // First stage : Tcp Connection
                {
                    ISOConnect();
                    if (LastError == 0) // Second stage : ISOTcp (ISO 8073) Connection
                    {
                        LastError = NegotiatePduLength(); // Third stage : S7 PDU negotiation
                    }
                }
            }
            Connected = LastError == 0;
            return LastError;
        }

        public void Disconnect()
        {
            if (Connected)
            {
                try
                {
                    //TcpSocket.Close();
                    _SocketClient.Close();
                    _PDULength = 0;
                }
                catch (IOException ex)
                {
                }
                Connected = _SocketClient.Connected;
            }
        }

        public int ConnectTo(String Address, int Rack, int Slot)
        {
            int RemoteTSAP = (ConnType << 8) + (Rack * 0x20) + Slot;
            SetConnectionParams(Address, 0x0100, RemoteTSAP);
            return Connect();
        }

        public int PDULength()
        {
            return _PDULength;
        }

        public void SetConnectionParams(String Address, int LocalTSAP, int RemoteTSAP)
        {
            int LocTSAP = LocalTSAP & 0x0000FFFF;
            int RemTSAP = RemoteTSAP & 0x0000FFFF;
            IpAddr = Address;
            LocalTSAP_HI = (byte)(LocTSAP >> 8);
            LocalTSAP_LO = (byte)(LocTSAP & 0x00FF);
            RemoteTSAP_HI = (byte)(RemTSAP >> 8);
            RemoteTSAP_LO = (byte)(RemTSAP & 0x00FF);
        }

        public int ReadArea(int Area, int DBNumber, int Start, int Amount, byte[] Data)
        {
            int Address;
            int NumElements;
            int MaxElements;
            int TotElements;
            int SizeRequested;
            int Length;
            int Offset = 0;
            int WordSize = 1;

            LastError = 0;

            // If we are addressing Timers or counters the element size is 2
            if ((Area == S7.S7AreaCT) || (Area == S7.S7AreaTM))
                WordSize = 2;

            MaxElements = (_PDULength - 18) / WordSize; // 18 = Reply telegram header
            TotElements = Amount;

            while ((TotElements > 0) && (LastError == 0))
            {
                NumElements = TotElements;
                if (NumElements > MaxElements)
                    NumElements = MaxElements;

                SizeRequested = NumElements * WordSize;

                // Setup the telegram
                Array.Copy(S7_RW, 0, PDU, 0, Size_RD);
                // Set DB Number
                PDU[27] = (byte)Area;
                // Set Area
                if (Area == S7.S7AreaDB)
                    S7.SetWordAt(PDU, 25, DBNumber);

                // Adjusts Start and word length
                if ((Area == S7.S7AreaCT) || (Area == S7.S7AreaTM))
                {
                    Address = Start;
                    if (Area == S7.S7AreaCT)
                        PDU[22] = S7WLCounter;
                    else
                        PDU[22] = S7WLTimer;
                }
                else
                    Address = Start << 3;

                // Num elements
                S7.SetWordAt(PDU, 23, NumElements);

                // Address into the PLC (only 3 bytes)           
                PDU[30] = (byte)(Address & 0x0FF);
                Address = Address >> 8;
                PDU[29] = (byte)(Address & 0x0FF);
                Address = Address >> 8;
                PDU[28] = (byte)(Address & 0x0FF);

                SendPacket(PDU, Size_RD);
                if (LastError == 0)
                {
                    Length = RecvIsoPacket();
                    if (LastError == 0)
                    {
                        if (Length >= 25)
                        {
                            if ((Length - 25 == SizeRequested) && (PDU[21] == (byte)0xFF))
                            {
                                Array.Copy(PDU, 25, Data, Offset, SizeRequested);
                                Offset += SizeRequested;
                            }
                            else
                                LastError = errS7DataRead;
                        }
                        else
                            LastError = errS7InvalidPDU;
                    }
                }

                TotElements -= NumElements;
                Start += NumElements * WordSize;
            }
            return LastError;
        }

        public int WriteArea(int Area, int DBNumber, int Start, int Amount, byte[] Data)
        {
            int Address;
            int NumElements;
            int MaxElements;
            int TotElements;
            int DataSize;
            int IsoSize;
            int Length;
            int Offset = 0;
            int WordSize = 1;

            LastError = 0;

            // If we are addressing Timers or counters the element size is 2
            if ((Area == S7.S7AreaCT) || (Area == S7.S7AreaTM))
                WordSize = 2;

            MaxElements = (_PDULength - 35) / WordSize; // 18 = Reply telegram header
            TotElements = Amount;

            while ((TotElements > 0) && (LastError == 0))
            {
                NumElements = TotElements;
                if (NumElements > MaxElements)
                    NumElements = MaxElements;

                DataSize = NumElements * WordSize;
                IsoSize = Size_WR + DataSize;

                // Setup the telegram
                Array.Copy(S7_RW, 0, PDU, 0, Size_WR);
                // Whole telegram Size
                S7.SetWordAt(PDU, 2, IsoSize);
                // Data Length
                Length = DataSize + 4;
                S7.SetWordAt(PDU, 15, Length);
                // Function
                PDU[17] = (byte)0x05;
                // Set DB Number
                PDU[27] = (byte)Area;
                if (Area == S7.S7AreaDB)
                    S7.SetWordAt(PDU, 25, DBNumber);

                // Adjusts Start and word length
                if ((Area == S7.S7AreaCT) || (Area == S7.S7AreaTM))
                {
                    Address = Start;
                    Length = DataSize;
                    if (Area == S7.S7AreaCT)
                        PDU[22] = S7WLCounter;
                    else
                        PDU[22] = S7WLTimer;
                }
                else
                {
                    Address = Start << 3;
                    Length = DataSize << 3;
                }
                // Num elements
                S7.SetWordAt(PDU, 23, NumElements);
                // Address into the PLC
                PDU[30] = (byte)(Address & 0x0FF);
                Address = Address >> 8;
                PDU[29] = (byte)(Address & 0x0FF);
                Address = Address >> 8;
                PDU[28] = (byte)(Address & 0x0FF);
                // Length
                S7.SetWordAt(PDU, 33, Length);

                // Copies the Data
                Array.Copy(Data, Offset, PDU, 35, DataSize);

                SendPacket(PDU, IsoSize);
                if (LastError == 0)
                {
                    Length = RecvIsoPacket();
                    if (LastError == 0)
                    {
                        if (Length == 22)
                        {
                            if ((S7.GetWordAt(PDU, 17) != 0) || (PDU[21] != (byte)0xFF))
                                LastError = errS7DataWrite;
                        }
                        else
                            LastError = errS7InvalidPDU;
                    }
                }

                Offset += DataSize;
                TotElements -= NumElements;
                Start += NumElements * WordSize;
            }
            return LastError;
        }

        public int GetAgBlockInfo(int BlockType, int BlockNumber, S7BlockInfo Block)
        {
            int Length;
            LastError = 0;
            // Block Type
            S7_BI[30] = (byte)BlockType;
            // Block Number
            S7_BI[31] = (byte)((BlockNumber / 10000) + 0x30);
            BlockNumber = BlockNumber % 10000;
            S7_BI[32] = (byte)((BlockNumber / 1000) + 0x30);
            BlockNumber = BlockNumber % 1000;
            S7_BI[33] = (byte)((BlockNumber / 100) + 0x30);
            BlockNumber = BlockNumber % 100;
            S7_BI[34] = (byte)((BlockNumber / 10) + 0x30);
            BlockNumber = BlockNumber % 10;
            S7_BI[35] = (byte)((BlockNumber / 1) + 0x30);

            SendPacket(S7_BI);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (Length > 32) // the minimum expected
                {
                    if ((S7.GetWordAt(PDU, 27) == 0) && (PDU[29] == (byte)0xFF))
                    {
                        Block.Update(PDU, 42);
                    }
                    else
                        LastError = errS7FunctionError;
                }
                else
                    LastError = errS7InvalidPDU;
            }

            return LastError;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DBNumber">DB Number</param>
        /// <param name="Buffer">Destination buffer</param>
        /// <param name="SizeRead">How many bytes were read</param>
        /// <returns></returns>

        public int DBGet(int DBNumber, byte[] Buffer, IntByRef SizeRead)
        {
            S7BlockInfo Block = new S7BlockInfo();
            // Query the DB Length
            LastError = GetAgBlockInfo(S7.Block_DB, DBNumber, Block);
            if (LastError == 0)
            {
                int SizeToRead = Block.MC7Size();
                // Checks the room
                if (SizeToRead <= Buffer.Length)
                {
                    LastError = ReadArea(S7.S7AreaDB, DBNumber, 0, SizeToRead, Buffer);
                    if (LastError == 0)
                        SizeRead.Value = SizeToRead;
                }
                else
                    LastError = errS7BufferTooSmall;
            }
            return LastError;
        }

        public int ReadSZL(int ID, int Index, S7Szl SZL)
        {
            int Length;
            int DataSZL;
            int Offset = 0;
            Boolean Done = false;
            Boolean First = true;
            byte Seq_in = 0x00;
            int Seq_out = 0x0000;

            LastError = 0;
            SZL.DataSize = 0;
            do
            {
                if (First)
                {
                    S7.SetWordAt(S7_SZL_FIRST, 11, ++Seq_out);
                    S7.SetWordAt(S7_SZL_FIRST, 29, ID);
                    S7.SetWordAt(S7_SZL_FIRST, 31, Index);
                    SendPacket(S7_SZL_FIRST);
                }
                else
                {
                    S7.SetWordAt(S7_SZL_NEXT, 11, ++Seq_out);
                    PDU[24] = (byte)Seq_in;
                    SendPacket(S7_SZL_NEXT);
                }
                if (LastError != 0)
                    return LastError;

                Length = RecvIsoPacket();
                if (LastError == 0)
                {
                    if (First)
                    {
                        if (Length > 32) // the minimum expected
                        {
                            if ((S7.GetWordAt(PDU, 27) == 0) && (PDU[29] == (byte)0xFF))
                            {
                                // Gets Amount of this slice
                                DataSZL = S7.GetWordAt(PDU, 31) - 8; // Skips extra params (ID, Index ...)
                                Done = PDU[26] == 0x00;
                                Seq_in = (byte)PDU[24]; // Slice sequence

                                SZL.LENTHDR = S7.GetWordAt(PDU, 37);
                                SZL.N_DR = S7.GetWordAt(PDU, 39);
                                SZL.Copy(PDU, 41, Offset, DataSZL);
                                Offset += DataSZL;
                                SZL.DataSize += DataSZL;
                            }
                            else
                                LastError = errS7FunctionError;
                        }
                        else
                            LastError = errS7InvalidPDU;
                    }
                    else
                    {
                        if (Length > 32) // the minimum expected
                        {
                            if ((S7.GetWordAt(PDU, 27) == 0) && (PDU[29] == (byte)0xFF))
                            {
                                // Gets Amount of this slice
                                DataSZL = S7.GetWordAt(PDU, 31);
                                Done = PDU[26] == 0x00;
                                Seq_in = (byte)PDU[24]; // Slice sequence
                                SZL.Copy(PDU, 37, Offset, DataSZL);
                                Offset += DataSZL;
                                SZL.DataSize += DataSZL;
                            }
                            else
                                LastError = errS7FunctionError;
                        }
                        else
                            LastError = errS7InvalidPDU;
                    }
                }
                First = false;
            }
            while (!Done && (LastError == 0));

            return LastError;
        }


        public int GetCpuInfo(S7CpuInfo Info)
        {
            S7Szl SZL = new S7Szl(1024);

            LastError = ReadSZL(0x001C, 0x0000, SZL);
            if (LastError == 0)
            {
                Info.Update(SZL.Data, 0);
            }
            return LastError;
        }

        public int GetCpInfo(S7CpInfo Info)
        {
            S7Szl SZL = new S7Szl(1024);

            LastError = ReadSZL(0x0131, 0x0001, SZL);
            if (LastError == 0)
            {
                Info.Update(SZL.Data, 0);
            }
            return LastError;
        }

        public int GetOrderCode(S7OrderCode Code)
        {
            S7Szl SZL = new S7Szl(1024);

            LastError = ReadSZL(0x0011, 0x0000, SZL);
            if (LastError == 0)
            {
                Code.Update(SZL.Data, 0, SZL.DataSize);
            }
            return LastError;
        }

        public int GetPlcDateTime(DateTime DateTime)
        {
            int Length;

            LastError = 0;
            SendPacket(S7_GET_DT);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (Length > 30) // the minimum expected
                {
                    if ((S7.GetWordAt(PDU, 27) == 0) && (PDU[29] == (byte)0xFF))
                    {
                        DateTime = S7.GetDateAt(PDU, 34);
                    }
                    else
                        LastError = errS7FunctionError;
                }
                else
                    LastError = errS7InvalidPDU;
            }

            return LastError;
        }

        public int SetPlcDateTime(DateTime DateTime)
        {
            int Length;

            LastError = 0;
            S7.SetDateAt(S7_SET_DT, 31, DateTime);

            SendPacket(S7_SET_DT);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (Length > 30) // the minimum expected
                {
                    if (S7.GetWordAt(PDU, 27) != 0)
                        LastError = errS7FunctionError;
                }
                else
                    LastError = errS7InvalidPDU;
            }

            return LastError;
        }

        public int SetPlcSystemDateTime()
        {
            return SetPlcDateTime(DateTime.Now);
        }

        public int PlcStop()
        {
            int Length;

            LastError = 0;
            SendPacket(S7_STOP);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (Length > 18) // 18 is the minimum expected
                {
                    if (S7.GetWordAt(PDU, 17) != 0)
                        LastError = errS7FunctionError;
                }
                else
                    LastError = errS7InvalidPDU;
            }
            return LastError;
        }

        public int PlcHotStart()
        {
            int Length;

            LastError = 0;
            SendPacket(S7_HOT_START);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (Length > 18) // the minimum expected
                {
                    if (S7.GetWordAt(PDU, 17) != 0)
                        LastError = errS7FunctionError;
                }
                else
                    LastError = errS7InvalidPDU;
            }
            return LastError;
        }

        public int PlcColdStart()
        {
            int Length;

            LastError = 0;
            SendPacket(S7_COLD_START);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (Length > 18) // the minimum expected
                {
                    if (S7.GetWordAt(PDU, 17) != 0)
                        LastError = errS7FunctionError;
                }
                else
                    LastError = errS7InvalidPDU;
            }
            return LastError;
        }

        public int GetPlcStatus(IntByRef Status)
        {
            int Length;

            LastError = 0;
            SendPacket(S7_GET_STAT);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (Length > 30) // the minimum expected
                {
                    if (S7.GetWordAt(PDU, 27) == 0)
                    {
                        switch (PDU[44])
                        {
                            case S7.S7CpuStatusUnknown:
                            case S7.S7CpuStatusRun:
                            case S7.S7CpuStatusStop:
                                Status.Value = PDU[44];
                                break;
                            default:
                                // Since RUN status is always 0x08 for all CPUs and CPs, STOP status
                                // sometime can be coded as 0x03 (especially for old cpu...)
                                Status.Value = S7.S7CpuStatusStop;
                                break;
                        }
                    }
                    else
                        LastError = errS7FunctionError;
                }
                else
                    LastError = errS7InvalidPDU;
            }
            return LastError;
        }

        public int SetSessionPassword(String Password)
        {
            byte[] pwd = { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            int Length;

            LastError = 0;
            // Adjusts the Password length to 8
            if (Password.Length > 8)
                Password = Password.Substring(0, 8);
            else
            {
                while (Password.Length < 8)
                    Password = Password + " ";
            }

            try
            {
                pwd = Password.GetBytes();//.getBytes("UTF-8");
            }
            catch (Exception ex)//UnsupportedEncodingException
            {
                LastError = errS7InvalidParams;
            }
            if (LastError == 0)
            {
                // Encodes the password
                pwd[0] = (byte)(pwd[0] ^ 0x55);
                pwd[1] = (byte)(pwd[1] ^ 0x55);
                for (int c = 2; c < 8; c++)
                {
                    pwd[c] = (byte)(pwd[c] ^ 0x55 ^ pwd[c - 2]);
                }
                Array.Copy(pwd, 0, S7_SET_PWD, 29, 8);
                // Sends the telegrem
                SendPacket(S7_SET_PWD);
                if (LastError == 0)
                {
                    Length = RecvIsoPacket();
                    if (Length > 32) // the minimum expected
                    {
                        if (S7.GetWordAt(PDU, 27) != 0)
                            LastError = errS7FunctionError;
                    }
                    else
                        LastError = errS7InvalidPDU;
                }
            }
            return LastError;
        }

        public int ClearSessionPassword()
        {
            int Length;

            LastError = 0;
            SendPacket(S7_CLR_PWD);
            if (LastError == 0)
            {
                Length = RecvIsoPacket();
                if (Length > 30) // the minimum expected
                {
                    if (S7.GetWordAt(PDU, 27) != 0)
                        LastError = errS7FunctionError;
                }
                else
                    LastError = errS7InvalidPDU;
            }
            return LastError;
        }

        public int GetProtection(S7Protection Protection)
        {
            S7Szl SZL = new S7Szl(256);

            LastError = ReadSZL(0x0232, 0x0004, SZL);
            if (LastError == 0)
            {
                Protection.Update(SZL.Data);
            }
            return LastError;
        }

        public void Dispose()
        {
            _SocketClient.Dispose();
        }
    }
}
