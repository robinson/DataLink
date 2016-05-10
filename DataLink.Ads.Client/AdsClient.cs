﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLink.Ads.Client.Commands;
using DataLink.Ads.Client.Common;
using DataLink.Ads.Client.Helpers;
using DataLink.Ads.Client.Special;

namespace DataLink.Ads.Client
{
    public class AdsClient : IDisposable
    {

        /// <summary>
        /// AdsClient Constructor 
        /// This can be used if you wan't to use your own IAmsSocket implementation.
        /// </summary>
        /// <param name="amsSocket">Your own IAmsSocket implementation</param>
        /// <param name="amsNetIdSource">
        /// The AmsNetId of this device. You can choose something in the form of x.x.x.x.x.x 
        /// You have to define this ID in combination with the IP as a route on the target Ads device
        /// </param>
        /// <param name="amsNetIdTarget">The AmsNetId of the target Ads device</param>
        /// <param name="amsPortTarget">Ams port. Default 801</param>
        public AdsClient(string amsNetIdSource, IAmsSocket amsSocket, string amsNetIdTarget, ushort amsPortTarget = 801)
        {
            ams = new Ams(amsSocket);
            ams.AmsNetIdSource = new AmsNetId(amsNetIdSource);
            ams.AmsNetIdTarget = new AmsNetId(amsNetIdTarget);
            ams.AmsPortTarget = amsPortTarget;
        }

        /// <summary>
        /// AdsClient Constructor
        /// </summary>
        /// <param name="settings">The connection settings</param>
        public AdsClient(IAdsConnectionSettings settings)
        {
			ams = new Ams(settings.AmsSocket);
            ams.AmsNetIdSource = new AmsNetId(settings.AmsNetIdSource);
            ams.AmsNetIdTarget = new AmsNetId(settings.AmsNetIdTarget);
            ams.AmsPortTarget = settings.AmsPortTarget;
            this.Name = settings.Name;
        }

        private Ams ams;
        public Ams Ams { get { return ams; } }

        /// <summary>
        /// An internal list of handles and its associated lock object.
        /// </summary>
        private Dictionary<string, uint> activeSymhandles = new Dictionary<string, uint>();
        private object activeSymhandlesLock = new object();

        /// <summary>
        /// Clears the dictionary of handles.
        /// </summary>
        public void ClearSymhandleDictionary()
        {
            lock (activeSymhandlesLock)
                activeSymhandles.Clear();
        }

        /// <summary>
        /// Special functions. (functionality not documented by Beckhoff)
        /// </summary>
        private AdsSpecial special;
        public AdsSpecial Special
        {
            get 
            {
                if (special == null)
                {
                    special = new AdsSpecial(ams);
                }
                return special; 
            }
        }
        
        /// <summary>
        /// When using the generic string method, this is the default string length
        /// </summary>
        private uint defaultStringLenght = 81;
        public uint DefaultStringLength
        {
            get { return defaultStringLenght; }
            set { defaultStringLenght = value; }
        }
 
        public string Name { get; set; }
        
        /// <summary>
        /// This event is called when a subscribed notification is raised
        /// </summary>
        public event AdsNotificationDelegate OnNotification
        {
            add { ams.OnNotification += value; }
            remove { ams.OnNotification -= value; }
        }

        protected virtual void Dispose(bool managed)
        {
          
            if (ams.ConnectedAsync == false) 
            {
                DeleteActiveNotifications();
                ReleaseActiveSymhandles();
            }
          

            if (ams != null) ams.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #region Async Methods


        /// <summary>
        /// Get a handle from a variable name
        /// </summary>
        /// <param name="varName">A twincat variable like ".XXX"</param>
        /// <returns>The handle</returns>
        public virtual async Task<uint> GetSymhandleByNameAsync(string varName) 
        {
            // Check, if the handle is already present.
            lock (activeSymhandlesLock)
            {
                if (activeSymhandles.ContainsKey(varName))
                    return activeSymhandles[varName];
            }

            // It was not retrieved before - get it from the control.
            var adsCommand = new AdsWriteReadCommand(0x0000F003, 0x00000000, varName.ToAdsBytes(), 4);
            var result = await adsCommand.RunAsync(this.ams);
            if (result == null || result.Data == null || result.Data.Length < 4)
                return 0;

            var handle = BitConverter.ToUInt32(result.Data, 0);

            // Now, try to add it.
            lock (activeSymhandlesLock)
            {
                if (!activeSymhandles.ContainsKey(varName))
                    activeSymhandles.Add(varName, handle);

                return handle;
            }
        }

        public async Task<uint> GetSymhandleByNameAsync(IAdsSymhandle symhandle)
        {
            //var symhandle = new AdsSymhandle();
            symhandle.Symhandle = await GetSymhandleByNameAsync(symhandle.VarName);
            symhandle.ConnectionName = Name;
            return symhandle.Symhandle;
        }

        /// <summary>
        /// Release symhandle
        /// </summary>
        /// <param name="symhandle">The handle returned by GetSymhandleByName</param>
        /// <returns>An awaitable task.</returns>
        public virtual Task ReleaseSymhandleAsync(uint symhandle)
        {
            // Perform a reverse-lookup at the dictionary.
            lock (activeSymhandlesLock)
            {
                var key = "";

                foreach (var kvp in activeSymhandles)
                {
                    if (kvp.Value != symhandle)
                        continue;
                    key = kvp.Key;
                    break;
                }

                activeSymhandles.Remove(key);
            }

            return ReleaseSymhandleAsyncInternal(symhandle);
        }

        private Task ReleaseSymhandleAsyncInternal(uint symhandle)
        {
            // Run the release command.
            var adsCommand = new AdsWriteCommand(0x0000F006, 0x00000000, BitConverter.GetBytes(symhandle));
            return adsCommand.RunAsync(this.ams);
        }

        /// <summary>
        /// Release symhandle.
        /// </summary>
        /// <param name="adsSymhandle">The handle.</param>
        /// <returns>An awaitable task.</returns>
        public Task ReleaseSymhandleAsync(IAdsSymhandle adsSymhandle)
        {
            return ReleaseSymhandleAsync(adsSymhandle.Symhandle);
        }

        /// <summary>
        /// Read the value from the handle returned by GetSymhandleByNameAsync
        /// </summary>
        /// <param name="varHandle">The handle returned by GetSymhandleByNameAsync</param>
        /// <returns>A byte[] with the value of the twincat variable</returns>
        public virtual async Task<byte[]> ReadBytesAsync(uint varHandle, uint readLength)
        {
            AdsReadCommand adsCommand = new AdsReadCommand(0x0000F005, varHandle, readLength);
            var result = await adsCommand.RunAsync(this.ams);
            return result.Data;
        }

        public Task<byte[]> ReadBytesAsync(IAdsSymhandle adsSymhandle)
        {
            return ReadBytesAsync(adsSymhandle.Symhandle, adsSymhandle.ByteLength);
        }

        /// <summary>
        /// Reads the value from the name of a twincat variable
        /// </summary>
        /// <param name="varName">A twincat variable like ".XXX" or "MAIN.YYY" etc.</param>
        /// <returns>A byte[] with the value of the twincat variable</returns>
        public async Task<byte[]> ReadBytesAsync<T>(string varName)
        {
            uint varHandle = await GetSymhandleByNameAsync(varName);
            var length = GenericHelper.GetByteLengthFromType<T>(DefaultStringLength);
            var result = await ReadBytesAsync(varHandle, length);
            return result;
        }

        public virtual async Task<byte[]> ReadBytesI_Async(uint offset, uint readLength)
        {
            AdsReadCommand adsCommand = new AdsReadCommand(0x0000F020, offset, readLength);
            var result = await adsCommand.RunAsync(this.ams);
            return result.Data;
        }

        public virtual async Task<byte[]> ReadBytesQ_Async(uint offset, uint readLength)
        {
            AdsReadCommand adsCommand = new AdsReadCommand(0x0000F030, offset, readLength);
            var result = await adsCommand.RunAsync(this.ams);
            return result.Data;
        }

        /// <summary>
        /// Read the value from the handle returned by GetSymhandleByNameAsync.
        /// </summary>
        /// <typeparam name="T">A type like byte, ushort, uint depending on the length of the twincat variable</typeparam>
        /// <param name="varHandle">The handle returned by GetSymhandleByNameAsync</param>
        /// <param name="arrayLength">An optional array length.</param>
        /// <param name="adsObj">An optional existing object.</param>
        /// <returns>The value of the twincat variable</returns>
        public async Task<T> ReadAsync<T>(uint varHandle, uint arrayLength = 1, object adsObj = null) 
        {
            var length = GenericHelper.GetByteLengthFromType<T>(DefaultStringLength, arrayLength);
            var value = await ReadBytesAsync(varHandle, length);

            if (value != null)
                return GenericHelper.GetResultFromBytes<T>(value, DefaultStringLength, arrayLength, adsObj);
            else
                return default(T);
        }

        /// <summary>
        /// Read the value from the handle.
        /// </summary>
        /// <typeparam name="T">A type like byte, ushort, uint depending on the length of the twincat variable</typeparam>
        /// <param name="adsSymhandle">The handle.</param>
        /// <param name="arrayLength">An optional array length.</param>
        /// <param name="adsObj">An optional existing object.</param>
        /// <returns>The value of the twincat variable</returns>
        public Task<T> ReadAsync<T>(IAdsSymhandle adsSymhandle, uint arrayLength = 1, object adsObj = null) 
        {
            return ReadAsync<T>(adsSymhandle.Symhandle, arrayLength, adsObj);
        }

        /// <summary>
        /// Read the value from the name of a twincat variable.
        /// </summary>
        /// <typeparam name="T">A type like byte, ushort, uint depending on the length of the twincat variable</typeparam>
        /// <param name="varName">The name of the twincat variable.</param>
        /// <param name="arrayLength">An optional array length.</param>
        /// <param name="adsObj">An optional existing object.</param>
        /// <returns>The value of the twincat variable</returns>
        public async Task<T> ReadAsync<T>(string varName, uint arrayLength = 1, object adsObj = null)
        {
            uint varHandle = await GetSymhandleByNameAsync(varName);
            var result = await ReadAsync<T>(varHandle, arrayLength, adsObj);
            return result;
        }

        /// <summary>
        /// Add a noticiation when a variable changes or cyclic after a defined time in ms
        /// </summary>
        /// <param name="varHandle">The handle returned by GetSymhandleByNameAsync</param>
        /// <param name="length">The length of the data that must be send by the notification</param>
        /// <param name="transmissionMode">On change or cyclic</param>
        /// <param name="cycleTime">The cyclic time in ms. If used with OnChange, then the value is send once after this time in ms</param>
        /// <param name="userData">A custom object that can be used in the callback</param>
        /// <returns>The notification handle</returns>
        public Task<uint> AddNotificationAsync(uint varHandle, uint length, AdsTransmissionMode transmissionMode, uint cycleTime, object userData)
        {
            return AddNotificationAsync(varHandle, length, transmissionMode, cycleTime, userData, typeof(byte[]));
        }

        public async Task<uint> AddNotificationAsync<T>(string varName, AdsTransmissionMode transmissionMode, uint cycleTime, uint arraylength = 1, object userData = null)
        {
            uint varHandle = await GetSymhandleByNameAsync(varName);
            var length = GenericHelper.GetByteLengthFromType<T>(DefaultStringLength, arraylength);
            var result = await AddNotificationAsync(varHandle, length, transmissionMode, cycleTime, userData, typeof(T));
            return result;
        }

        public Task<uint> AddNotificationAsync(IAdsSymhandle adsSymhandle, AdsTransmissionMode transmissionMode, uint cycleTime, object userData)
        {
            return AddNotificationAsync(adsSymhandle.Symhandle, adsSymhandle.ByteLength, transmissionMode, cycleTime, userData);
        }

        /// <summary>
        /// Add a noticiation when a variable changes or cyclic after a defined time in ms
        /// </summary>
        /// <param name="varHandle">The handle returned by GetSymhandleByNameAsync</param>
        /// <param name="length">The length of the data that must be send by the notification</param>
        /// <param name="transmissionMode">On change or cyclic</param>
        /// <param name="cycleTime">The cyclic time in ms. If used with OnChange, then the value is send once after this time in ms</param>
        /// <param name="userData">A custom object that can be used in the callback</param>
        /// <param name="typeOfValue">The type of the returned notification value</param>
        /// <returns>The notification handle</returns>
        public virtual async Task<uint> AddNotificationAsync(uint varHandle, uint length, AdsTransmissionMode transmissionMode, uint cycleTime, object userData, Type typeOfValue)
        {
            var adsCommand = new AdsAddDeviceNotificationCommand(0x0000F005, varHandle, length, transmissionMode);
            adsCommand.CycleTime = cycleTime;
            adsCommand.UserData = userData;
            adsCommand.TypeOfValue = typeOfValue;
            var result = await adsCommand.RunAsync(this.ams);
            adsCommand.Notification.NotificationHandle = result.NotificationHandle;
            adsCommand.Notification.Symhandle = varHandle;
            return result.NotificationHandle;
        }

        public Task<uint> AddNotificationAsync(IAdsSymhandle adsSymhandle, AdsTransmissionMode transmissionMode, uint cycleTime, object userData, Type typeOfValue)
        {
            return AddNotificationAsync(adsSymhandle.Symhandle, adsSymhandle.ByteLength, transmissionMode, cycleTime, userData, typeOfValue);
        }

        /// <summary>
        /// Add a noticiation when a variable changes or cyclic after a defined time in ms
        /// </summary>
        /// <typeparam name="T">Type for defining the length of the data that must be send by the notification</typeparam>
        /// <param name="varHandle">The handle returned by GetSymhandleByNameAsync</param>
        /// <param name="transmissionMode">On change or cyclic</param>
        /// <param name="cycleTime">The cyclic time in ms. If used with OnChange, then the value is send once after this time in ms</param>
        /// <param name="userData">A custom object that can be used in the callback</param>
        /// <returns></returns>
        public Task<uint> AddNotificationAsync<T>(uint varHandle, AdsTransmissionMode transmissionMode, uint cycleTime, object userData) 
        {
            uint length = GenericHelper.GetByteLengthFromType<T>(DefaultStringLength);
            return AddNotificationAsync(varHandle, length, transmissionMode, cycleTime, userData, typeof(T));
        }

        public Task<uint> AddNotificationAsync<T>(IAdsSymhandle adsSymhandle, AdsTransmissionMode transmissionMode, uint cycleTime, object userData)
        {
            return AddNotificationAsync<T>(adsSymhandle.Symhandle, transmissionMode, cycleTime, userData);
        }

        /// <summary>
        /// Delete a previously registerd notification
        /// </summary>
        /// <param name="notificationHandle">The handle returned by AddNotification(Async)</param>
        /// <returns></returns>
        public virtual Task DeleteNotificationAsync(uint notificationHandle)
        {
            var adsCommand = new AdsDeleteDeviceNotificationCommand(notificationHandle);
            return adsCommand.RunAsync(this.ams);
        }

        public async Task DeleteNotificationAsync(string varName)
        {
            uint varHandle = await GetSymhandleByNameAsync(varName);
            var notificationHandle = this.ams.NotificationRequests.First(request => request.Symhandle == varHandle).NotificationHandle;
            await DeleteNotificationAsync(notificationHandle);
        }

        /// <summary>
        /// Write the value to the handle returned by GetSymhandleByNameAsync
        /// </summary>
        /// <param name="varHandle">The handle returned by GetSymhandleByNameAsync</param>
        /// <param name="varValue">The byte[] value to be sent</param>
        public virtual Task WriteBytesAsync(uint varHandle, IEnumerable<byte> varValue)
        {
            AdsWriteCommand adsCommand = new AdsWriteCommand(0x0000F005, varHandle, varValue);
            return adsCommand.RunAsync(this.ams);
        }

        public async Task WriteBytesAsync(string varName, IEnumerable<byte> varValue)
        {
            uint varHandle = await GetSymhandleByNameAsync(varName);
            await WriteBytesAsync(varHandle, varValue);
        }

        public Task WriteBytesAsync(IAdsSymhandle adsSymhandle, IEnumerable<byte> varValue)
        {
            return WriteBytesAsync(adsSymhandle.Symhandle, varValue);
        }

        /// <summary>
        /// Write the value to the handle returned by GetSymhandleByNameAsync
        /// </summary>
        /// <typeparam name="T">A type like byte, ushort, uint depending on the length of the twincat variable</typeparam>
        /// <param name="varHandle">The handle returned by GetSymhandleByNameAsync</param>
        /// <param name="varValue">The value to be sent</param>
        public Task WriteAsync<T>(uint varHandle, T varValue) 
        {
            IEnumerable<byte> varValueBytes = GenericHelper.GetBytesFromType<T>(varValue, defaultStringLenght);
            return this.WriteBytesAsync(varHandle, varValueBytes);
        }

        public Task WriteAsync<T>(IAdsSymhandle adsSymhandle, T varValue) 
        {
            return WriteAsync<T>(adsSymhandle.Symhandle, varValue);
        }

        public async Task WriteAsync<T>(string varName, T varValue)
        {
            uint varHandle = await GetSymhandleByNameAsync(varName);
            await WriteAsync<T>(varHandle, varValue);
        }

        /// <summary>
        /// Get some information of the ADS device (version, name)
        /// </summary>
        /// <returns></returns>
        public virtual async Task<AdsDeviceInfo> ReadDeviceInfoAsync()
        {
            AdsReadDeviceInfoCommand adsCommand = new AdsReadDeviceInfoCommand();
            var result = await adsCommand.RunAsync(this.ams);
            return result.AdsDeviceInfo;
        }

        /// <summary>
        /// Read the ads state 
        /// </summary>
        /// <returns></returns>
        public virtual async Task<AdsState> ReadStateAsync()
        {
            var adsCommand = new AdsReadStateCommand();
            var result = await adsCommand.RunAsync(this.ams);
            return result.AdsState;
        }

        public virtual async Task DeleteActiveNotificationsAsync()
        {
            while (ams.NotificationRequests.Count > 0)
            {
                await DeleteNotificationAsync(ams.NotificationRequests[0].NotificationHandle);
            }
        }

        public virtual async Task ReleaseActiveSymhandlesAsync()
        {
            List<uint> handles;

            lock (activeSymhandlesLock)
            {
                handles = activeSymhandles.Values.ToList();
                activeSymhandles.Clear();
            }

            foreach (var handle in handles)
                await ReleaseSymhandleAsyncInternal(handle);
        }

        #endregion

        #region Blocking Methods
     

        /// <summary>
        /// Get a handle from a variable name
        /// </summary>
        /// <param name="varName">A twincat variable like ".XXX"</param>
        /// <returns>The handle</returns>
        public virtual uint GetSymhandleByName(string varName)
        {
            // Check, if the handle is already present.
            lock (activeSymhandlesLock)
            {
                if (activeSymhandles.ContainsKey(varName))
                    return activeSymhandles[varName];
            }

            // It was not retrieved before - get it from the control.
            var adsCommand = new AdsWriteReadCommand(0x0000F003, 0x00000000, varName.ToAdsBytes(), 4);
            var result = adsCommand.Run(this.ams);
            if (result == null || result.Data == null || result.Data.Length < 4)
                return 0;

            var handle = BitConverter.ToUInt32(result.Data, 0);

            // Now, try to add it.
            lock (activeSymhandlesLock)
            {
                if (!activeSymhandles.ContainsKey(varName))
                    activeSymhandles.Add(varName, handle);

                return handle;
            }
        }

        /// <summary>
        /// Get a handle object from a variable name
        /// </summary>
        /// <param name="varName">A twincat variable like ".XXX"</param>
        /// <returns>An AdsSymhandle object</returns>
        public uint GetSymhandleByName(IAdsSymhandle symHandle)
        {
            symHandle.Symhandle = GetSymhandleByName(symHandle.VarName);
            symHandle.ConnectionName = Name;
            return symHandle.Symhandle;
        }

        private void ReleaseSymhandleInternal(uint symhandle)
        {
            // Run the release command.
            var adsCommand = new AdsWriteCommand(0x0000F006, 0x00000000, BitConverter.GetBytes(symhandle));
            adsCommand.Run(this.ams);
        }

        /// <summary>
        /// Release symhandle
        /// </summary>
        /// <param name="symhandle">The handle returned by GetSymhandleByName</param>
        public virtual void ReleaseSymhandle(uint symhandle)
        {
            // Perform a reverse-lookup at the dictionary.
            lock (activeSymhandlesLock)
            {
                foreach (var kvp in activeSymhandles)
                {
                    if (kvp.Value == symhandle)
                    {
                        activeSymhandles.Remove(kvp.Key);
                        break;
                    }
                }
            }

            ReleaseSymhandleInternal(symhandle);
        }

        /// <summary>
        /// Release symhandle.
        /// </summary>
        /// <param name="adsSymhandle">The handle.</param>
        public void ReleaseSymhandle(IAdsSymhandle adsSymhandle)
        {
            ReleaseSymhandle(adsSymhandle.Symhandle);
        }

        /// <summary>
        /// Read the value from the handle returned by GetSymhandleByName
        /// </summary>
        /// <param name="varHandle">The handle returned by GetSymhandleByName</param>
        /// <returns>A byte[] with the value of the twincat variable</returns>
        public virtual byte[] ReadBytes(uint varHandle, uint readLength)
        {
            AdsReadCommand adsCommand = new AdsReadCommand(0x0000F005, varHandle, readLength);
            var result = adsCommand.Run(this.ams);
            return result.Data;
        }

        public byte[] ReadBytes(IAdsSymhandle adsSymhandle)
        {
            return ReadBytes(adsSymhandle.Symhandle, adsSymhandle.ByteLength);
        }

        public byte[] ReadBytes<T>(string varName)
        {
            uint varHandle = GetSymhandleByName(varName);
            var length = GenericHelper.GetByteLengthFromType<T>(defaultStringLenght);
            return ReadBytes(varHandle, length);
        }

        public virtual byte[] ReadBytesI(uint offset, uint readLength)
        {
            AdsReadCommand adsCommand = new AdsReadCommand(0x0000F020, offset, readLength);
            var result = adsCommand.Run(this.ams);
            return result.Data;
        }

        public virtual byte[] ReadBytesQ(uint offset, uint readLength)
        {
            AdsReadCommand adsCommand = new AdsReadCommand(0x0000F030, offset, readLength);
            var result = adsCommand.Run(this.ams);
            return result.Data;
        }

        /// <summary>
        /// Read the value from the handle returned by GetSymhandleByName.
        /// </summary>
        /// <typeparam name="T">A type like byte, ushort, uint depending on the length of the twincat variable</typeparam>
        /// <param name="varHandle">The handle returned by GetSymhandleByName</param>
        /// <param name="arrayLength">An optional array length.</param>
        /// <param name="adsObj">An optional existing object.</param>
        /// <returns>The value of the twincat variable</returns>
        public T Read<T>(uint varHandle, uint arrayLength = 1, object adsObj = null)
        {
            var length = GenericHelper.GetByteLengthFromType<T>(DefaultStringLength, arrayLength);
            var value = ReadBytes(varHandle, length);

            if (value != null)
                return GenericHelper.GetResultFromBytes<T>(value, DefaultStringLength, arrayLength, adsObj);
            else
                return default(T);
        }

        public T Read<T>(string varName, uint arrayLength = 1, object adsObj = null)
        {
            var varHandle = GetSymhandleByName(varName);
            return Read<T>(varHandle, arrayLength, adsObj);
        }

        /// <summary>
        /// Read the value from the handle.
        /// </summary>
        /// <typeparam name="T">A type like byte, ushort, uint depending on the length of the twincat variable</typeparam>
        /// <param name="adsSymhandle">The handle.</param>
        /// <param name="arrayLength">An optional array length.</param>
        /// <param name="adsObj">An optional existing object.</param>
        /// <returns>The value of the twincat variable</returns>
        public T Read<T>(IAdsSymhandle adsSymhandle, uint arrayLength = 1, object adsObj = null) 
        {
            return Read<T>(adsSymhandle.Symhandle, arrayLength, adsObj);
        }

        /// <summary>
        /// Add a noticiation when a variable changes or cyclic after a defined time in ms
        /// </summary>
        /// <param name="varHandle">The handle returned by GetSymhandleByName</param>
        /// <param name="length">The length of the data that must be send by the notification</param>
        /// <param name="transmissionMode">On change or cyclic</param>
        /// <param name="cycleTime">The cyclic time in ms. If used with OnChange, then the value is send once after this time in ms</param>
        /// <param name="userData">A custom object that can be used in the callback</param>
        /// <returns>The notification handle</returns>
        public uint AddNotification(uint varHandle, uint length, AdsTransmissionMode transmissionMode, uint cycleTime, object userData)
        {
            return AddNotification(varHandle, length, transmissionMode, cycleTime, userData, typeof(byte[]));
        }

        public uint AddNotification(IAdsSymhandle adsSymhandle, AdsTransmissionMode transmissionMode, uint cycleTime, object userData)
        {
            return AddNotification(adsSymhandle.Symhandle, adsSymhandle.ByteLength, transmissionMode, cycleTime, userData);
        }

        public uint AddNotification<T>(string varName, AdsTransmissionMode transmissionMode, uint cycleTime, object userData = null)
        {
            var varHandle = GetSymhandleByName(varName);
            uint length = GenericHelper.GetByteLengthFromType<T>(DefaultStringLength);
            return AddNotification(varHandle, length, transmissionMode, cycleTime, userData, typeof(T));
        }

        /// <summary>
        /// Add a noticiation when a variable changes or cyclic after a defined time in ms
        /// </summary>
        /// <param name="varHandle">The handle returned by GetSymhandleByName</param>
        /// <param name="length">The length of the data that must be send by the notification</param>
        /// <param name="transmissionMode">On change or cyclic</param>
        /// <param name="cycleTime">The cyclic time in ms. If used with OnChange, then the value is send once after this time in ms</param>
        /// <param name="userData">A custom object that can be used in the callback</param>
        /// <param name="TypeOfValue">The type of the returned notification value</param>
        /// <returns>The notification handle</returns>
        public virtual uint AddNotification(uint varHandle, uint length, AdsTransmissionMode transmissionMode, uint cycleTime, object userData, Type TypeOfValue)
        {
            var adsCommand = new AdsAddDeviceNotificationCommand(0x0000F005, varHandle, length, transmissionMode);
            adsCommand.CycleTime = cycleTime;
            adsCommand.UserData = userData;
            adsCommand.TypeOfValue = TypeOfValue;
            var result = adsCommand.Run(this.ams);
            adsCommand.Notification.NotificationHandle = result.NotificationHandle;
            adsCommand.Notification.Symhandle = varHandle;
            return result.NotificationHandle;
        }

        public uint AddNotification(IAdsSymhandle adsSymhandle, AdsTransmissionMode transmissionMode, uint cycleTime, object userData, Type TypeOfValue)
        {
            return AddNotification(adsSymhandle.Symhandle, adsSymhandle.ByteLength, transmissionMode, cycleTime, userData, TypeOfValue);
        }

        /// <summary>
        /// Add a noticiation when a variable changes or cyclic after a defined time in ms
        /// </summary>
        /// <typeparam name="T">Type for defining the length of the data that must be send by the notification</typeparam>
        /// <param name="varHandle">The handle returned by GetSymhandleByNameAsync</param>
        /// <param name="transmissionMode">On change or cyclic</param>
        /// <param name="cycleTime">The cyclic time in ms. If used with OnChange, then the value is send once after this time in ms</param>
        /// <param name="userData">A custom object that can be used in the callback</param>
        /// <returns></returns>
        public uint AddNotification<T>(uint varHandle, AdsTransmissionMode transmissionMode, uint cycleTime, object userData)
        {
            uint length = GenericHelper.GetByteLengthFromType<T>(DefaultStringLength);
            return AddNotification(varHandle, length, transmissionMode, cycleTime, userData, typeof(T));
        }

        public uint AddNotification<T>(IAdsSymhandle adsSymhandle, AdsTransmissionMode transmissionMode, uint cycleTime, object userData) 
        {
            return AddNotification<T>(adsSymhandle.Symhandle, transmissionMode, cycleTime, userData);
        }

        /// <summary>
        /// Delete a previously registerd notification
        /// </summary>
        /// <param name="notificationHandle">The handle returned by AddNotification</param>
        /// <returns></returns>
        public virtual void DeleteNotification(uint notificationHandle)
        {
            var adsCommand = new AdsDeleteDeviceNotificationCommand(notificationHandle);
            adsCommand.Run(this.ams);
        }

        public virtual void DeleteNotification(string varName)
        {
            uint varHandle = GetSymhandleByName(varName);
            var notificationHandle = this.ams.NotificationRequests.First(request => request.Symhandle == varHandle).NotificationHandle;
            DeleteNotification(notificationHandle);
        }

        /// <summary>
        /// Write the value to the handle returned by GetSymhandleByName
        /// </summary>
        /// <param name="varHandle">The handle returned by GetSymhandleByName</param>
        /// <param name="varValue">The byte[] value to be sent</param>
        public virtual void WriteBytes(uint varHandle, IEnumerable<byte> varValue)
        {
            AdsWriteCommand adsCommand = new AdsWriteCommand(0x0000F005, varHandle, varValue);
            adsCommand.Run(this.ams);
        }

        public void WriteBytes(IAdsSymhandle adsSymhandle, IEnumerable<byte> varValue)
        {
            WriteBytes(adsSymhandle.Symhandle, varValue);
        }

        public void WriteBytes(string varName, IEnumerable<byte> varValue)
        {
            var varHandle = GetSymhandleByName(varName);
            WriteBytes(varHandle, varValue);
        }

        /// <summary>
        /// Write the value to the handle returned by GetSymhandleByName
        /// </summary>
        /// <typeparam name="T">A type like byte, ushort, uint depending on the length of the twincat variable</typeparam>
        /// <param name="varHandle">The handle returned by GetSymhandleByName</param>
        /// <param name="varValue">The value to be sent</param>
        public void Write<T>(uint varHandle, T varValue)
        {
            IEnumerable<byte> varValueBytes = GenericHelper.GetBytesFromType<T>(varValue, defaultStringLenght);
            this.WriteBytes(varHandle, varValueBytes);
        }

        public void Write<T>(IAdsSymhandle adsSymhandle, T varValue)
        {
            Write<T>(adsSymhandle, varValue);
        }

        public void Write<T>(string varName, T varValue)
        {
            var varHandle = GetSymhandleByName(varName);
            Write<T>(varHandle, varValue);
        }

        /// <summary>
        /// Get some information of the ADS device (version, name)
        /// </summary>
        /// <returns></returns>
        public AdsDeviceInfo ReadDeviceInfo()
        {
            AdsReadDeviceInfoCommand adsCommand = new AdsReadDeviceInfoCommand();
            var result = adsCommand.Run(this.ams);
            return result.AdsDeviceInfo;
        }

        /// <summary>
        /// Read the ads state 
        /// </summary>
        /// <returns></returns>
        public AdsState ReadState()
        {
            var adsCommand = new AdsReadStateCommand();
            var result = adsCommand.Run(this.ams);
            return result.AdsState;
        }

        public virtual void DeleteActiveNotifications()
        {
            if (ams.NotificationRequests != null)
            {
                while (ams.NotificationRequests.Count > 0)
                {
                    DeleteNotification(ams.NotificationRequests[0].NotificationHandle);
                }
            }
        }

        public virtual void ReleaseActiveSymhandles()
        {
            List<uint> handles;

            lock (activeSymhandlesLock)
            {
                handles = activeSymhandles.Values.ToList();
                activeSymhandles.Clear();
            }

            foreach (var handle in handles)
                ReleaseSymhandleInternal(handle);
        }

        
        #endregion

    }
}
