﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CatLib;
using TinaX.Conf;
using TinaX.VFS;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace TinaX
{
    public class XCore
    {
        #region Instance

        private static XCore _instance;
        public static XCore I
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new XCore();
                }
                return _instance;
            }
        }
        public static XCore Instance
        {
            get { return I; }
        }
        
        #endregion

        #region Info
        public string version_name
        {
            get
            {
                return FrameworkInfo.FrameworkVersionName;
            }
        }

        public int version_code
        {
            get
            {
                return FrameworkInfo.FrameworkVersionCode;
            }
        }

        /// <summary>
        /// 框架的沙箱存储路径
        /// </summary>
        public string LocalStorage_TinaX
        {
            get
            {
                return UnityEngine.Application.persistentDataPath + "/" + Setup.Framework_LocalStorage_TinaX;
            }
        }

        /// <summary>
        /// App的沙箱存储路径（提供给框架使用开发者）
        /// </summary>
        public string LocalStorage_App
        {
            get
            {
                return UnityEngine.Application.persistentDataPath + "/" + Setup.Framework_LocalStorage_App;
            }
        }

        /// <summary>
        /// 框架的基础GameObject
        /// </summary>
        public GameObject BaseGameObject
        {
            get
            {
                return mBaseGameObject;
            }
        }

        

        #endregion

        #region Runtime
        private bool m_inited = false;
        private CatLib.Application m_catlib_app;
        private MainConfig mMainConfig;
        private GameObject mBaseGameObject;
        #endregion

        #region Action

        /// <summary>
        /// 框架软重启时触发的action，如果业务逻辑有需要的，在这时候顺带着处理下吧
        /// </summary>
        public static Action OnFrameworkRestart; 

        #endregion

        public XCore Init(MainConfig mainConfig)
        {
            if (m_inited) { return this; }
            m_inited = true;

            XLog.Print("[TinaX Framework] TinaX6 - v." + version_name + "    | Nekonya Studio | Corala.Space Project | Powerd by yomunsam - www.yomunchan.moe");

            mMainConfig = mainConfig;

            //生成一个全局的GameObject
            mBaseGameObject = GameObjectHelper.FindOrCreateGo(Setup.Framework_Base_GameObject)
                .DontDestroy()
                .SetPosition(new Vector3(-1000, -1000, -1000));

            //初始化配置与变量


            //启动引导系统
            m_catlib_app = new CatLib.Application();
            m_catlib_app.OnFindType((t) => Type.GetType(t));
            m_catlib_app.Bootstrap(new XBootstrap());
            m_catlib_app.Init();

            //管理器等初始化工作
            InitMgrs();

            //检查和处理自动更新
            HandleAutoUpgrade(() =>
            {
                //因为更新操作是异步的，所以接下来要执行的东西都在这个回调里

                StartupApp();
            });

            
            


            return this;
        }

        /// <summary>
        /// 软重启App
        /// </summary>
        /// <returns></returns>
        public XCore RestartApp()
        {
            Debug.Log("[TinaX] Framework开始软重启");

            _instance = null;
            m_inited = false;

            #region 处理其他的资源释放之类的


            #endregion

            App.Terminate(); //停用CatLib


            XStart.RestartFramework();

            if (OnFrameworkRestart != null)
            {
                OnFrameworkRestart();
            }
            return this;
        }



        private void InitMgrs()
        {
            XI18N.Instance.Start();
#if TinaX_CA_LuaRuntime_Enable
            LuaScript.I.Start();
#endif

        }

        private void StartupApp()
        {

#if TinaX_CA_LuaRuntime_Enable
            var luaConfig = Config.GetTinaXConfig<Lua.LuaConfig>(Conf.ConfigPath.lua);
            if (luaConfig != null)
            {
                if (!luaConfig.LuaScriptStartup.IsNullOrEmpty() && luaConfig.EnableLua)
                {
                    LuaScript.I.RunFile(luaConfig.LuaScriptStartup);
                }
            }
#endif

            if (mMainConfig.Startup_Scene != null && mMainConfig.Startup_Scene != "")
            {
                if (SceneMgr.Instance.GetActiveSceneName() != mMainConfig.Startup_Scene)
                {
                    SceneMgr.Instance.OpenScene(mMainConfig.Startup_Scene);
                }
            }

            
        }

        /// <summary>
        /// 处理自动更新
        /// </summary>
        /// <param name="OnFinish"></param>
        private void HandleAutoUpgrade(Action OnFinish)
        {

            var mUpgradeConfig = TinaX.Config.GetTinaXConfig<TinaX.Upgrade.UpgradeConfig>(TinaX.Conf.ConfigPath.upgrade);
            if (mUpgradeConfig == null)
            {
                OnFinish();
                return;
            }

            if (!mUpgradeConfig.Auto_Upgrade)
            {
                //不需要框架层处理自动更新
                OnFinish();
                return;
            }


#if UNITY_EDITOR
            //编辑器模式下，检查下要不要检查更新：如果使用AssetBundle包模拟加载，则检查更新，否则检查个锤子
            //编辑器模式下，需要判断，是从哪儿加载资源
            if (!Menu.GetChecked(Const.AssetSystemConst.menu_editor_load_from_asset_pack_name))
            {
                //直接使用编辑器加载策略
                //这种情况下，不用检查更新
                OnFinish();
                XLog.Print("Framework 热更新：在"+XLog.GetColorString_Blue("编辑器模式") +"下且未采用资源包方式加载资源，故忽略热更新检查。");
                return;
            }

#endif

            TinaX.Upgrade.AutoUpgradeUI_Mgr.Start((res)=> {


                OnFinish();
            });

        }

    }
}
