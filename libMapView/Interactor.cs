/*============================================================================
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
using System.Drawing;
using Akichko.libGis;

namespace Akichko.libMapView
{
    public class Interactor : IInputBoundary
    {
        protected CmnMapMgr mapMgr;
        protected ViewParam viewParam;
        protected IOutputBoundary presenter;
        //Presenter presenter;

        //動作設定
        protected InteractorSettings settings;

        protected CmnRouteMgr routeMgr;

        //制御用
        protected bool drawEnable = false;
        protected bool isPaintNeeded = true;
        protected bool isAllTileLoaded = false;

        protected byte currentTileLv;
        protected uint currentTileId;
        protected bool currentTileChanged = false;

        //保存パラメータ
        protected CmnObjHandle selectedHdl;
        protected LatLon selectedLatLon;
        protected LatLon clickedLatLon;
        protected LatLon nearestLatLon;
        protected IEnumerable<CmnObjHandle> route;

        /* 起動・設定・終了 ***********************************************/

        public Interactor(IOutputBoundary presenter, InteractorSettings settings)
        {
            this.presenter = presenter;
            this.settings = settings;
        }


        public void OpenFile(string fileName, CmnMapMgr mapMgr, CmnDrawApi drawApi)
        {
            this.mapMgr = mapMgr;
            mapMgr.Connect(fileName);
            presenter.SetDrawInterface(drawApi);

            //routeMgr = new RouteMgr(MapDataType.MapManager);

            //RefreshMapCache();
            drawEnable = true;
            currentTileChanged = true;
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


        /* データ管理 ***********************************************/

        private async Task<int> RefreshMapCacheAsync()
        {
            if (settings.isAllTileReadMode)
            {
                if (!isAllTileLoaded)
                {

                    //全地図データロード
                    List<uint> tileList = mapMgr.GetMapTileIdList();

                    foreach (uint tileId in tileList)
                    {
                        mapMgr.LoadTile(tileId, null);
                    }
                    isAllTileLoaded = true;
                }
            }
            else
            {
                isAllTileLoaded = false;

                if (currentTileChanged)
                {
                    currentTileChanged = false;

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
                    await Task.WhenAll(tasks).ConfigureAwait(false);
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
            }
            return 0;
        }

        /* 検索 ***********************************************/



        public CmnObjHandle SearchObject(LatLon baseLatLon)
        {
            if (!drawEnable)
                return null;

            selectedHdl = mapMgr.SearchObj(baseLatLon, settings.drawMapObjFilter, settings.ClickSearchRange, settings.timeStamp);
            if (selectedHdl == null)
                return null;

            PolyLinePos nearestPos = LatLon.CalcNearestPoint(baseLatLon, selectedHdl?.Geometry);

            presenter.SetSelectedObjHdl(selectedHdl, nearestPos);
            presenter.ShowAttribute(selectedHdl);
            this.nearestLatLon = nearestPos.latLon;

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl)?.Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);

            return selectedHdl;
        }

        public CmnObjHandle SearchObject(uint tileId, uint objType, UInt64 objId)
        {
            if (!drawEnable)
                return null;

            selectedHdl = mapMgr.SearchObj(tileId, objType, objId);
            presenter.SetSelectedObjHdl(selectedHdl);
            presenter.ShowAttribute(selectedHdl);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl)?.Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);

            return selectedHdl;
        }

        public CmnObjHandle SearchObject(uint tileId, uint objType, UInt16 objIndex)
        {
            if (!drawEnable)
                return null;

            selectedHdl = mapMgr.SearchObj(tileId, objType, objIndex);
            presenter.SetSelectedObjHdl(selectedHdl);
            presenter.ShowAttribute(selectedHdl);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl)?.Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);

            return selectedHdl;
        }

        public CmnObjHandle SearchObject(CmnSearchKey key)
        {
            if (!drawEnable)
                return null;

            selectedHdl = mapMgr.SearchObj(key);
            presenter.SetSelectedObjHdl(selectedHdl);
            presenter.ShowAttribute(selectedHdl);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl)?.Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);

            return selectedHdl;
        }

        public CmnObjHandle SearchAttrObject(CmnSearchKey key)
        {
            CmnObjHandle attrHdl = mapMgr.SearchObj(key);

            return attrHdl;
        }



        /* 描画 ***********************************************/

        public async Task<int> PaintAsync()
        {
            if (!drawEnable || !isPaintNeeded)
                return 0;
            isPaintNeeded = false;

            int timeS = Environment.TickCount;

            //タイル読み込み・解放
            var task = RefreshMapCacheAsync();
            int timeRefreshCache = Environment.TickCount - timeS;

            //描画対象タイルを特定
            uint centerTileId = mapMgr.tileApi.CalcTileId(viewParam.viewCenter);
            IEnumerable<CmnTile> drawTileList = mapMgr.SearchTiles(centerTileId, settings.tileDrawDistanceX, settings.tileDrawDistanceY);

            //描画エリア内タイル抽出（隣接１メッシュ）
            TileXYL xylNW = mapMgr.tileApi.CalcTileXYL(viewParam.GetDrawAreaRectPos(ERectPos.NorthWest));
            TileXYL xylSE = mapMgr.tileApi.CalcTileXYL(viewParam.GetDrawAreaRectPos(ERectPos.SouthEast));

            List<CmnTile> drawTileList2 = drawTileList
                .Where(x => CheckInDrawArea(x.TileId, xylNW, xylSE, 0))
                .ToList();


            //同期　⇒しないとスレッドセーフ懸念 ＆ 描画イベント不足
            //task.Wait();

            //各タイルを描画
            DrawTile(drawTileList2, viewParam, settings.drawMapObjFilter, settings.timeStamp);

            int timeE = Environment.TickCount - timeS;

            presenter.PrintLog(0, $"{timeRefreshCache},{timeE}");

            await task;

            return 0;
        }

        bool CheckInDrawArea(uint tileId, TileXYL xylNW, TileXYL xylSE, int range)
        {
            TileXYL xyl = mapMgr.tileApi.CalcTileXYL(tileId);

            if (xyl.x < xylNW.x - range || xyl.x > xylSE.x + range || xyl.y < xylSE.y - range || xyl.y > xylNW.y + range)
                return false;
            else
                return true;
        }


        //描画メイン
        public void DrawTile(List<CmnTile> tileList, ViewParam viewParam, CmnObjFilter filter, int timeStamp)
        {
            int timeS = Environment.TickCount;
            //設定
            presenter.SetViewSettings(settings);

            //Graphics初期化
            presenter.InitializeGraphics(viewParam);
            int timeGInit = Environment.TickCount - timeS;

            //背景形状を描画
            presenter.DrawBackGround(viewParam);

            //各タイルを描画
            presenter.DrawMap(tileList, filter, timeStamp, viewParam);
            int timeDrawMap = Environment.TickCount - timeS;

            //タイル枠描画
            presenter.DrawTileBorder(tileList, viewParam);

            //選択座標点追加描画
            presenter.DrawPoint(selectedLatLon, viewParam);
            presenter.DrawPoint(clickedLatLon, viewParam);
            presenter.DrawPoint(nearestLatLon, viewParam);

            //ルート形状描画
            presenter.DrawRouteGeometry(viewParam);

            //中心十字描画

            //描画エリア更新
            presenter.UpdateImage();
            int timeUpdateImage = Environment.TickCount - timeS;


            presenter.PrintLog(1, $"{timeGInit},{timeDrawMap},{timeUpdateImage}");
        }

        public void RefreshDrawArea()
        {
            isPaintNeeded = true;
            presenter.RefreshDrawArea();
        }

        public void SetSelectedLatLon(LatLon latlon)
        {
            this.selectedLatLon = latlon;
            //presenter.SetSelectedLatLon(latlon);
            isPaintNeeded = true;
        }

        public void SetNearestObj(PolyLinePos nearestPos)
        {
            this.nearestLatLon = nearestPos.latLon;
            isPaintNeeded = true;
        }

        public void SetRouteGeometry(LatLon[] routeGeometry)
        {
            presenter.SetRouteGeometry(routeGeometry);
        }

        public void SetBoundaryGeometry(List<LatLon[]> boundaryList)
        {
            presenter.SetBoundaryList(boundaryList);
        }

        public void SetClickedLatLon(LatLon clickedLatLon)
        {
            this.clickedLatLon = clickedLatLon;
            presenter.UpdateClickedLatLon(clickedLatLon);
        }

        public void SetSelectedAttr(CmnObjHandle selectedAttr)
        {
            presenter.SetSelectedAttr(selectedAttr);
            isPaintNeeded = true;
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


        /* テキスト表示 ***********************************************/

        private void UpdateCurrentTileId()
        {
            uint newTileId = mapMgr?.tileApi.CalcTileId(viewParam.viewCenter) ?? 0;
            if (currentTileId != newTileId)
            {
                currentTileId = newTileId;
                currentTileChanged = true;
            }
        }


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

            route = routeCalcResult.route.Select(x => x.DLinkHdl);
            presenter.OutputRoute(route);

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
        void Shutdown();

        //ビュー設定
        void SetViewCenter(LatLon latlon);
        void SetSettings(InteractorSettings settings);
        void SetViewParam(ViewParam viewParam);

        //描画設定
        void SetBoundaryGeometry(List<LatLon[]> boundaryList);
        void SetClickedLatLon(LatLon clickedLatLon);
        //void SetDrawInterface(CmnDrawApi drawApi);
        void SetRouteGeometry(LatLon[] routeGeometry);
        void SetSelectedAttr(CmnObjHandle attrObjHdl);
        void SetSelectedLatLon(LatLon latlon);
        //void SetNearestObj(PolyLinePos nearestPos);

        //描画
        Task<int> PaintAsync();
        void RefreshDrawArea();

        //検索
        CmnObjHandle SearchObject(LatLon baseLatLon);
        CmnObjHandle SearchObject(uint tileId, uint objType, ulong objId);
        CmnObjHandle SearchObject(CmnSearchKey key);
        CmnObjHandle SearchAttrObject(CmnSearchKey key);
        RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon);

    }

    public interface IOutputBoundary
    {
        //設定
        void SetDrawInterface(CmnDrawApi drawApi);
        void SetViewSettings(InteractorSettings settings);

        //地図描画
        void InitializeGraphics(ViewParam viewParam);
        void DrawBackGround(ViewParam viewParam);
        //void DrawMap(List<CmnTile> tileList, CmnObjFilter filter, int timeStamp);
        void DrawTileBorder(List<CmnTile> tileList, ViewParam viewParam);
        void DrawPoint(LatLon selectedLatLon, ViewParam viewParam);
        void DrawRouteGeometry(ViewParam viewParam);
        void UpdateImage();
        int DrawMapObj(CmnObjHandle cmnObjHandle, ViewParam viewParam);
        void DrawMap(List<CmnTile> tileList, CmnObjFilter filter, int timeStamp, ViewParam viewParam);
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
        void SetRouteGeometry(LatLon[] routeGeometry);
        //void SetSelectedLatLon(LatLon latlon);

        //ログ
        void PrintLog(int logType, string logStr);
        void OutputRoute(IEnumerable<CmnObjHandle> route);
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

        //フィルタ
        //public UInt32 drawMapObjType = 0xffffffff;
        public CmnObjFilter drawMapObjFilter = null;
        //public Dictionary<UInt32, UInt16> drawMapSubType;
        // public UInt16 drawLinkSubType = 0xffff;

        public int timeStamp;

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


}
