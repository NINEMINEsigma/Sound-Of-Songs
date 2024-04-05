using System.IO;
using UnityEngine;
using System.Security.Cryptography;
using System.IO.Compression;


#if UNITY_EDITOR
using UnityEditor;
#endif

//此处以及引用有内容源于ES3并加以更改，非原创
namespace AD.BASE
{
    public static class ADStreamEnum
    {
        public enum Location { File, PlayerPrefs, InternalMS, Resources, Cache };
        public enum Directory { PersistentDataPath, DataPath }
        public enum EncryptionType { None, AES };
        public enum CompressionType { None, Gzip };
        public enum Format { JSON };
        public enum ReferenceMode { ByRef, ByValue, ByRefAndValue };
        public enum FileMode { Read, Write, Append }
    }

    public static class ADHash
    {
#if NETFX_CORE
		public static string SHA1Hash(string input)
		{
			return System.Text.Encoding.UTF8.GetString(UnityEngine.Windows.Crypto.ComputeSHA1Hash(System.Text.Encoding.UTF8.GetBytes(input)));
		}
#else
        public static string SHA1Hash(string input)
        {
            using SHA1Managed sha1 = new SHA1Managed();
            return System.Text.Encoding.UTF8.GetString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
        }
#endif
    }

    public class ADFileStream : FileStream
    {
        private bool isDisposed = false;

        public ADFileStream(string path, ADStreamEnum.FileMode fileMode, int bufferSize, bool useAsync)
            : base(GetPath(path, fileMode), GetFileMode(fileMode), GetFileAccess(fileMode), FileShare.None, bufferSize, useAsync)
        {
        }

        // Gets a temporary path if necessary.
        protected static string GetPath(string path, ADStreamEnum.FileMode fileMode)
        {
            string directoryPath = FileC.GetDirectoryPath(path);
            // Attempt to create the directory incase it does not exist if we are storing data.
            if (fileMode != ADStreamEnum.FileMode.Read && directoryPath != FileC.persistentDataPath)
                FileC.CreateDirectory(directoryPath);
            if (fileMode != ADStreamEnum.FileMode.Write || fileMode == ADStreamEnum.FileMode.Append)
                return path;
            return (fileMode == ADStreamEnum.FileMode.Write) ? path + FileC.temporaryFileSuffix : path;
        }

        protected static FileMode GetFileMode(ADStreamEnum.FileMode fileMode)
        {
            if (fileMode == ADStreamEnum.FileMode.Read)
                return FileMode.Open;
            else if (fileMode == ADStreamEnum.FileMode.Write)
                return FileMode.Create;
            else
                return FileMode.Append;
        }

        protected static FileAccess GetFileAccess(ADStreamEnum.FileMode fileMode)
        {
            if (fileMode == ADStreamEnum.FileMode.Read)
                return FileAccess.Read;
            else if (fileMode == ADStreamEnum.FileMode.Write)
                return FileAccess.Write;
            else
                return FileAccess.Write;
        }

        protected override void Dispose(bool disposing)
        {
            // Ensure we only perform disposable once.
            if (isDisposed)
                return;
            isDisposed = true;

            base.Dispose(disposing);
        }
    }

    public class ADPlayerPrefsStream : MemoryStream
    {
        private string path;
        private bool append;
        private bool isWriteStream = false;
        private bool isDisposed = false;

        // This constructor should be used for read streams only.
        public ADPlayerPrefsStream(string path) : base(GetData(path, false))
        {
            this.path = path;
            this.append = false;
        }

        // This constructor should be used for write streams only.
        public ADPlayerPrefsStream(string path, int bufferSize, bool append = false) : base(bufferSize)
        {
            this.path = path;
            this.append = append;
            this.isWriteStream = true;
        }

        private static byte[] GetData(string path, bool isWriteStream)
        {
            if (!PlayerPrefs.HasKey(path))
                throw new FileNotFoundException("File \"" + path + "\" could not be found in PlayerPrefs");
            return System.Convert.FromBase64String(PlayerPrefs.GetString(path));
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;
            if (isWriteStream && this.Length > 0)
            {
                if (append)
                {
                    // Convert data back to bytes before appending, as appending Base-64 strings directly can corrupt the data.
                    var sourceBytes = System.Convert.FromBase64String(PlayerPrefs.GetString(path));
                    var appendBytes = this.ToArray();
                    var finalBytes = new byte[sourceBytes.Length + appendBytes.Length];
                    System.Buffer.BlockCopy(sourceBytes, 0, finalBytes, 0, sourceBytes.Length);
                    System.Buffer.BlockCopy(appendBytes, 0, finalBytes, sourceBytes.Length, appendBytes.Length);

                    PlayerPrefs.SetString(path, System.Convert.ToBase64String(finalBytes));

                    PlayerPrefs.Save();
                }
                else
                    PlayerPrefs.SetString(path + FileC.temporaryFileSuffix, System.Convert.ToBase64String(this.ToArray()));
                // Save the timestamp to a separate key.
                PlayerPrefs.SetString("timestamp_" + path, System.DateTime.UtcNow.Ticks.ToString());
            }
            base.Dispose(disposing);
        }
    }

    public class ADResourcesStream : MemoryStream
    {
        // Check that data exists by checking stream is not empty.
        public bool Exists { get { return this.Length > 0; } }

        // Used when creating 
        public ADResourcesStream(string path) : base(GetData(path))
        {
        }

        private static byte[] GetData(string path)
        {
            var textAsset = Resources.Load(path) as TextAsset;

            // If data doesn't exist in Resources, return an empty byte array.
            if (textAsset == null)
                return new byte[0];

            return textAsset.bytes;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public abstract class EncryptionAlgorithm
    {
        public abstract byte[] Encrypt(byte[] bytes, string password, int bufferSize);
        public abstract byte[] Decrypt(byte[] bytes, string password, int bufferSize);
        public abstract void Encrypt(Stream input, Stream output, string password, int bufferSize);
        public abstract void Decrypt(Stream input, Stream output, string password, int bufferSize);

        protected static void CopyStream(Stream input, Stream output, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = input.Read(buffer, 0, bufferSize)) > 0)
                output.Write(buffer, 0, read);
        }
    }

    public class AESEncryptionAlgorithm : EncryptionAlgorithm
    {
        private const int ivSize = 16;
        private const int keySize = 16;
        private const int pwIterations = 100;

        public override byte[] Encrypt(byte[] bytes, string password, int bufferSize)
        {
            using var input = new MemoryStream(bytes);
            using var output = new MemoryStream();
            Encrypt(input, output, password, bufferSize);
            return output.ToArray();
        }

        public override byte[] Decrypt(byte[] bytes, string password, int bufferSize)
        {
            using var input = new MemoryStream(bytes);
            using var output = new MemoryStream();
            Decrypt(input, output, password, bufferSize);
            return output.ToArray();
        }

        public override void Encrypt(Stream input, Stream output, string password, int bufferSize)
        {
            input.Position = 0;

#if NETFX_CORE
            // Generate an IV and write it to the output.
            var iv = CryptographicBuffer.GenerateRandom(ivSize);
            output.Write(iv.ToArray(), 0, ivSize);

            var pwBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
            var keyDerivationProvider = KeyDerivationAlgorithmProvider.OpenAlgorithm("PBKDF2_SHA1");
            KeyDerivationParameters pbkdf2Parms = KeyDerivationParameters.BuildForPbkdf2(iv, pwIterations);
            // Create a key based on original key and derivation parmaters
            CryptographicKey keyOriginal = keyDerivationProvider.CreateKey(pwBuffer);
            IBuffer keyMaterial = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Parms, keySize);

            var provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
            var key = provider.CreateSymmetricKey(keyMaterial);

            // Get the input stream as an IBuffer.
            IBuffer msg;
            using(var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                msg = ms.ToArray().AsBuffer();
            }

            var buffEncrypt = CryptographicEngine.Encrypt(key, msg, iv);


            output.Write(buffEncrypt.ToArray(), 0, (int)buffEncrypt.Length);
            output.Dispose();
#else
            using (var alg = Aes.Create())
            {
                alg.Mode = CipherMode.CBC;
                alg.Padding = PaddingMode.PKCS7;
                alg.GenerateIV();
                var key = new Rfc2898DeriveBytes(password, alg.IV, pwIterations);
                alg.Key = key.GetBytes(keySize);
                // Write the IV to the output stream.
                output.Write(alg.IV, 0, ivSize);
                using var encryptor = alg.CreateEncryptor();
                using var cs = new CryptoStream(output, encryptor, CryptoStreamMode.Write);
                CopyStream(input, cs, bufferSize);
            }
#endif
        }

        public override void Decrypt(Stream input, Stream output, string password, int bufferSize)
        {
#if NETFX_CORE
            var thisIV = new byte[ivSize];
            input.Read(thisIV, 0, ivSize);
            var iv = thisIV.AsBuffer();

            var pwBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);

            var keyDerivationProvider = KeyDerivationAlgorithmProvider.OpenAlgorithm("PBKDF2_SHA1");
            KeyDerivationParameters pbkdf2Parms = KeyDerivationParameters.BuildForPbkdf2(iv, pwIterations);
            // Create a key based on original key and derivation parameters.
            CryptographicKey keyOriginal = keyDerivationProvider.CreateKey(pwBuffer);
            IBuffer keyMaterial = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Parms, keySize);
            
            var provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
            var key = provider.CreateSymmetricKey(keyMaterial);

            // Get the input stream as an IBuffer.
            IBuffer msg;
            using(var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                msg = ms.ToArray().AsBuffer();
            }

            var buffDecrypt = CryptographicEngine.Decrypt(key, msg, iv);

            output.Write(buffDecrypt.ToArray(), 0, (int)buffDecrypt.Length);
#else
            using (var alg = Aes.Create())
            {
                var thisIV = new byte[ivSize];
                input.Read(thisIV, 0, ivSize);
                alg.IV = thisIV;

                var key = new Rfc2898DeriveBytes(password, alg.IV, pwIterations);
                alg.Key = key.GetBytes(keySize);

                using var decryptor = alg.CreateDecryptor();
                using var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
                CopyStream(cryptoStream, output, bufferSize);

            }
#endif
            output.Position = 0;
        }
    }

    public class UnbufferedCryptoStream : MemoryStream
    {
        private readonly Stream stream;
        private readonly bool isReadStream;
        private string password;
        private int bufferSize;
        private EncryptionAlgorithm alg;
        private bool disposed = false;

        public UnbufferedCryptoStream(Stream stream, bool isReadStream, string password, int bufferSize, EncryptionAlgorithm alg) : base()
        {
            this.stream = stream;
            this.isReadStream = isReadStream;
            this.password = password;
            this.bufferSize = bufferSize;
            this.alg = alg;


            if (isReadStream)
                alg.Decrypt(stream, this, password, bufferSize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;

            if (!isReadStream)
                alg.Encrypt(this, stream, password, bufferSize);
            stream.Dispose();
            base.Dispose(disposing);
        }
    }

#if UNITY_VISUAL_SCRIPTING
    [Unity.VisualScripting.IncludeInSettings(true)]
#elif BOLT_VISUAL_SCRIPTING
[Ludiq.IncludeInSettings(true)]
#endif
    public class ADSettings : System.ICloneable
    {

        #region Default settings

        #region Const String

        private const string defaultSettingsPath = "Assets/AD/Resources";

        private const string _LogWhenIdentifiedReleaseBuildADRelease 
            = "This has been identified as a release build as the title contains 'AD Release', so ADDefaults will not be created.";
        private const string _ExceptionADSettingPathIsNullAndCanntLoad
            = "The 'path' field of this ADSettings is null, indicating that it was not possible to load the default settings from Resources. " +
              "Please check that the AD Default Settings.prefab exists in Assets/Plugins/Resources/AD/";

        private const string _DefaultSaveFilePath = "SaveFile.ad";

        #endregion

        private static ADDefaults _defaultSettingsScriptableObject = null;
        public static ADDefaults DefaultSettingsScriptableObject
        {
            get
            {
                if (_defaultSettingsScriptableObject == null)
                {
                    _defaultSettingsScriptableObject = Resources.Load<ADDefaults>(_DefaultSaveFilePath);

#if UNITY_EDITOR
                    if (_defaultSettingsScriptableObject == null)
                    {
                        _defaultSettingsScriptableObject = ScriptableObject.CreateInstance<ADDefaults>();
                    
                        // If this is the version being submitted to the Asset Store, don't include ES3Defaults.
                        if (Application.productName.Contains("AD Release"))
                        {
                            Debug.Log(_LogWhenIdentifiedReleaseBuildADRelease);
                            return _defaultSettingsScriptableObject;
                        }

                        //AD Resources Folder is must exist
                        AssetDatabase.CreateAsset(_defaultSettingsScriptableObject,Path.Combine(defaultSettingsPath, _DefaultSaveFilePath));
                        AssetDatabase.SaveAssets();
                    }
#endif
                }
                return _defaultSettingsScriptableObject;
            }
        }

        private static ADSettings _defaultSettings = null;
        public static ADSettings DefaultSettings
        {
            get
            {
                if (_defaultSettings == null)
                {
                    if (DefaultSettingsScriptableObject != null)
                        _defaultSettings = DefaultSettingsScriptableObject.settings;
                }
                return _defaultSettings;
            }
        }

        private static ADSettings _unencryptedUncompressedSettings = null;
        internal static ADSettings UnencryptedUncompressedSettings
        {
            get
            {
                if (_unencryptedUncompressedSettings == null)
                    _unencryptedUncompressedSettings = new ADSettings(ADStreamEnum.EncryptionType.None, ADStreamEnum.CompressionType.None);
                return _unencryptedUncompressedSettings;
            }
        }

        #endregion

        #region Fields

        private static readonly string[] resourcesExtensions = new string[] { ".txt", ".htm", ".html", ".xml", ".bytes", ".json", ".csv", ".yaml", ".fnt" };

        #region

        [SerializeField]
        private ADStreamEnum.Location _location;
        /// <summary>The location where we wish to store data. As it's not possible to save/load from File in WebGL, if the default location is File it will use PlayerPrefs instead.</summary>
        public ADStreamEnum.Location location
        {
            get
            {
                if (_location == ADStreamEnum.Location.File && (Application.platform == RuntimePlatform.WebGLPlayer || Application.platform == RuntimePlatform.tvOS))
                    return ADStreamEnum.Location.PlayerPrefs;
                return _location;
            }
            set { _location = value; }
        }

        /// <summary>The path associated with this ADSettings object, if any.</summary>
        public string path = _DefaultSaveFilePath;
        /// <summary>The type of encryption to use when encrypting data, if any.</summary>
        public ADStreamEnum.EncryptionType encryptionType = ADStreamEnum.EncryptionType.None;
        /// <summary>The type of encryption to use when encrypting data, if any.</summary>
        public ADStreamEnum.CompressionType compressionType = ADStreamEnum.CompressionType.None;
        /// <summary>The password to use when encrypting data.</summary>
        public string encryptionPassword = "password";
        /// <summary>The default directory in which to store files, and the location which relative paths should be relative to.</summary>
        public ADStreamEnum.Directory directory = ADStreamEnum.Directory.PersistentDataPath;
        /// <summary>What format to use when serialising and deserialising data.</summary>
        public ADStreamEnum.Format format = ADStreamEnum.Format.JSON;
        /// <summary>Whether we want to pretty print JSON.</summary>
        public bool prettyPrint = true;
        /// <summary>Any stream buffers will be set to this length in bytes.</summary>
        public int bufferSize = 2048;
        /// <summary>The text encoding to use for text-based format. Note that changing this may invalidate previous save data.</summary>
        public System.Text.Encoding encoding = System.Text.Encoding.UTF8;
        // <summary>Whether we should serialise children when serialising a GameObject.</summary>
        public bool saveChildren = true;
        // <summary>Whether we should apply encryption and/or compression to raw cached data if they're specified in the cached data's settings.</summary>
        public bool postprocessRawCachedData = false;

        /// <summary>Whether we should check that the data we are loading from a file matches the method we are using to load it.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool typeChecking = true;

        /// <summary>Enabling this ensures that only serialisable fields are serialised. Otherwise, possibly unsafe fields and properties will be serialised.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool safeReflection = true;
        /// <summary>Whether UnityEngine.Object members should be stored by value, reference or both.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ADStreamEnum.ReferenceMode memberReferenceMode = ADStreamEnum.ReferenceMode.ByRef;
        /// <summary>Whether the main save methods should save UnityEngine.Objects by value, reference, or both.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ADStreamEnum.ReferenceMode referenceMode = ADStreamEnum.ReferenceMode.ByRefAndValue;

        /// <summary>How many levels of hierarchy Easy Save will serialise. This is used to protect against cyclic references.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public int serializationDepthLimit = 64;

        #endregion

        /// <summary>The names of the Assemblies we should try to load our ADTypes from.</summary>
        //[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        //public string[] assemblyNames = new string[] { "Assembly-CSharp-firstpass", "Assembly-CSharp" };

        /// <summary>Gets the full, absolute path which this ADSettings object identifies.</summary>
        public string FullPath
        {
            get
            {
                if (path == null)
                    throw new System.NullReferenceException(_ExceptionADSettingPathIsNullAndCanntLoad);

                if (IsAbsolute(path))
                    return path;

                if (location == ADStreamEnum.Location.File)
                {
                    if (directory == ADStreamEnum.Directory.PersistentDataPath)
                        return new ApplicationPersistentDataPathHelper(path).SourcePath;
                    if (directory == ADStreamEnum.Directory.DataPath)
                        return new ApplicationDataPathHelper(path);
                    throw new System.NotImplementedException("File directory \"" + directory + "\" has not been implemented.");
                }
                else if (location == ADStreamEnum.Location.Resources)
                {
                    // Check that it has valid extension
                    var extension = System.IO.Path.GetExtension(path);
                    bool hasValidExtension = false;
                    foreach (var ext in resourcesExtensions)
                    {
                        if (extension == ext)
                        {
                            hasValidExtension = true;
                            break;
                        }
                    }

                    if (!hasValidExtension)
                        throw new System.ArgumentException("Extension of file in Resources must be .json, .bytes, .txt, .csv, .htm, .html, .xml, .yaml or .fnt, but path given was \"" + path + "\"");

                    // Remove extension
                    string resourcesPath = path.Replace(extension, "");
                    return resourcesPath;
                }
                else return path;
            }
        }

        #endregion

        #region Constructors

        public ADSettings(string path = null, ADSettings settings = null) : this(true)
        {
            // if there are settings to merge, merge them.
            if (settings != null)
                settings.CopyInto(this);

            if (path != null)
                this.path = path;
        }

        public ADSettings(string path, params System.Enum[] enums) : this(enums)
        {
            if (path != null)
                this.path = path;
        }

        public ADSettings(params System.Enum[] enums) : this(true)
        {
            foreach (var setting in enums)
            {
                if (setting is ADStreamEnum.EncryptionType encryptionType)
                    this.encryptionType = encryptionType;
                else if (setting is ADStreamEnum.Location locationType)
                    this.location = locationType;
                else if (setting is ADStreamEnum.CompressionType compressionTypeType)
                    this.compressionType = compressionTypeType;
                else if (setting is ADStreamEnum.ReferenceMode referenceModeType)
                    this.referenceMode = referenceModeType;
                else if (setting is ADStreamEnum.Format formatType)
                    this.format = formatType;
                else if (setting is ADStreamEnum.Directory directoryType)
                    this.directory = directoryType;
                else throw new ADException("Not Support Setting Enum Type");
            }
        }

        public ADSettings(ADStreamEnum.EncryptionType encryptionType, string encryptionPassword) : this(true)
        {
            this.encryptionType = encryptionType;
            this.encryptionPassword = encryptionPassword;
        }

        public ADSettings(string path, ADStreamEnum.EncryptionType encryptionType, string encryptionPassword, ADSettings settings = null) : this(path, settings)
        {
            this.encryptionType = encryptionType;
            this.encryptionPassword = encryptionPassword;
        }

        /* Base constructor which allows us to bypass defaults so it can be called by Editor serialization */
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ADSettings(bool applyDefaults)
        {
            if (applyDefaults)
                if (DefaultSettings != null)
                    _defaultSettings.CopyInto(this);
        }

        #endregion

        #region Utility methods

        private static bool IsAbsolute(string path)
        {
            if (path.Length > 0 && (path[0] == '/' || path[0] == '\\'))
                return true;
            if (path.Length > 1 && path[1] == ':')
                return true;
            return false;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public object Clone()
        {
            var settings = new ADSettings();
            CopyInto(settings);
            return settings;
        }

        private void CopyInto(ADSettings newSettings)
        {
            newSettings._location = _location;
            newSettings.directory = directory;
            newSettings.format = format;
            newSettings.prettyPrint = prettyPrint;
            newSettings.path = path;
            newSettings.encryptionType = encryptionType;
            newSettings.encryptionPassword = encryptionPassword;
            newSettings.compressionType = compressionType;
            newSettings.bufferSize = bufferSize;
            newSettings.encoding = encoding;
            newSettings.typeChecking = typeChecking;
            newSettings.safeReflection = safeReflection;
            newSettings.memberReferenceMode = memberReferenceMode;
            //newSettings.assemblyNames = assemblyNames;
            newSettings.saveChildren = saveChildren;
            newSettings.serializationDepthLimit = serializationDepthLimit;
            newSettings.postprocessRawCachedData = postprocessRawCachedData;
        }

        #endregion
    }

    /*
     * 	A serializable version of the settings we can use as a field in the Editor, which doesn't automatically
     * 	assign defaults to itself, so we get no serialization errors.
     */
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Serializable]
    public class ADSerializableSettings : ADSettings
    {
        public ADSerializableSettings() : base(false) { }
        public ADSerializableSettings(bool applyDefaults) : base(applyDefaults) { }
        public ADSerializableSettings(string path) : base(false) { this.path = path; }
        public ADSerializableSettings(string path, ADStreamEnum.Location location) : base(false) { this.path = path; this.location = location; }

#if UNITY_EDITOR
        public bool showAdvancedSettings = false;
#endif
    }

}
