using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using AD.BASE;
using AD.Derivation.GameEditor;
using AD.Sample.Texter;
using AD.Sample.Texter.Data;
using AD.Utility;
using Newtonsoft.Json;
using UnityEngine;

/*
namespace AD.Obsolete.Sample.Texter
{
    [Serializable]
    public class ProjectData : AD.Derivation.Localization.Cache.CacheAssets<AD.Derivation.Localization.Cache.CacheAssetsKey, ProjectItemDataCache, ProjectItemData, ProjectData_BaseMap>
    {
        public static string ProjectItemDataExtension => "projitem";
        public static string ProjectItemDataPointExtension = "." + ProjectItemDataExtension;
        public const string OfflineExtension = "offline";

        public DataAssets DataAssetsForm;

        private static ProjectData_BaseMap Load(FileInfo file)
        {
            return ES3.Load<ProjectData_BaseMap>(ProjectItemDataExtension, file.FullName);
        }

        private static void Save(AD.Derivation.Localization.Cache.ICanCacheData<ProjectItemData, ProjectData_BaseMap> cacheDataForm, string path)
        {
            ES3.Save<ProjectData_BaseMap>(ProjectItemDataExtension, cacheDataForm.MatchElementBM.Get(), path);
        }

        public IEnumerator Load(TaskInfo loadingTask)
        {
            App.instance.AddMessage("Start Loading Data To Items 1/2");

            TimeClocker timer = TimeExtension.GetTimer();

            string fileListPath = Path.Combine(LoadingManager.FilePath, DataAssetsForm.AssetsName);
            FileC.TryCreateDirectroryOfFile(Path.Combine(fileListPath, "Empty.empty"));
            var fileList = FileC.FindAll(fileListPath, ProjectItemDataExtension);
            yield return new WaitForEndOfFrame();
            App.instance.AddMessage("Start Loading Data To Items 2/2");
            if (fileList != null)
            {
                List<ProjectItemData> projectItemDatas = new();
                for (int i = 0, e = fileList.Count; i < e; i++)
                {
                    FileInfo file = fileList[i];
                    if (timer.LastDalteSceond > 0.1f)
                    {
                        loadingTask.TaskValue = (float)i / (float)e + 0.3f;
                        yield return new WaitForEndOfFrame();
                        timer.Update();
                    }

                    ProjectData_BaseMap bmap = Load(file);
                    string key = bmap.ProjectItemID;
                    bmap.ToObject(out ProjectItemData projcetdata);
                    this.Add(
                        new AD.Derivation.Localization.Cache.CacheAssetsKey(key),
                        new ProjectItemDataCache(key, projcetdata, bmap));
                    projectItemDatas.Add(projcetdata);
                }
                for (int i = 0, e = projectItemDatas.Count; i < e; i++)
                {
                    ProjectItemData projcetdata = projectItemDatas[i];
                    if (timer.LastDalteSceond > 0.1f)
                    {
                        loadingTask.TaskValue = (float)i / (float)e + 1.2f;
                        yield return new WaitForEndOfFrame();
                        timer.Update();
                    }

                    App.instance.OnGenerate.Invoke(projcetdata);
                }
            }

            loadingTask.TaskPercent = 1.00f;
        }

        public IEnumerator Save(TaskInfo savingTask)
        {
            DebugExtension.Log();
            App.instance.AddMessage("Start Saving Data");

            TimeClocker timer = TimeExtension.GetTimer();

            string fileListPath = Path.Combine(LoadingManager.FilePath, DataAssetsForm.AssetsName);
            var oriFiles = FileC.FindAll(fileListPath, ProjectItemDataExtension);
            if (oriFiles != null)
                foreach (var file in oriFiles)
                {
                    file.Delete();
                }

            this.datas.RemoveNullValues();

            int i = 0, e = this.Count;
            foreach (var item in this)
            {
                if (timer.LastDalteSceond > 0.1f)
                {
                    savingTask.TaskValue = (float)i / (float)e + 0.3f;
                    yield return new WaitForEndOfFrame();
                    timer.Update();
                }
                Save(item, Path.Combine(LoadingManager.FilePath, DataAssetsForm.AssetsName, item.MatchElementBM.Get().ProjectItemID + ProjectItemDataPointExtension));
            }

            savingTask.TaskPercent = 1.00f;
        }

        public byte[] BuildOffline(string path)
        {
            FileC.TryCreateDirectroryOfFile(path);
            OfflineFile offlineFile = new();
            using ADFile file = new(path, true, false, true);

            foreach (Derivation.Localization.Cache.ICanCacheData<ProjectItemData, ProjectData_BaseMap> item in this)
            {
                offlineFile.Add((object)item.MatchElementBM.Get());
            }

            file.ReplaceAllData(ADFile.ToBytes(offlineFile));
            file.SaveFileData();
            return file.FileData;
        }

        public void BuildFromOffline(byte[] data)
        {
            OfflineFile offlineFile = ADFile.FromBytes(data) as OfflineFile;
            offlineFile.ReleaseFile(Path.Combine(LoadingManager.FilePath, DataAssetsForm.AssetsName));

            List<ProjectData_BaseMap> result = new();
            foreach (var item in offlineFile.MainMapDatas)
            {
                var temp = ProjectData_BaseMap.ReadOffline(item);
                if(temp is ICanMakeOffline offline)
                {
                    offline.ReplacePath(offlineFile.PathRelayers);
                }
                result.Add(temp);
            }
            this.Clear();
            foreach (var item in result)
            {
                item.ToObject(out ProjectItemData itemData);
                this.Add(new(item.ProjectItemID), new(item.ProjectItemID, itemData, item));
            }
        }
    }

    [Serializable]
    public class ProjectItemDataCache : AD.Derivation.Localization.Cache.AbstractCache<ProjectItemData, ProjectData_BaseMap>
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif
    {
#if UNITY_EDITOR

        [SerializeField] private ProjectItemData m_ProjectItemData;
        [SerializeField] private ProjectData_BaseMap m_ProjectData_BaseMap;

        public void OnAfterDeserialize()
        {
            this.MatchElement.Set(m_ProjectItemData);
            this.MatchElementBM.Set(m_ProjectData_BaseMap);
        }

        public void OnBeforeSerialize()
        {
            m_ProjectItemData = this.MatchElement.Get();
            m_ProjectData_BaseMap = this.MatchElementBM.Get();
        }

#endif

        public ProjectItemDataCache(string key, ProjectItemData baseData, ProjectData_BaseMap mapData) : base(key)
        {
            this.MatchElement = new();
            if (baseData != null) this.MatchElement.Set(baseData);
            this.MatchElementBM = new();
            if (mapData != null) this.MatchElementBM.Set(mapData);
        }
        public ProjectItemDataCache(string key) : this(key, null, null) { }
        public ProjectItemDataCache() : this("Empty New Key") { }

        public override bool FromObject(ProjectItemData from)
        {
            this.MatchElement.Set(from);
            UpdataMatchBMByBase();
            return true;
        }

        public override bool FromObject(IBase from)
        {
            bool result = false;
            if (from is ProjectItemData projectItemData)
            {
                result = FromObject(projectItemData);
                UpdataMatchBMByBase();
            }
            return result;
        }

        public override void ToObject(out ProjectItemData obj)
        {
            obj = this.MatchElement.Get();
        }
    }

    [Serializable]
    public class ProjectItemData : IBase<ProjectData_BaseMap>
    {
#if UNITY_EDITOR
        public
#else
        
#endif
        static List<ProjectItemData> s_Datas = new();

        public const string ProjectRootID = "ProjectRoot";

        public static IProjectItem GetParent(string key)
        {
            if (key == ProjectRootID) return App.instance.GetController<ProjectManager>().ProjectRootMono;
            var cat = s_Datas.FirstOrDefault(T => T.ProjectItemID == key);
            if (cat == null) return null;
            else return cat.MatchProjectItem;
        }

        public ProjectItemData() { s_Datas.Add(this); }
        public ProjectItemData(IProjectItemWhereNeedInitData matchProjectItem) : this(matchProjectItem, "New Object", ProjectRootID, Vector2.zero) { }
        protected ProjectItemData(IProjectItemWhereNeedInitData matchProjectItem, string projectItemID, string parentItemID, Vector2 projectItemPosition)
        {
            _MatchProjectItem = matchProjectItem;
            ProjectItemID = projectItemID;
            ParentItemID = parentItemID ?? ProjectItemData.ProjectRootID;
            ProjectItemPosition = projectItemPosition;
            s_Datas.Add(this);
        }
        ~ProjectItemData()
        {
            s_Datas.RemoveAll(null);
            s_Datas.Remove(this);
        }

        private IProjectItemWhereNeedInitData _MatchProjectItem;
        public IProjectItemWhereNeedInitData MatchProjectItem
        {
            get => _MatchProjectItem;
            set
            {
                _MatchProjectItem = value;
            }
        }
        private string projectItemID;
        public string ProjectItemID
        {
            get => projectItemID;
            set
            {
                Internal_Set_ProjectItemID(value);
            }
        }

        private void Internal_Set_ProjectItemID(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                ADGlobalSystem.ThrowLogicError("ProjectItemID : value arg is invalid");
            }
            var container = App.instance.GetController<ProjectManager>().CurrentProjectData;
            if (!string.IsNullOrEmpty(projectItemID))
                container.Remove(new(projectItemID));
            string key = value;
            int icounter = 1;
            while (s_Datas.FirstOrDefault(T => T.ProjectItemID == key) != null)
            {
                key = value + $"({icounter++})";
            }
            projectItemID = key.ToString();
        }

        public string ParentItemID;
        public Vector2 ProjectItemPosition;

        #region IBase

        public bool FromMap(IBaseMap from)
        {
            if (from is ProjectData_BaseMap projectData_BaseMap)
            {
                return FromMap(projectData_BaseMap);
            }
            return false;
        }

        public void ToMap(out IBaseMap BM)
        {
            ToMap(out ProjectData_BaseMap projectData_BaseMap);
            BM = projectData_BaseMap;
        }

        #endregion

        /// <summary>
        /// Base is useful
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public virtual bool FromMap(ProjectData_BaseMap from)
        {
            this.ProjectItemID = from.ProjectItemID;
            this.ParentItemID = from.ParentItemID ?? ProjectItemData.ProjectRootID;
            this.ProjectItemPosition = new(from.ProjectItemPositionX, from.ProjectItemPositionY);
            return true;
        }

        /// <summary>
        /// Base is NotImplementedException
        /// </summary>
        /// <param name="BM"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void ToMap(out ProjectData_BaseMap BM)
        {
            throw new NotImplementedException();
        }
    }

    namespace Data
    {
        [EaseSave3]
        [Serializable]
        public class ProjectData_BaseMap : IBaseMap<ProjectItemData>
        {
            public static Dictionary<string, string> RegisterOfflineSourceFile;

            public string ProjectItemID;
            public string ParentItemID;
            public float ProjectItemPositionX;
            public float ProjectItemPositionY;

            public virtual byte[] BuildOffline()
            {
                return ADFile.ToBytes(this);
            }
            public static ProjectData_BaseMap ReadOffline(byte[] data)
            {
                return ADFile.FromBytes(data) as ProjectData_BaseMap;
            }

            #region NotSupport

            public bool Deserialize(string source)
            {
                throw new ADException("Not Support");
            }

            public string Serialize()
            {
                throw new ADException("Not Support");
            }

            #endregion

            #region IBaseMap

            public bool FromObject(IBase from)
            {
                if (from is ProjectItemData projectItemData)
                {
                    return FromObject(projectItemData);
                }
                return false;
            }

            public void ToObject(out IBase obj)
            {
                ToObject(out ProjectItemData projectItemData);
                obj = projectItemData;
            }

            #endregion

            #region IBaseMap<ProjectItemData>

            /// <summary>
            /// Base is NotImplementedException
            /// </summary>
            /// <param name="obj"></param>
            /// <exception cref="NotImplementedException"></exception>
            public virtual void ToObject(out ProjectItemData obj)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Base is useful
            /// </summary>
            /// <param name="from"></param>
            /// <returns></returns>
            public virtual bool FromObject(ProjectItemData from)
            {
                this.ProjectItemID = from.ProjectItemID;
                this.ParentItemID = from.ParentItemID ?? ProjectItemData.ProjectRootID;
                this.ProjectItemPositionX = from.ProjectItemPosition.x;
                this.ProjectItemPositionY = from.ProjectItemPosition.y;
                return true;
            }

            #endregion

        }
    }
}
*/

namespace AD.Sample.Texter
{

    [Serializable]
    public class ProjectItemData
    {
        public const string ProjectRootID = "ProjectRoot";

        public ProjectItemData() { }
        public ProjectItemData(IProjectItemWhereNeedInitData matchProjectItem) : this(matchProjectItem, "New Object", Vector2.zero) { }
        protected ProjectItemData(IProjectItemWhereNeedInitData matchProjectItem, string projectItemID, Vector2 projectItemPosition)
        {
            MatchProjectItem = matchProjectItem;
            ProjectItemID = projectItemID;
            ProjectItemPosition = projectItemPosition;
        }
        public ProjectItemData(IProjectItemRoot root)
        {
            MatchProjectItem = root;
            ProjectItemID = "Root";
            ProjectItemPosition = new Vector2(0,0);
        }

        [AD.SAL._Ignore_, NonSerialized] public IProjectItem MatchProjectItem;
        public string ProjectItemID;
        public ProjectItemData Parent;
        public List<ProjectItemData> Childs;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public float ProjectItemPositionX, ProjectItemPositionY;
        [AD.SAL._Ignore_]
        public Vector2 ProjectItemPosition
        {
            get => new(ProjectItemPositionX, ProjectItemPositionY);
            set
            {
                ProjectItemPositionX = value.x;
                ProjectItemPositionY = value.y;
            }
        }

        public ProjectItemData ExecuteBeforeSave()
        {
            Childs = MatchProjectItem
                .GetChilds()
                .GetSubList<ProjectItemData, ICanSerializeOnCustomEditor>(T => true, T => T
                .As<IProjectItemWhereNeedInitData>()
                .SourceData
                .ExecuteBeforeSave());
            if(MatchProjectItem.ParentTarget != null)
            {
                if(MatchProjectItem.ParentTarget.As<IProjectItemWhereNeedInitData>(out var parent0))
                {
                    Parent = parent0.SourceData;
                }
                else if(MatchProjectItem.ParentTarget is IProjectItemRoot)
                {
                    Parent = App.instance.GetController<ProjectManager>().CurrentRootData;
                }
            }
            return this;
        }

        public byte[] BuildOffline(string path)
        {
            FileC.TryCreateDirectroryOfFile(path);
            OfflineFile offlineFile = new();
            using ADFile file = new(path, true, false, true);

            ExecuteBeforeSave();
            offlineFile.Add(this);
            file.ReplaceAllData(ADFile.ToBytes(offlineFile));
            file.SaveFileData();
            return file.FileData;
        }

        public virtual byte[] BuildOffline()
        {
            return ADFile.ToBytes(this);
        }

        public static ProjectItemData ReadOffline(byte[] data)
        {
            return ADFile.FromBytes(data) as ProjectItemData;
        }

        public void BuildFromOffline(byte[] data,string path)
        {
            OfflineFile offlineFile = ADFile.FromBytes(data) as OfflineFile;
            offlineFile.ReleaseFile(path);
            foreach (var item in offlineFile.MainMapDatas)
            {
                App.instance.OnGenerate.Invoke(ReadOffline(item));
            }
        }
    }
}
