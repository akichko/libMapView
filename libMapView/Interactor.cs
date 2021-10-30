﻿/*============================================================================
MIT License

Copyright (c) 2021 akichko

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using Akichko.libGis;

namespace Akichko.libMapView
{
    public class InteractorMgr : IInputBoundary
    {
        Interactor front;
        Interactor background;
        protected IOutputBoundary presenter;
        //protected InteractorSettings settings;
        ViewParam viewParam;

        Interactor currentInteractor;
        SemaphoreSlim semaphoPainting = new SemaphoreSlim(1, 1);

        public InteractorMgr(IOutputBoundary presenter, InteractorSettings settings)
        {
            front = new Interactor(presenter, settings);
            currentInteractor = front;
        }


        public void OpenFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi)
        {
            front.OpenFile(fileName, mapMgr, drawApi);
        }

        public void OpenBgFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi, IOutputBoundary presenter, InteractorSettings settingsBg)
        {
            background = new Interactor(presenter, settingsBg);
            background.SetViewParam(viewParam);
            background.OpenFile(fileName, mapMgr, drawApi);
        }

        public void Shutdown()
        {
            front?.Shutdown();
            background?.Shutdown();
        }

        public void SetViewCenter(LatLon latlon)
        {
            front?.SetViewCenter(latlon);
            background?.SetViewCenter(latlon);
        }

        public void SetSettings(InteractorSettings settings)
        {
            currentInteractor.SetSettings(settings);
            RefreshDrawArea();
        }

        public void SetViewParam(ViewParam viewParam)
        {
            this.viewParam = viewParam;
            front?.SetViewParam(viewParam);
            background?.SetViewParam(viewParam);
        }

        public void SetBoundaryGeometry(List<LatLon[]> boundaryList)
        {
            currentInteractor.SetBoundaryGeometry(boundaryList);
            RefreshDrawArea();
        }

        public void SetClickedLatLon(LatLon clickedLatLon)
        {
            currentInteractor.SetClickedLatLon(clickedLatLon);
            RefreshDrawArea();
        }

        public void SetRouteGeometry(LatLon[] routeGeometry)
        {
            currentInteractor.SetRouteGeometry(routeGeometry);
            RefreshDrawArea();
        }

        public void SetSelectedAttr(CmnObjHandle attrObjHdl)
        {
            currentInteractor.SetSelectedAttr(attrObjHdl);
            RefreshDrawArea();
        }

        public void SetSelectedLatLon(LatLon latlon)
        {
            currentInteractor.SetSelectedLatLon(latlon);
            RefreshDrawArea();
        }

        public void Paint()
        {
            //semaphoPainting.Wait();
            
            if(background == null)
            {
                front.Paint();
            }
            else
            {
                int ret1 = background?.MakeImage() ?? -1;
                if (ret1 < 0)
                    return;

                int ret2 = front.MakeImage(background?.presenter);
                if (ret2 < 0)
                    return;

                front.UpdateImage();
            }
            //semaphoPainting.Release();
        }

        public void RefreshDrawArea()
        {
            background?.RefreshDrawArea();
            front.RefreshDrawArea();
        }

        public CmnObjHandle SearchObject(LatLon baseLatLon)
        {
            return currentInteractor.SearchObject(baseLatLon);
        }

        public CmnObjHandle SearchObject(uint tileId, uint objType, ulong objId)
        {
            return currentInteractor.SearchObject(tileId, objType, objId);
        }

        public CmnObjHandle SearchObject(CmnSearchKey key)
        {
            return currentInteractor.SearchObject(key);
        }

        public CmnObjHandle SearchAttrObject(CmnSearchKey key)
        {
            return currentInteractor.SearchAttrObject(key);
        }

        public RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
        {
            return currentInteractor.CalcRoute(orgLatLon, dstLatLon);
        }

        public async Task<int> RefreshMapCacheAsync()
        {
            try
            {
                await front.RefreshMapCacheAsync().ConfigureAwait(false);
                if (background != null)
                    await background.RefreshMapCacheAsync().ConfigureAwait(false);
            }
            catch(Exception e)
            {
                throw new ApplicationException(e.Message, e.InnerException);
            }
            return 0;
        }
    }

    public class Interactor// : IInputBoundary
    {
        protected CmnMapMgr mapMgr;
        protected ViewParam viewParam;
        public IOutputBoundary presenter;
        //Presenter presenter;

        //動作設定
        protected InteractorSettings settings;

        protected CmnRouteMgr routeMgr;

        SemaphoreSlim semaphoReloading = new SemaphoreSlim(1, 1);

        //制御用
        protected InteractorStatus status;

        /* 起動・設定・終了 ***********************************************/

        public Interactor(IOutputBoundary presenter, InteractorSettings settings)
        {
            this.presenter = presenter;
            this.settings = settings;
            this.status = new InteractorStatus();
        }


        public void OpenFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi)
        {
            this.mapMgr = mapMgr;
            mapMgr.Connect(fileName);
            presenter.SetDrawInterface(drawApi);

            //routeMgr = new RouteMgr(MapDataType.MapManager);

            //RefreshMapCache();
            status.drawEnable = true;
            status.isReloadNeeded = true;
            RefreshDrawArea();
        }


        public void SetRouteMgr(CmnRouteMgr routeMgr)
        {
            this.routeMgr = routeMgr;
            this.routeMgr.SetMapMgr(mapMgr);
        }

        public void Shutdown()
        {
            if (mapMgr != null)
                mapMgr.Disconnect();
        }

        public void SetSettings(InteractorSettings settings)
        {
            this.settings = settings;
        }


        /* 描画 ***********************************************/

        public int PaintAppend(IOutputBoundary prePresenter = null)
        {
            if (!status.drawEnable || !status.isPaintNeeded)
                return -1;
            status.isPaintNeeded = false;

            int timeS = Environment.TickCount;

            //描画対象タイルを特定
            List<CmnTile> drawAreaTileList = CalcDrawAreaTileList();

            //描画
            presenter.InitializeGraphics(settings, viewParam, prePresenter);
            DrawMap(drawAreaTileList, viewParam, settings.drawMapObjFilter, settings.timeStamp);

            UpdateImage();

            int exeTime = Environment.TickCount - timeS;
            presenter.PrintLog(1, $"Paint:{exeTime}");

            return 0;
        }

        public int MakeImage(IOutputBoundary prePresenter = null)
        {
            if (!status.drawEnable || !status.isPaintNeeded)
                return -1;
            status.isPaintNeeded = false;

            int timeS = Environment.TickCount;

            //描画対象タイルを特定
            List<CmnTile> drawAreaTileList = CalcDrawAreaTileList();

            //描画
            presenter.InitializeGraphics(settings, viewParam, prePresenter);
            DrawMap(drawAreaTileList, viewParam, settings.drawMapObjFilter, settings.timeStamp);

            //UpdateImage();

            int exeTime = Environment.TickCount - timeS;
            //presenter.PrintLog(1, $"Paint:{exeTime}");

            return 0;
        }

        public void UpdateImage() => presenter.UpdateImage();

        public int Paint(IInputBoundary preInput = null)
        {
            int ret = MakeImage();
            if(ret == 0)
                UpdateImage();

            return 0;
        }


        protected List<CmnTile> CalcDrawAreaTileList()
        {
            //描画対象タイルを特定
            uint centerTileId = mapMgr.tileApi.CalcTileId(viewParam.viewCenter);
            IEnumerable<CmnTile> drawTileList = mapMgr.SearchTiles(centerTileId, settings.tileDrawDistanceX, settings.tileDrawDistanceY);

            //描画エリア内タイル抽出（隣接１メッシュ）
            TileXYL xylNW = mapMgr.tileApi.CalcTileXYL(viewParam.GetDrawAreaRectPos(RectPos.NorthWest));
            TileXYL xylSE = mapMgr.tileApi.CalcTileXYL(viewParam.GetDrawAreaRectPos(RectPos.SouthEast));

            List<CmnTile> drawAreaTileList = drawTileList
                .Where(x => CheckInDrawArea(x.TileId, xylNW, xylSE, 0))
                .ToList();

            return drawAreaTileList;
        }

        //描画メイン
        public void DrawMap(List<CmnTile> tileList, ViewParam viewParam, CmnObjFilter filter, long timeStamp = -1)
        {
            //設定
            //presenter.SetViewSettings(settings);

            //Graphics初期化
            //presenter.InitializeGraphics(settings, viewParam);

            //背景形状を描画
            presenter.DrawBackGround(viewParam);

            //各タイルを描画
            presenter.DrawTiles(tileList, filter, viewParam, timeStamp);

            //タイル枠描画
            presenter.DrawTileBorder(tileList, viewParam);

            //選択座標点追加描画
            presenter.DrawPoint(status.clickedLatLon, viewParam, PointType.Clicked);
            presenter.DrawPoint(status.nearestLatLon, viewParam, PointType.Nearest);
            presenter.DrawPoint(status.selectedLatLon, viewParam, PointType.Selected);

            //ルート形状描画
            presenter.DrawRouteGeometry(status.routeGeometry, viewParam);

            //中心十字描画
            presenter.DrawCenterMark(viewParam);
        }

        //public void DrawMap(List<CmnTile> tileList, ViewParam viewParam, CmnObjFilter filter, long timeStamp = -1)
        //{
        //    //int timeS = Environment.TickCount;
        //    //設定
        //    //presenter.SetViewSettings(settings);

        //    //Graphics初期化
        //    presenter.InitializeGraphics(settings, viewParam);

        //    //背景形状を描画
        //    presenter.DrawBackGround(viewParam);

        //    //各タイルを描画
        //    presenter.DrawTiles(tileList, filter, viewParam, timeStamp);

        //    //int timeDrawMap = Environment.TickCount - timeS;

        //    //タイル枠描画
        //    presenter.DrawTileBorder(tileList, viewParam);

        //    //選択座標点追加描画
        //    presenter.DrawPoint(status.clickedLatLon, viewParam, PointType.Clicked);
        //    presenter.DrawPoint(status.nearestLatLon, viewParam, PointType.Nearest);
        //    presenter.DrawPoint(status.selectedLatLon, viewParam, PointType.Selected);

        //    //ルート形状描画
        //    presenter.DrawRouteGeometry(status.routeGeometry, viewParam);

        //    //中心十字描画
        //    presenter.DrawCenterMark(viewParam);

        //    //描画エリア更新
        //    presenter.UpdateImage();
        //    //int timeUpdateImage = Environment.TickCount - timeS;


        //    //presenter.PrintLog(2, $"Draw:{timeDrawMap},ImgUpdate:{timeUpdateImage}");
        //}

        public void RefreshDrawArea()
        {
            status.isPaintNeeded = true;
            presenter.RefreshDrawArea();
        }

        bool CheckInDrawArea(uint tileId, TileXYL xylNW, TileXYL xylSE, int range)
        {
            TileXYL xyl = mapMgr.tileApi.CalcTileXYL(tileId);

            if (xyl.x < xylNW.x - range || xyl.x > xylSE.x + range || xyl.y < xylSE.y - range || xyl.y > xylNW.y + range)
                return false;
            else
                return true;
        }

        /* データ管理 ***********************************************/

        public async Task<int> RefreshMapCacheAsync()
        {
            if (!status.drawEnable || !status.isReloadNeeded)
                return 0;
            status.isReloadNeeded = false;

            await semaphoReloading.WaitAsync().ConfigureAwait(false);

            int timeS = Environment.TickCount;

            //test
            //throw new ApplicationException("E100", new NotImplementedException("E10"));

            if (settings.isAllTileReadMode)
            {
                if (!status.isAllTileLoaded)
                {
                    status.isAllTileLoaded = true;

                    //全地図データロード
                    List<uint> tileList = mapMgr.GetMapTileIdList();

                    foreach (uint tileId in tileList)
                    {
                        mapMgr.LoadTile(tileId, null);
                    }
                }
            }
            else
            {
                status.isAllTileLoaded = false;


                //座標周辺の地図をロード
                uint centerTileId = mapMgr.tileApi.CalcTileId(viewParam.viewCenter);
                List<uint> tileIdList = mapMgr.tileApi.CalcTileIdAround(centerTileId, settings.tileLoadDistanceX, settings.tileLoadDistanceY);

#if false //同期
                    foreach (uint tileId in tileIdList)
                    {
                        mapMgr.LoadTile(tileId, null);
                    }
#else //非同期
                var tasks = tileIdList.Select(x => mapMgr.LoadTileAsync(x, null)).ToArray();

                //スレッドセーフ懸念 ＆ 描画イベント不足
                //Task.WaitAll(tasks);
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch(Exception e)
                {
                    throw new ApplicationException(e.Message, e.InnerException);
                }
#endif
                //遠くの地図をアンロード

                IEnumerable<CmnTile> tileList = mapMgr.GetLoadedTileList();
                foreach (CmnTile tile in tileList)
                {
                    TileXY offset = mapMgr.tileApi.CalcTileAbsOffset(centerTileId, tile.TileId);
                    if (offset.x > settings.tileReleaseDistanceX || offset.y > settings.tileReleaseDistanceY)
                        mapMgr.UnloadTile(tile.TileId);
                }

            }

            int exeTime = Environment.TickCount - timeS;
            presenter.PrintLog(0, $"Reload:{exeTime}");

            RefreshDrawArea();
            semaphoReloading.Release();

            return 0;
        }

        public void ReloadMap()
        {
            status.isReloadNeeded = true;
            presenter.RefreshDrawArea();
        }

        private void UpdateCurrentTileId()
        {
            uint newTileId = mapMgr?.tileApi.CalcTileId(viewParam.viewCenter) ?? 0;
            if (status.currentTileId != newTileId)
            {
                status.currentTileId = newTileId;
                status.isReloadNeeded = true;
            }
        }

        /* 検索 ***********************************************/


        public CmnObjHandle SearchObject(LatLon baseLatLon)
        {
            if (!status.drawEnable)
                return null;

            status.selectedHdl = mapMgr.SearchObj(baseLatLon, settings.drawMapObjFilter, settings.ClickSearchRange, settings.timeStamp);
            if (status.selectedHdl == null)
                return null;

            PolyLinePos nearestPos = LatLon.CalcNearestPoint(baseLatLon, status.selectedHdl?.Geometry);

            presenter.SetSelectedObjHdl(status.selectedHdl, nearestPos);
            presenter.ShowAttribute(status.selectedHdl);
            this.status.nearestLatLon = nearestPos.latLon;

            SearchRelatedObject(status.selectedHdl);

            return status.selectedHdl;
        }

        public CmnObjHandle SearchObject(uint tileId, uint objType, UInt64 objId)
        {
            if (!status.drawEnable)
                return null;

            status.selectedHdl = mapMgr.SearchObj(tileId, objType, objId);
            presenter.SetSelectedObjHdl(status.selectedHdl);
            presenter.ShowAttribute(status.selectedHdl);

            SearchRelatedObject(status.selectedHdl);

            return status.selectedHdl;
        }

        public CmnObjHandle SearchObject(uint tileId, uint objType, UInt16 objIndex)
        {
            if (!status.drawEnable)
                return null;

            status.selectedHdl = mapMgr.SearchObj(tileId, objType, objIndex);
            presenter.SetSelectedObjHdl(status.selectedHdl);
            presenter.ShowAttribute(status.selectedHdl);

            SearchRelatedObject(status.selectedHdl);

            return status.selectedHdl;
        }

        public CmnObjHandle SearchObject(CmnSearchKey key)
        {
            if (!status.drawEnable)
                return null;

            status.selectedHdl = mapMgr.SearchObj(key);
            presenter.SetSelectedObjHdl(status.selectedHdl);
            presenter.ShowAttribute(status.selectedHdl);

            SearchRelatedObject(status.selectedHdl);

            return status.selectedHdl;
        }

        void SearchRelatedObject(CmnObjHandle selectedHdl)
        {
            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl)?.Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);
        }

        public CmnObjHandle SearchAttrObject(CmnSearchKey key)
        {
            CmnObjHandle attrHdl = mapMgr.SearchObj(key);

            return attrHdl;
        }


        /* 情報保持 ***********************************************/

        public void SetSelectedLatLon(LatLon latlon)
        {
            this.status.selectedLatLon = latlon;
            //presenter.SetSelectedLatLon(latlon);
            status.isPaintNeeded = true;
        }

        public void SetNearestObj(PolyLinePos nearestPos)
        {
            this.status.nearestLatLon = nearestPos.latLon;
            status.isPaintNeeded = true;
        }

        public void SetRouteGeometry(LatLon[] routeGeometry)
        {
            this.status.routeGeometry = routeGeometry;
            //presenter.SetRouteGeometry(routeGeometry);
        }

        public void SetBoundaryGeometry(List<LatLon[]> boundaryList)
        {
            presenter.SetBoundaryList(boundaryList);
        }

        public void SetClickedLatLon(LatLon clickedLatLon)
        {
            this.status.clickedLatLon = clickedLatLon;
            presenter.UpdateClickedLatLon(clickedLatLon);
        }

        public void SetSelectedAttr(CmnObjHandle selectedAttr)
        {
            presenter.SetSelectedAttr(selectedAttr);
            status.isPaintNeeded = true;
        }


        //public void SetRouteObjList(List<CmnDirObjHandle> routeObjList)
        //{
        //    presenter.SetRouteObjList(routeObjList);
        //}



        /* 描画用パラメータ変更 ***********************************************/

        public void SetViewParam(ViewParam viewParam)
        {
            this.viewParam = viewParam;
            // presenter.viewParam = viewParam;
            UpdateCurrentTileId();

        }


        public void SetViewCenter(LatLon latlon)
        {
            viewParam.SetViewCenter(latlon);
            UpdateCurrentTileId();
        }

        //public void MoveViewCenter(LatLon relLatLon)
        //{
        //    viewParam.MoveViewCenter(relLatLon);
        //    presenter.UpdateCenterLatLon(viewParam.viewCenter);
        //    UpdateCurrentTileId();
        //}

        //public void MoveViewCenter(int x, int y)
        //{
        //    viewParam.MoveViewCenter(x, y);
        //    //presenter.UpdateCenterTileId(mapMgr.tileApi.CalcTileId(viewParam.viewCenter));
        //    presenter.UpdateCenterLatLon(viewParam.viewCenter);
        //    UpdateCurrentTileId();
        //}

        //public void ChangeZoom(double delta, int x, int y)
        //{

        //    LatLon clickedLatLon = GetLatLon(x, y);

        //    viewParam.Zoom *= delta;

        //    LatLon afterLatLon = GetLatLon(x, y);

        //    //マウス位置保持のための移動
        //    LatLon relLatLon = new LatLon(clickedLatLon.lat - afterLatLon.lat, clickedLatLon.lon - afterLatLon.lon);
        //    MoveViewCenter(relLatLon);

        //    presenter.UpdateCenterLatLon(viewParam.viewCenter);

        //}




        /* その他 ***********************************************/


        public virtual RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
        {
            CmnRouteMgr routeMgr = mapMgr.CreateRouteMgr();

            routeMgr.orgLatLon = orgLatLon;
            routeMgr.dstLatLon = dstLatLon;

            //Prepare
            routeMgr.Prepare(false);

            //計算
            RouteResult routeCalcResult = routeMgr.CalcRoute();

            if (routeCalcResult.resultCode != ResultCode.Success)
            {
                SetRouteGeometry(null);
                return routeCalcResult;
            }

            status.route = routeCalcResult.route.Select(x => x.DLinkHdl);
            presenter.OutputRoute(status.route);

            SetRouteGeometry(routeMgr.GetResult());

            return routeCalcResult;
        }



        public void ShowAttribute()
        { }

    }

    public interface IInputBoundary
    {
        //開始・終了
        void OpenFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi);
        //void OpenBgFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi, InteractorSettings settings);
        void OpenBgFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi, IOutputBoundary presenter, InteractorSettings settingsBg);
        void Shutdown();

        //ビュー設定
        void SetViewCenter(LatLon latlon);
        void SetSettings(InteractorSettings settings);
        void SetViewParam(ViewParam viewParam);

        //描画設定
        void SetBoundaryGeometry(List<LatLon[]> boundaryList);
        void SetClickedLatLon(LatLon clickedLatLon);
        void SetRouteGeometry(LatLon[] routeGeometry);
        void SetSelectedAttr(CmnObjHandle attrObjHdl);
        void SetSelectedLatLon(LatLon latlon);
        //void SetNearestObj(PolyLinePos nearestPos);

        //描画
        void Paint();
        void RefreshDrawArea();

        //検索
        CmnObjHandle SearchObject(LatLon baseLatLon);
        CmnObjHandle SearchObject(uint tileId, uint objType, ulong objId);
        CmnObjHandle SearchObject(CmnSearchKey key);
        CmnObjHandle SearchAttrObject(CmnSearchKey key);
        RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon);

        Task<int> RefreshMapCacheAsync();
    }

    public interface IOutputBoundary
    {
        //設定
        void SetDrawInterface(CmnDrawApi drawApi);
        //void SetDrawBgInterface(CmnDrawApi drawBgApi);
        void SetViewSettings(InteractorSettings settings);

        //地図描画
        void InitializeGraphics(InteractorSettings settings, ViewParam viewParam, IOutputBoundary presenter = null);
        void DrawBackGround(ViewParam viewParam);
        void DrawTiles(List<CmnTile> tileList, CmnObjFilter filter, ViewParam viewParam, long timeStamp = -1);
        void DrawTileBorder(List<CmnTile> tileList, ViewParam viewParam);
        void DrawPoint(LatLon latlon, ViewParam viewParam, PointType type = PointType.None);
        void DrawRouteGeometry(LatLon[] routeGeometry, ViewParam viewParam);
        void DrawCenterMark(ViewParam viewParam);
        void UpdateImage();
        //int DrawMapObj(CmnObjHandle cmnObjHandle, ViewParam viewParam);
        void RefreshDrawArea();

        //属性
        void UpdateCenterLatLon(LatLon latlon);
        void UpdateCenterTileId(uint tileId);
        void ShowAttribute(CmnObjHandle objHdl);
        void SetSelectedObjHdl(CmnObjHandle objHdl, PolyLinePos nearestPos = null);
        void SetSelectedAttr(CmnObjHandle selectedAttr);
        void SetRelatedObj(List<CmnObjHdlRef> relatedHdlList);
        void UpdateClickedLatLon(LatLon clickedLatLon);
        void SetBoundaryList(List<LatLon[]> boundaryList);
        //void SetRouteGeometry(LatLon[] routeGeometry);
        void OutputRoute(IEnumerable<CmnObjHandle> route);
        //void SetSelectedLatLon(LatLon latlon);

        //ログ
        void PrintLog(int logType, string logStr);
    }

    public class InteractorSettings
    {
        //読み込み
        public int tileLoadDistanceX = 3;
        public int tileLoadDistanceY = 2;
        public int tileReleaseDistanceX = 15;
        public int tileReleaseDistanceY = 10;
        public bool isAllTileReadMode = false;

        //検索
        public int ClickSearchRange = 1; //無制限ならint.MaxValue

        //描画
        public int tileDrawDistanceX = 2;
        public int tileDrawDistanceY = 1;
        public bool isTileBorderDisp = true;
        public bool isOneWayDisp = false;
        public bool isAdminBoundaryDisp = true;
        public bool isCenterMarkDisp = false;

        //フィルタ
        public CmnObjFilter drawMapObjFilter = null;
        public long timeStamp;

        public InteractorSettings()
        {
            drawMapObjFilter = new CmnObjFilter();
        }

        public void SetMapSubType(uint type, ushort subType)
        {
            drawMapObjFilter = new CmnObjFilter();
            drawMapObjFilter.AddRule(type, (new ListFilter<ushort>(false)).AddList(subType));


            //if (drawMapSubType == null)
            //    drawMapSubType = new Dictionary<uint, ushort>();
            //drawMapSubType[type] = subType;
        }
    }

    public class InteractorStatus
    {
        public bool drawEnable = false;
        public bool isPaintNeeded = true;

        public bool isReloadNeeded = false;
        public bool isAllTileLoaded = false;

        public byte currentTileLv;
        public uint currentTileId;

        //保存パラメータ
        public CmnObjHandle selectedHdl;
        public LatLon selectedLatLon;
        public LatLon clickedLatLon;
        public LatLon nearestLatLon;
        public IEnumerable<CmnObjHandle> route;
        public LatLon[] routeGeometry;
    }

    public enum PointType
    {
        None,
        Clicked,
        Nearest,
        Selected
    }

}
