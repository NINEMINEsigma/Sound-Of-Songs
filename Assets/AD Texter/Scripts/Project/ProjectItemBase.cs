using System.Collections.Generic;
using AD.Derivation.GameEditor;
using System;
using AD.Utility.Object;
using AD.BASE;

namespace AD.Sample.Texter
{
    public interface IProjectItem : ICanSerializeOnCustomEditor
    {
        bool IsAbleDisplayedOnHierarchyAtTheSameTime(Type type);
        void ExecuteBeforeSave();

        void ReDrawLine();

        EditGroup MyEditGroup { get; }
    }

    public interface IProjectItemWhereNeedInitData : IProjectItem, ICanInitialize
    {
        ProjectItemData SourceData { get; set; }
        string ProjectItemBindKey { get; }
    }

    public interface IProjectItemRoot: IProjectItem
    {

    }
}
