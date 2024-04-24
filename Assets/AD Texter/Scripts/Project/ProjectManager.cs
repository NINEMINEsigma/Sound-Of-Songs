using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AD.BASE;
using AD.Derivation.GameEditor;
using AD.Derivation.Performance;
using AD.Derivation.SceneTrans;
using AD.Sample.Texter.Project;
using AD.Utility;
using AD.Utility.Object;
using UnityEngine;

namespace AD.Sample.Texter
{
    [Serializable]
    public class PrefabModel : ADModel
    {
        [HideInInspector] public Transform Root;

        public ADSerializableDictionary<string, EditGroup> Prefabs = new();

        public ADSerializableDictionary<string, List<GameObject>> SubProjectItemPrefabs = new();

        public EditGroup ObtainInstance(string name)
        {
            return Prefabs[name].PrefabInstantiate();
        }

        public T ObtainInstance<T>(string name) where T : Component
        {
            return Prefabs[name].PrefabInstantiate<T, EditGroup>();
        }

        public override void Init()
        {
            //Not Init
        }

        public IADModel Load(string path)
        {
            throw new System.NotImplementedException();
        }

        public void Save(string path)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ProjectManager : ADController
    {

        public class ProjectLoadEntry : ADModel
        {
            public DataAssets Current;
            public ProjectLoadEntry PastInfo;

            public ProjectLoadEntry() : this(null, null) { }
            public ProjectLoadEntry(DataAssets current, ProjectLoadEntry pastInfo)
            {
                Current = current;
                PastInfo = pastInfo;
            }

            public ProjectLoadEntry GetNext(DataAssets next)
            {
                return new ProjectLoadEntry(next, this);
            }

            public static ProjectLoadEntry Temp(DataAssets next)
            {
                return new ProjectLoadEntry(next, null);
            }

            public override void Init()
            {
                //Not Init
            }

            public IADModel Load(string path)
            {
                throw new NotImplementedException();
            }

            public void Save(string path)
            {
                throw new NotImplementedException();
            }
        }

        public GameEditorApp UIApp => GameEditorApp.instance;

        [Header("Assets")]
        public Transform ProjectTransformRoot;
        public ProjectRoot ProjectRootMono;
        public CameraCore MainCameraCore;
        public TimeClocker ADGTimeC;
        public MainSceneLoader MainSceneLoaderManager;
        [Header("Prefab")]
        public PrefabModel ProjectPrefabModel;
         

        private void Start()
        {
            App.instance.RegisterController(this);
            App.instance.OnGenerate.AddListener(T => this.OnGenerate.Invoke(T));
            ADGlobalSystem.instance.IsAutoSaveArchitecturesDebugLog = true;
            ADGTimeC = ADGlobalSystem.instance.AutoSaveArchitecturesDebugLogTimeLimitCounter;
        }

        private void OnApplicationQuit()
        {
            App.instance.SaveRecord();
        }

        public override void Init()
        {
            Architecture.RegisterController(MainSceneLoaderManager);
            StartCoroutine(LoadEveryOne());
        }

        private IEnumerator LoadEveryOne()
        {
            while (!GameEditorApp.instance.Contains<Information>())
            {
                yield return null;  
            }
            Architecture.AddMessage("Start Loading Model");

            var assetsHeader = Architecture.GetModel<DataAssets>();
            if (Architecture.Contains<ProjectLoadEntry>())
            {
                //更新
                Architecture.RegisterModel<ProjectLoadEntry>(Architecture.GetModel<ProjectLoadEntry>().GetNext(assetsHeader));
            }
            else
            {
                Architecture.RegisterModel(ProjectLoadEntry.Temp(assetsHeader));
            }
            ProjectPrefabModel.Root = ProjectTransformRoot;
            Architecture.RegisterModel(ProjectPrefabModel);

            ProjectRootMono.Init();

            using ADFile file = new(new(Path.Combine(LoadingManager.FilePath, assetsHeader.AssetsName, "data.line"), ADStreamEnum.Location.File, ADStreamEnum.Format.LINE));
            if (FileC.FileExists(file.FilePath))
            {
                if (file.Deserialize(out ProjectItemData root, "master"))
                {
                    root.MatchProjectItem = ProjectRootMono;
                    yield return LoadSingle(root);
                }
                else throw new ADException("Load Failed", file.ErrorException);
            }
        }

        private IEnumerator LoadSingle(ProjectItemData data)
        {
            App.instance.OnGenerate.Invoke(data);
            foreach (var child in data.Childs)
            {
                yield return LoadSingle(child);
            }
        }

        public void SaveEveryOne(IProjectItem item)
        {
            item.ExecuteBeforeSave();
            foreach (var child in item.GetChilds().Contravariance<ICanSerializeOnCustomEditor, IProjectItem>())
            {
                SaveEveryOne(child);   
            }
        }

        [HideInInspector] public ProjectItemData CurrentRootData;

        public void SaveProjectData()
        {
            try
            {
                SaveEveryOne(ProjectRootMono);
                CurrentRootData = new ProjectItemData(ProjectRootMono);
                SaveProjectData(CurrentRootData);
                CurrentRootData = null;
                GameEditorApp
                    .instance
                    .GetSystem<GameEditorWindowGenerator>()
                    .ObtainElement(new Vector2(200, 0))
                    .SetTitle($"Finish");
            }
            catch (ADException ex)
            {
                GameEditorApp
                    .instance
                    .GetSystem<GameEditorWindowGenerator>()
                    .ObtainElement(new Vector2(400, 320))
                    .SetTitle($"AD : {ex.GetType().Name}")
                    .GenerateText("message", ex.Message, new Vector2(400, 320));
            }
            catch (Exception ex)
            {
                GameEditorApp
                    .instance
                    .GetSystem<GameEditorWindowGenerator>()
                    .ObtainElement(new Vector2(400, 320))
                    .SetTitle($"Unknown : {ex.GetType().Name}")
                    .GenerateText("message", ex.Message, new Vector2(400, 320));
            }
        }

        public void SaveProjectData(ProjectItemData root)
        {
            root.ExecuteBeforeSave();
            var assetsHeader = Architecture.GetModel<DataAssets>();
            string path = Path.Combine(LoadingManager.FilePath, assetsHeader.AssetsName, "data.line");
            using ADFile file = new(new(path, ADStreamEnum.Location.File, ADStreamEnum.Format.LINE));
            FileC.TryCreateDirectroryOfFile(path);
            if (!file.Serialize(root, "master"))
            {
                Architecture.AddMessage("Serialize Failed");
            }
        }

        public void BackToEntry()
        {
            ADGlobalSystem.instance.OnEnd();
        }

        public void LoadSubProject(DataAssets next)
        {
            Architecture.RegisterModel(next);
            ADGlobalSystem.instance.TargetSceneName = SceneExtension.GetCurrent().name;
            ADGlobalSystem.instance.OnEnd();
        }

        public void BackToPreviousScene()
        {
            ADGlobalSystem.instance.TargetSceneName = SceneExtension.GetCurrent().name;
            Architecture.RegisterModel(Architecture.GetModel<ProjectLoadEntry>());
            ADGlobalSystem.instance.OnEnd();
        }

        public ADEvent<ProjectItemData> OnGenerate = new();

        public GameObject LastFocusTarget;
        public void CatchItemByCameraCore(GameObject target, RayExtension.RayInfo info)
        {
            if (LastFocusTarget == target)
            {
                MainCameraCore.TryStartCoroutineMove();
                LastFocusTarget = null;
                if (target.transform.parent != null && target.transform.parent.TryGetComponent<ColliderLayer>(out var colliderLayer))
                {
                    if (colliderLayer.ParentGroup.gameObject.ObtainComponent(out IProjectItem item))
                    {
                        UIApp.GetController<Properties>().MatchTarget = item;
                        UIApp.GetController<Properties>().ClearAndRefresh();
                    }
                }
            }
            else LastFocusTarget = target;
        }

        //Offline

        public void LoadFromOfflineFile(OfflineFile offline)
        {
            ProjectItemData root = (ProjectItemData)ADFile.FromBytes(offline.MainMapDatas[0]);
            var assetsHeader = Architecture.GetModel<DataAssets>();
            offline.ReleaseFile(Path.Combine(LoadingManager.FilePath, assetsHeader.AssetsName));
            offline.Reconnect(root);
            SaveProjectData(root);
        }

        public void LoadFromOfflineFile(string path)
        {
            using ADFile file = new(path, false, true, true);
            OfflineFile offline = (OfflineFile)ADFile.FromBytes(file.FileData);
            LoadFromOfflineFile(offline);
            file.Dispose();
            App.instance.GetController<MainSceneLoader>().UnloadAll();
            ADGlobalSystem.instance.TargetSceneName = SceneExtension.GetCurrent().name;
            ADGlobalSystem.instance.OnEnd();
        }

        public void CreateOfflineFile(string path)
        {
            FileC.TryCreateDirectroryOfFile(path);
            OfflineFile offlineFile = new();
            offlineFile.Add(new ProjectItemData(ProjectRootMono).ExecuteBeforeSave());
            offlineFile.Build(path);
        }

        public void CreateOfflineFile()
        {
            CreateOfflineFile(Path.Combine(LoadingManager.FilePath, "Export", Architecture.GetModel<DataAssets>().AssetsName + ".offline"));
        }
    }
}
