using System;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.IO.Compression;
using AD.BASE.IO;
using static UnityEngine.Rendering.DebugUI;

namespace AD.BASE
{
    [Serializable]
    public sealed class ADFile : IDisposable
    {
        public static implicit operator bool(ADFile file) => file.ErrorException == null;

        public string FilePath { get => m_FilePath; private set => m_FilePath = value; }
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public bool IsError { get => isError; private set => isError = value; }
        public bool IsEmpty { get => isEmpty; private set => isEmpty = value; }
        public Exception ErrorException { get; private set; } = null;
        public bool IsKeepFileControl { get => isKeepFileControl; private set => isKeepFileControl = value; }
        private Stream FileStream;
        internal Stream _MyStream => FileStream;
        public ADSettings MySetting { get => mySetting; private set => mySetting = value; }
        public byte[] FileData { get; private set; } = null;

        [SerializeField] private string m_FilePath = "";
        [SerializeField] private ADSettings mySetting;
        [SerializeField] private bool isDelete = false;
        [SerializeField] private bool isError = false;
        [SerializeField] private bool isEmpty = false;
        [SerializeField] private bool isKeepFileControl = false;

        public void Delete()
        {
            this.Dispose();
            FileC.DeleteFile(FilePath);
            ErrorException = null;
            IsError = false;
            IsEmpty = true;
            isDelete = true;
        }

        /// <summary>
        /// Just Use This ADFile Delete , You Can Use This Function To Create
        /// </summary>
        /// <param name="isRefresh"></param>
        /// <param name="isKeepFileControl"></param>
        /// <returns></returns>
        public bool Create(bool isRefresh = true, bool isKeepFileControl = true)
        {
            if (isDelete)
            {
                try
                {
                    if (File.Exists(FilePath))
                    {
                        Timestamp = File.GetLastWriteTime(FilePath).ToUniversalTime();
                    }
                    else File.Create(FilePath);
                    InitFileStream(isRefresh, isKeepFileControl);
                }
                catch (Exception ex)
                {
                    SetErrorStatus(ex);
                    return false;
                }
                return true;
            }
            return false;
        }

        ~ADFile()
        {
            Dispose();
        }

        public ADFile(string filePath, bool isTryCreate, bool isRefresh, bool isKeepFileControl)
        {
            try
            {
                FilePath = filePath;
                if (File.Exists(filePath))
                {
                    Timestamp = File.GetLastWriteTime(filePath).ToUniversalTime();
                }
                else if (!isTryCreate)
                {
                    SetErrorStatus(new ADException("File Cannt Found"));
                    return;
                }
                else FileC.CreateFile(filePath);
                InitFileStream(isRefresh, isKeepFileControl);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        public ADFile(string filePath, bool isTryCreate, bool isRefresh, Stream stream)
        {
            try
            {
                FilePath = filePath;
                if (File.Exists(filePath))
                {
                    Timestamp = File.GetLastWriteTime(filePath).ToUniversalTime();
                }
                else if (!isTryCreate)
                {
                    SetErrorStatus(new ADException("File Cannt Found"));
                    return;
                }
                else FileC.CreateFile(filePath);
                InitFileStream(isRefresh, stream);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        public ADFile(bool isCanOverwrite, string filePath, bool isRefresh, bool isKeepFileControl)
        {
            try
            {
                FilePath = filePath;
                if (File.Exists(filePath))
                {
                    if (isCanOverwrite)
                    {
                        SetErrorStatus(new ADException("File Is Exists"));
                        return;
                    }
                }
                else FileC.CreateFile(filePath);
                Timestamp = File.GetLastWriteTime(filePath).ToUniversalTime();
                InitFileStream(isRefresh, isKeepFileControl);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        public ADFile(bool isCanOverwrite, string filePath, bool isRefresh, Stream stream)
        {
            try
            {
                FilePath = filePath;
                if (File.Exists(filePath))
                {
                    if (isCanOverwrite)
                    {
                        SetErrorStatus(new ADException("File Is Exists"));
                        return;
                    }
                }
                else FileC.CreateFile(filePath);
                Timestamp = File.GetLastWriteTime(filePath).ToUniversalTime();
                InitFileStream(isRefresh, stream);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        public ADFile(ADSettings settings)
        {
            try
            {
                this.MySetting = settings;
                this.IsKeepFileControl = false;
                this.FilePath = settings.FullPath;
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        private void InitFileStream(bool isRefresh, bool isKeepFileControl)
        {
            if (this.IsKeepFileControl = isKeepFileControl)
            {
                FileStream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite);
            }
            if (isRefresh) UpdateFileData();
        }

        private void InitFileStream(bool isRefresh, Stream stream)
        {
            if (isRefresh) UpdateFileData(stream);
        }

        private void SetErrorStatus(Exception ex)
        {
            this.IsError = true;
            this.IsEmpty = true;
            this.ErrorException = ex;
            Timestamp = DateTime.UtcNow;
            Debug.LogException(ex);
        }

        private bool DebugMyself()
        {
            if (this.IsEmpty || this.ErrorException != null)
            {
                Debug.LogWarning("This File Was Drop in a error : " + this.ErrorException.Message);
                Debug.LogException(ErrorException);
                return true;
            }
            return false;
        }

        public void UpdateFileData()
        {
            if (DebugMyself()) return;
            if (this.IsKeepFileControl)
            {
                UpdateFileData(FileStream);
            }
            else
            {
                using (var nFileStream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    UpdateFileData(nFileStream);
                }
            }
        }

        public void UpdateFileData(Stream stream)
        {
            if (DebugMyself()) return;
            FileData = new byte[stream.Length];
            byte[] buffer = new byte[256];
            int len, i = 0;
            while ((len = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                for (int j = 0; j < len; j++)
                {
                    FileData[i++] = buffer[j];
                }
            }
        }

        public AssetBundle LoadAssetBundle()
        {
            if (DebugMyself()) return null;
            return AssetBundle.LoadFromMemory(FileData);
        }

        public T LoadObject<T>(bool isRefresh, Func<string, T> loader, System.Text.Encoding encoding)
        {
            if (DebugMyself()) return default;
            if (isRefresh) UpdateFileData();
            string str = encoding.GetString(FileData);
            return loader(str);
        }

        public object LoadObject<T>(bool isRefresh, Func<string, object> loader, System.Text.Encoding encoding)
        {
            if (DebugMyself()) return null;
            if (isRefresh) UpdateFileData();
            string str = encoding.GetString(FileData);
            return loader(str);
        }

        public T LoadObject<T>(bool isRefresh, Func<string, T> loader)
        {
            if (DebugMyself()) return default;
            if (isRefresh) UpdateFileData();
            string str = System.Text.Encoding.Default.GetString(FileData);
            return loader(str);
        }

        public object LoadObject<T>(bool isRefresh, Func<string, object> loader)
        {
            if (DebugMyself()) return null;
            if (isRefresh) UpdateFileData();
            string str = System.Text.Encoding.Default.GetString(FileData);
            return loader(str);
        }

        public string GetString(bool isRefresh, System.Text.Encoding encoding)
        {
            if (DebugMyself()) return null;
            if (isRefresh) UpdateFileData();
            return encoding.GetString(FileData);
        }

        /// <summary>
        /// Text Mode
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="isRefresh"></param>
        /// <param name="encoding"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Deserialize<T>(bool isRefresh, System.Text.Encoding encoding, out object obj)
        {
            if (DebugMyself())
            {
                obj = ErrorException;
                return false;
            }
            string source = "";
            try
            {
                source = GetString(isRefresh, encoding);
                if (typeof(T).IsPrimitive)
                {
                    obj = typeof(T).GetMethod("Parse").Invoke(source, null);
                    return true;
                }
                else if (typeof(T).GetAttribute<SerializableAttribute>() != null)
                {
                    obj = JsonConvert.DeserializeObject<T>(source);
                    if (obj != null) return true;
                    else return false;
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
                Debug.LogError("ADFile.Deserialize<T>(bool,Encoding) : T is " + typeof(T).FullName + " , is failed on " + FilePath + "\nsource : " + source);
                Debug.LogException(ex);
            }
            obj = default(T);
            return false;
        }

        /// <summary>
        /// Binary Stream Mode(Load From Immediate Current File)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="isText"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Deserialize<T>(out object obj)
        {
            if (DebugMyself())
            {
                obj = ErrorException;
                return false;
            }
            try
            {
                if (IsKeepFileControl)
                {
                    obj = new BinaryFormatter().Deserialize(FileStream);
                }
                else
                {
                    using FileStream fs = new(FilePath, FileMode.Open);
                    obj = new BinaryFormatter().Deserialize(fs);
                }
                return obj != null;
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
                Debug.LogError("ADFile.Deserialize<T>() : T is " + typeof(T).FullName + " , is failed on " + FilePath);
                Debug.LogException(ex);
                obj = default(T);
                return false;
            }
        }

        public bool Deserialize<T>(out T result, string key = "default")
        {
            ADSettings settings = MySetting ?? new ADSettings(FilePath);
            result = default;
            try
            {
                using ADReader reader = ADReader.Create(IsKeepFileControl ? FileStream : ADFile.CreateStream(settings, ADStreamEnum.FileMode.Read), settings, true);
                result = reader.Read<T>(key);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Save in AD.BASE.IO mode. <see cref="AD.BASE.IO.ADJSONWriter"/>
        /// </summary>
        public bool Serialize<T>(T obj, string key = "default")
        {
            ADSettings settings = MySetting ?? new ADSettings(FilePath);
            try
            {
                using ADWriter writer = ADWriter.Create(IsKeepFileControl ? FileStream : ADFile.CreateStream(settings, ADStreamEnum.FileMode.Write), settings, true, true);
                writer.Write<T>(key, obj);
                writer.Save();
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
                return false;
            }
            return true;
        }

        public bool Serialize<T>(T obj, System.Text.Encoding encoding, bool isAllowSerializeAsBinary = true)
        {
            if (DebugMyself())
            {
                return false;
            }
            try
            {
                if (typeof(T).GetAttribute<SerializableAttribute>() == null)
                {
                    Debug.LogWarning("this type is not use SerializableAttribute but you now is try to serialize it");
                    if (!isAllowSerializeAsBinary) throw new ADException("Not Support");
                    using MemoryStream ms = new();
                    new BinaryFormatter().Serialize(ms, obj);
                    FileData = ms.GetBuffer();
                    SaveFileData();
                    return true;
                }
                else
                {
                    if (IsKeepFileControl)
                    {
                        this.FileData = encoding.GetBytes(JsonConvert.SerializeObject(obj, Formatting.Indented));
                        FileStream.Write(this.FileData, 0, this.FileData.Length);
                    }
                    else
                    {
                        File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented), encoding);
                        UpdateFileData();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
#if UNITY_EDITOR
                Debug.LogError("ADFile.Deserialize<T>(bool,Encoding) : T is " + typeof(T).FullName + " , is failed on " + FilePath);
                Debug.LogException(ex);
#endif
                return false;
            }
        }

        /// <summary>
        /// Saved entirely in binary
        /// </summary>
        public bool Serialize<T>(T obj)
        {
            if (DebugMyself())
            {
                return false;
            }
            try
            {
                using MemoryStream ms = new();
                new BinaryFormatter().Serialize(ms, obj);
                this.FileData = ms.GetBuffer();
                FileData = ms.GetBuffer();
                SaveFileData();
                return true;
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
#if UNITY_EDITOR
                Debug.LogError("ADFile.Deserialize<T>(bool,Encoding) : T is " + typeof(T).FullName + " , is failed on " + FilePath);
                Debug.LogException(ex);
#endif
                return false;
            }
        }

        public void Close()
        {
            if (IsKeepFileControl)
            {
                FileStream?.Close();
                FileStream?.Dispose();
                FileStream = null;
                IsKeepFileControl = false;
                IsError = false;
                IsEmpty = true;
            }
            this.FileData = null;
        }

        public void Keep(ADStreamEnum.FileMode mode)
        {
            if (MySetting != null)
            {
                Close();
                IsKeepFileControl = true;
                InitFileStream(false, ADFile.CreateStream(MySetting, mode));
            }
            else SetErrorStatus(new ADException("Setting is missing or destroy"));
        }

        public void Keep(bool isRefresh)
        {
            if(MySetting!=null)
            {
                Close();
                IsKeepFileControl = true;
                InitFileStream(isRefresh, ADFile.CreateStream(MySetting, ADStreamEnum.FileMode.Write));
            }
            else if (!IsKeepFileControl)
            {
                Close();
                InitFileStream(isRefresh, true);
            }
        }

        public void Dispose()
        {
            this.Close();
            this.MySetting = null;
            this.FileData = null;
        }

        public void Append(byte[] appendition)
        {
            byte[] newData = new byte[appendition.Length + FileData.Length];
            Array.Copy(FileData, 0, newData, 0, FileData.Length);
            Array.Copy(appendition, 0, newData, FileData.Length, appendition.Length);
            FileData = newData;
        }

        public void ReplaceAllData(byte[] data)
        {
            FileData = data;
        }

        public static byte[] ToBytes(object obj)
        {
            using MemoryStream ms = new();
            new BinaryFormatter().Serialize(ms, obj);
            return ms.GetBuffer();
        }

        public static object FromBytes(byte[] bytes)
        {
            using MemoryStream ms = new();
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;
            return new BinaryFormatter().Deserialize(ms);
        }

        public bool SaveFileData()
        {
            if (DebugMyself())
            {
                return false;
            }
            try
            {
                if (IsKeepFileControl)
                {
                    FileStream.Write(FileData, 0, FileData.Length);
                }
                else
                {
                    File.WriteAllBytes(FilePath, FileData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            return true;
        }

        private const string _ErrorCannotWriteToResourcesWhenEditorTime
            = "Cannot write directly to Resources folder. Try writing to a directory outside of Resources, and then manually move the file there.";
        private const string _ErrorCannotWriteToResourcesWhenRuntime
            = "Cannot write to Resources folder at runtime. Use a different save location at runtime instead.";

        public static Stream CreateStream(ADSettings settings, ADStreamEnum.FileMode fileMode)
        {
            bool isWriteStream = (fileMode != ADStreamEnum.FileMode.Read);
            Stream stream = null;

            // Check that the path is in a valid format. This will throw an exception if not.
            string fullPath = settings.FullPath;
            new FileInfo(fullPath);

            try
            {
                if (settings.location == ADStreamEnum.Location.InternalMS)
                {
                    // There's no point in creating an empty MemoryStream if we're only reading from it.
                    if (!isWriteStream)
                        return null;
                    stream = new MemoryStream(settings.bufferSize);
                }
                else if (settings.location == ADStreamEnum.Location.File)
                {
                    if (!isWriteStream && !FileC.FileExists(fullPath))
                        return null;
                    stream = new ADFileStream(fullPath, fileMode, settings.bufferSize, false);
                }
                else if (settings.location == ADStreamEnum.Location.PlayerPrefs)
                {
                    if (isWriteStream)
                        stream = new ADPlayerPrefsStream(fullPath, settings.bufferSize, (fileMode == ADStreamEnum.FileMode.Append));
                    else
                    {
                        if (!PlayerPrefs.HasKey(fullPath))
                            return null;
                        stream = new ADPlayerPrefsStream(fullPath);
                    }
                }
                else if (settings.location == ADStreamEnum.Location.Resources)
                {
                    if (!isWriteStream)
                    {
                        var resourcesStream = new ADResourcesStream(fullPath);
                        if (resourcesStream.Exists)
                            stream = resourcesStream;
                        else
                        {
                            resourcesStream.Dispose();
                            return null;
                        }
                    }
                    else if (UnityEngine.Application.isEditor)
                        throw new System.NotSupportedException(_ErrorCannotWriteToResourcesWhenEditorTime);
                    else
                        throw new System.NotSupportedException(_ErrorCannotWriteToResourcesWhenRuntime);
                }

                return CreateStream(stream, settings, fileMode);
            }
            catch (System.Exception e)
            {
                stream?.Dispose();
                throw e;
            }
        }

        public static Stream CreateStream(Stream stream, ADSettings settings, ADStreamEnum.FileMode fileMode)
        {
            try
            {
                bool isWriteStream = (fileMode != ADStreamEnum.FileMode.Read);

#if !DISABLE_ENCRYPTION
                // Encryption
                if (settings.encryptionType != ADStreamEnum.EncryptionType.None && stream.GetType() != typeof(UnbufferedCryptoStream))
                {
                    EncryptionAlgorithm alg = null;
                    if (settings.encryptionType == ADStreamEnum.EncryptionType.AES)
                        alg = new AESEncryptionAlgorithm();
                    stream = new UnbufferedCryptoStream(stream, !isWriteStream, settings.encryptionPassword, settings.bufferSize, alg);
                }
#endif

                // Compression
                if (settings.compressionType != ADStreamEnum.CompressionType.None && stream.GetType() != typeof(GZipStream))
                {
                    if (settings.compressionType == ADStreamEnum.CompressionType.Gzip)
                        stream = isWriteStream ? new GZipStream(stream, CompressionMode.Compress) : new GZipStream(stream, CompressionMode.Decompress);
                }

                return stream;
            }
            catch (System.Exception e)
            {
                if (stream != null)
                    stream.Dispose();
                if (e.GetType() == typeof(System.Security.Cryptography.CryptographicException))
                    throw new System.Security.Cryptography.CryptographicException("Could not decrypt file. Please ensure that you are using the same password used to encrypt the file.");
                else
                    throw e;
            }
        }

        public static void CopyTo(Stream source, Stream destination)
        {
#if UNITY_2019_1_OR_NEWER
            source.CopyTo(destination);
#else
            byte[] buffer = new byte[2048];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                destination.Write(buffer, 0, bytesRead);
#endif
        }
    }

    [Serializable]
    public class OfflineFile
    {
        public List<byte[]> MainMapDatas = new();
        public Dictionary<string, byte[]> SourceAssetsDatas = new();
        public Dictionary<string, string> PathRelayers = new();

        public void Add(ICanMakeOffline target)
        {
            MainMapDatas.Add(ADFile.ToBytes(target));
            foreach (var path in target.GetFilePaths())
            {
                if (!SourceAssetsDatas.ContainsKey(path))
                {
                    try
                    {
                        SourceAssetsDatas.Add(path, File.ReadAllBytes(path));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        public void Add(object target)
        {
            if (target is ICanMakeOffline mO) Add(mO);
            else
            {
                MainMapDatas.Add(ADFile.ToBytes(target));
            }
        }

        public void Build(string path)
        {
            ADFile file = new(path, true, false, true);
            file.ReplaceAllData(ADFile.ToBytes(this));
            file.Dispose();
        }

        public static OfflineFile BuildFrom(string path)
        {
            return ADFile.FromBytes(File.ReadAllBytes(path)) as OfflineFile;
        }

        public void ReleaseFile(string directory)
        {
            foreach (var asset in SourceAssetsDatas)
            {
                string fileName = Path.GetFileName(asset.Key);
                ADFile file = new(Path.Combine(directory, fileName), true, false, true);
                file.ReplaceAllData(SourceAssetsDatas[asset.Key]);
                file.SaveFileData();
                file.Dispose();
                PathRelayers.Add(asset.Key, file.FilePath);
            }
        }

        /// <summary>
        /// Used After <see cref="ReleaseFile(string)"/>
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public string GetNewPath(string origin)
        {
            return PathRelayers.TryGetValue(origin, out var path) ? path : null;
        }
    }

    public interface ICanMakeOffline
    {
        public string[] GetFilePaths();

        public void ReplacePath(Dictionary<string, string> sourceAssetsDatas);
    }
}

