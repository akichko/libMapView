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
using System.Threading;
using Akichko.libGis;
using System.Drawing;

namespace Akichko.libMapView
{
    public class Interactor //: IInputBoundary
    {
        protected CmnMapMgr mapMgr;
        protected ViewParam viewParam;
        protected Presenter presenter;
        //protected IOutputBoundary presenter;

        //動作設定
        protected InteractorSettings settings;


        protected Image imgCache;

        protected Dictionary<PointType, LatLon> drawPointDic;
        protected Dictionary<LineType, LatLon[]> drawLineDic;

        SemaphoreSlim semaphoReloading = new SemaphoreSlim(1, 1);

        //制御用
        public InteractorStatus status;

        public InteractorStatus Status { get { return status; } set { status = value; } }

        public bool IsPaintNeeded() => status.isPaintNeeded;

        public InteractorSettings GetSettings() => settings;

        public CmnMapMgr GetMapMgr() => mapMgr;

        /* 起動・設定・終了 ***********************************************/

        public Interactor(Presenter presenter, InteractorSettings settings)
        {
            this.presenter = presenter;
            this.settings = settings;
            this.status = new InteractorStatus();

            drawPointDic = new Dictionary<PointType, LatLon>();
            drawLineDic = new Dictionary<LineType, LatLon[]>();
        }


        public void SetMapMgr(CmnMapMgr mapMgr, CmnDrawApi drawApi)
        {
            this.mapMgr = mapMgr;
            presenter.SetDrawInterface(drawApi);

            status.drawEnable = true;
            status.isReloadNeeded = true;

            _ = RefreshMapCacheAsync();
        }


        public int Disconnect()
        {
            if (mapMgr == null)
                return -1;

            int ret = mapMgr.Disconnect();
            if (ret < 0)
                return -1;

            mapMgr = null;

            return 0;
        }

        public void SetSettings(InteractorSettings settings)
        {
            this.settings = settings;
        }


        /* 描画 ***********************************************/


        public Image MakeImage(Image img = null)
        {
            if (!status.drawEnable)
                return null;

            if (!status.isPaintNeeded)
                return (Image)imgCache.Clone();

            status.isPaintNeeded = false;

            int timeS = Environment.TickCount;

            //描画対象タイルを特定
            List<CmnTile> drawAreaTileList = CalcDrawAreaTileList();

            //描画
            presenter.InitializeGraphics(settings, viewParam, img);
            DrawMap(drawAreaTileList, viewParam, settings.drawMapObjFilter, settings.timeStamp);


            int exeTime = Environment.TickCount - timeS;
            presenter.PrintLog(1, $"Draw:{exeTime}");

            imgCache?.Dispose();
            imgCache = presenter.GetDrawAreaBitMap();
            return (Image)imgCache.Clone();
        }

        public void UpdateImage(Image drawAreaImg = null) => presenter.UpdateImage(drawAreaImg);



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
            //背景形状を描画
            if(settings.isAdminBoundaryDisp)
                presenter.DrawBackGround(viewParam);

            //各タイルを描画
            presenter.DrawTiles(tileList, filter, viewParam, timeStamp);

            //タイル枠描画
            if (settings.isTileBorderDisp)
                presenter.DrawTileBorder(tileList, viewParam);

            //ライン追加描画（ルート形状等）
            drawLineDic.ForEach(x => presenter.DrawLine(x.Value, viewParam, x.Key));

            //座標点追加描画
            drawPointDic.ForEach(x => presenter.DrawPoint(x.Value, viewParam, x.Key));

            //中心十字描画
            if (settings.isCenterMarkDisp)
                presenter.DrawCenterMark(viewParam);
        }

        public void Repaint()
        {
            status.isPaintNeeded = true;
        }


        public void RefreshDrawArea()
        {
            presenter.RefreshDrawArea();
        }

        bool CheckInDrawArea(uint tileId, TileXYL xylNW, TileXYL xylSE, int range)
        {
            TileXYL xyl = mapMgr.tileApi.CalcTileXYL(tileId);

            //暫定
            if (xylNW.x > xylSE.x)
                return true;

            if (xylNW.y < xylSE.y)
                return true;

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
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch(Exception e)
                {
                    throw new ApplicationException(e.Message, e.InnerException);
                }
#endif

                TimeStampRange timeStampRange = mapMgr.GetTimeStampRange();
                presenter.SetTimeStampRange(timeStampRange);

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

            semaphoReloading.Release();

            status.isPaintNeeded = true;
            RefreshDrawArea();

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

                _ = RefreshMapCacheAsync();
            }
        }

        /* 検索 ***********************************************/

        public void ShowAttribute(CmnObjHandle objHdl) => presenter.ShowAttribute(objHdl);

        public void SetSelectedObjHdl(CmnObjHandle objHdl) => presenter.SetSelectedObjHdl(objHdl);
   
        //MapSearchに移動
        public CmnObjHandle SearchObject(LatLon baseLatLon)
        {
            if (!status.drawEnable)
                return null;

            //最近傍オブジェクト取得
            status.selectedHdl = mapMgr.SearchObj(baseLatLon, settings.ClickSearchRange, settings.drawMapObjFilter, null, settings.timeStamp);
            if (status.selectedHdl == null)
                return null;

            //最近傍座標計算
            PolyLinePos nearestPos = LatLon.CalcNearestPoint(baseLatLon, status.selectedHdl?.Geometry);

            presenter.SetSelectedObjHdl(status.selectedHdl);

            //選択オブジェクト属性表示
            presenter.ShowAttribute(status.selectedHdl);

            //最近傍座標点描画
            SetDrawPoint(PointType.Nearest, nearestPos.latLon);

            //関連オブジェクト取得
            SearchRelatedObject(status.selectedHdl);

            return status.selectedHdl;
        }

        public CmnObjHandle SearchObject(uint tileId, uint objType, UInt64 objId)
        {
            if (!status.drawEnable)
                return null;

            status.selectedHdl = mapMgr.SearchObj(tileId, objType, objId, settings.timeStamp);
            presenter.SetSelectedObjHdl(status.selectedHdl);
            presenter.ShowAttribute(status.selectedHdl);

            SearchRelatedObject(status.selectedHdl);

            return status.selectedHdl;
        }

        //読み込み済みタイルから検索
        public CmnObjHandle SearchObject(uint objType, UInt64 objId)
        {
            if (!status.drawEnable)
                return null;

            status.selectedHdl = mapMgr.GetLoadedTileList()
                .Select(tile => mapMgr.SearchObj(tile.TileId, objType, objId, settings.timeStamp))
                .FirstOrDefault();

            presenter.SetSelectedObjHdl(status.selectedHdl);
            presenter.ShowAttribute(status.selectedHdl);

            SearchRelatedObject(status.selectedHdl);

            return status.selectedHdl;
        }

        //public CmnObjHandle SearchObject(uint tileId, uint objType, UInt16 objIndex)
        //{
        //    if (!status.drawEnable)
        //        return null;

        //    status.selectedHdl = mapMgr.SearchObj(tileId, objType, objIndex, settings.timeStamp);
        //    presenter.SetSelectedObjHdl(status.selectedHdl);
        //    presenter.ShowAttribute(status.selectedHdl);

        //    SearchRelatedObject(status.selectedHdl);

        //    return status.selectedHdl;
        //}

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

        public void SearchRelatedObject(CmnObjHandle selectedHdl)
        {
            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl)?.Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);
        }

        public CmnObjHandle SearchAttrObject(CmnSearchKey key)
        {
            CmnObjHandle attrHdl = mapMgr.SearchObj(key);

            return attrHdl;
        }


        public CmnObjHandle SearchRandomObject(uint objType)
        {
            uint randomTileId = mapMgr.GetRandomTileId();
            LatLon tileCenter = mapMgr.tileApi.CalcLatLon(randomTileId);

            SetViewCenter(tileCenter);
            mapMgr.LoadTile(randomTileId);
            CmnTile randomTile = mapMgr.SearchTile(randomTileId);
            CmnObjHandle randomObjHdl = randomTile.GetRandomObj(objType);

            return randomObjHdl;
        }


        /* 情報保持 ***********************************************/


        public void SetBoundaryGeometry(List<LatLon[]> boundaryList)
        {
            presenter.SetBoundaryList(boundaryList);
        }


        public void SetAttrSelectedObj(CmnObjHandle selectedAttr)
        {
            presenter.SetSelectedAttr(selectedAttr);
            status.isPaintNeeded = true;
        }

        public void ClearStatus()
        {
            status.Clear();
            drawPointDic.Clear();
            drawLineDic.Clear();
            presenter.SetSelectedObjHdl(null);
            presenter.SetRelatedObj(null);
            RefreshDrawArea();
        }

        public void SetDrawPoint(PointType pointType, LatLon latlon)
        {
            if (latlon != null)
            {
                drawPointDic[pointType] = latlon;
            }
            else //null -> clear
            {
                if (drawPointDic.ContainsKey(pointType))
                    drawPointDic.Remove(pointType);
            }

            if(pointType == PointType.Clicked)
            {
                presenter.UpdateLatLon(PointType.Clicked, latlon);
            }

           // status.isPaintNeeded = true;
        }

        public void SetDrawLine(LineType lineType, LatLon[] geometry)
        {
            if (geometry != null)
            {
                drawLineDic[lineType] = geometry;
            }
            else //null -> clear
            {
                if (drawLineDic.ContainsKey(lineType))
                    drawLineDic.Remove(lineType);
            }

            //status.isPaintNeeded = true;
        }



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

        public void SetViewCenter(uint tileId)
        {
            LatLon tileCenter = mapMgr.tileApi.CalcLatLon(tileId);
            viewParam.SetViewCenter(tileCenter);
            UpdateCurrentTileId();
        }


        /* その他 ***********************************************/

        //public virtual RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon) =>
        //    routeMgr.CalcRoute(orgLatLon, dstLatLon);

        public void OutputRoute(List<CmnObjHandle> route, LatLon[] routeGeometry) =>
            presenter.OutputRoute(route, routeGeometry);

       // public virtual RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
       //{
       //     ////計算
       //     RouteResult routeCalcResult = routeMgr.CalcRoute(orgLatLon, dstLatLon);

       //     if (routeCalcResult.resultCode != ResultCode.Success)
       //     {
       //         SetDrawLine(LineType.RouteGeometry, null);

       //         return routeCalcResult;
       //     }

       //     status.route = routeCalcResult.route.Select(x => x.DLinkHdl);
       //     LatLon[] routeGeometry = routeMgr.GetResult();
       //     presenter.OutputRoute(status.route, routeGeometry);

       //     SetDrawLine(LineType.RouteGeometry, routeGeometry);

       //     return routeCalcResult;
       // }

       // //自律走行経路
       // public virtual RouteResult CalcRoute(LatLon orgLatLon)
       // {
       //     //計算
       //     RouteResult routeCalcResult = routeMgr.CalcAutoRoute(orgLatLon);

       //     if (routeCalcResult.resultCode != ResultCode.Success)
       //     {
       //         SetDrawLine(LineType.RouteGeometry, null);

       //         return routeCalcResult;
       //     }

       //     //status.route = routeCalcResult.links.Select(x => x.DLinkHdl);

       //     LatLon[] routeGeometry = routeMgr.GetRouteGeometry(routeCalcResult.links);
       //     presenter.OutputRoute(routeCalcResult.links, routeGeometry);

       //     SetDrawLine(LineType.RouteGeometry, routeGeometry);

       //     return routeCalcResult;
       // }


        public void ShowAttribute()
        { }


    }

    //public interface IInputBoundary
    //{
    //    InteractorStatus Status { get; set; }

    //    //開始・終了
    //    void SetMapMgr(CmnMapMgr mapMgr, CmnDrawApi drawApi);
    //    void Shutdown();

    //    //ビュー設定
    //    void SetViewCenter(LatLon latlon);
    //    void SetViewCenter(uint tileId);
    //    void SetSettings(InteractorSettings settings);
    //    void SetViewParam(ViewParam viewParam);

    //    //描画設定
    //    void ClearStatus();
    //    void SetBoundaryGeometry(List<LatLon[]> boundaryList);
    //    void SetAttrSelectedObj(CmnObjHandle attrObjHdl);
    //    //void SetAttrSelectedLatLon(LatLon latlon);

    //    void SetDrawPoint(PointType pointType, LatLon latlon);

    //    //描画
    //    Task<int> RefreshMapCacheAsync();
    //    void Paint(IInputBoundary preInteractor = null);
    //    int MakeImage(IInputBoundary preInteractor = null);
    //    void UpdateImage();
    //    void RefreshDrawArea();

    //    //検索
    //    CmnObjHandle SearchObject(LatLon baseLatLon);
    //    CmnObjHandle SearchObject(uint tileId, uint objType, ulong objId);
    //    CmnObjHandle SearchObject(uint tileId, uint objType, UInt16 objIndex);
    //    CmnObjHandle SearchObject(CmnSearchKey key);
    //    CmnObjHandle SearchAttrObject(CmnSearchKey key);
    //    CmnObjHandle SearchRandomObject(uint objType);

    //    //ルート計算
    //    void SetRouteMgr(CmnRouteMgr routeMgr);
    //    RouteResult CalcRoute(LatLon orgLatLon, LatLon dstLatLon);
    //    RouteResult CalcRoute(LatLon orgLatLon);

    //}

    public interface IOutputBoundary
    {
        //設定
        void SetDrawInterface(CmnDrawApi drawApi);
        void SetViewSettings(InteractorSettings settings);

        //地図描画
        void InitializeGraphics(InteractorSettings settings, ViewParam viewParam, Image img = null);
        void DrawBackGround(ViewParam viewParam);
        void DrawTiles(List<CmnTile> tileList, CmnObjFilter filter, ViewParam viewParam, long timeStamp = -1);
        void DrawTileBorder(List<CmnTile> tileList, ViewParam viewParam);
        void DrawPoint(LatLon latlon, ViewParam viewParam, PointType type = PointType.Other);
        void DrawRouteGeometry(LatLon[] routeGeometry, ViewParam viewParam);
        void DrawCenterMark(ViewParam viewParam);
        void UpdateImage(Image drawAreaImg = null);
        //int DrawMapObj(CmnObjHandle cmnObjHandle, ViewParam viewParam);
        void RefreshDrawArea();

        //属性

        public void UpdateLatLon(PointType pointType, LatLon latlon);
        void UpdateCenterTileId(uint tileId);
        void ShowAttribute(CmnObjHandle objHdl);
        void SetSelectedObjHdl(CmnObjHandle objHdl);
        void SetSelectedAttr(CmnObjHandle selectedAttr);
        void SetRelatedObj(List<CmnObjHdlRef> relatedHdlList);
        void SetBoundaryList(List<LatLon[]> boundaryList);
        void OutputRoute(IEnumerable<CmnObjHandle> route, LatLon[] routeGeometry);

        //ログ
        void PrintLog(int logType, string logStr);
        void SetTimeStampRange(TimeStampRange timeStampRange);
    }

    public class InteractorSettings
    {
        //読み込み
        public int tileLoadDistanceX = 3;
        public int tileLoadDistanceY = 2;
        public int tileReleaseDistanceX = 12;
        public int tileReleaseDistanceY = 8;
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

        public bool isCarPosRadiusDisp = false;

        //フィルタ
        public CmnObjFilter drawMapObjFilter = null;
        public long timeStamp = -1;

        //経路計算
        //public DykstraSetting dykstraSetting = null;

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
        public CmnObjHandle selectedHdl = null;
        public IEnumerable<CmnObjHandle> route = null;
        //public LatLon[] routeGeometry = null;

        public void Clear()
        {
            selectedHdl = null;
            route = null;
            //routeGeometry = null;
        }
    }

    public enum PointType
    {
        Clicked,
        Nearest,
        AttrSelected,
        Origin,
        Destination,
        Location,
        DistanceBase,
        ViewCenter,
        Other
    }

    public enum LineType
    {
        RouteGeometry,
        Distance,
        Other
    }

}
