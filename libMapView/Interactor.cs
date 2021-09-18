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
    class Interactor : IInputBoundary
    {
        CmnMapMgr mapMgr;
        ViewParam viewParam;
        IOutputBoundary presenter;
        //Presenter presenter;

        //動作設定
        public InteractorSettings settings;

        //制御用
        bool drawEnable = false;
        bool isPaintNeeded = true;
        public bool isAllTileLoaded = false;

        byte currentTileLv;
        uint currentTileId;
        bool currentTileChanged = false;

        LatLon selectedLatLon;

        /* 起動・設定・終了 ***********************************************/

        public Interactor(IOutputBoundary presenter)
        {
            viewParam = new ViewParam(35.4629, 139.62657, 1.0);
            this.presenter = presenter;
        }


        public void OpenFile(string fileName, CmnMapMgr mapMgr)
        {
            this.mapMgr = mapMgr;
            mapMgr.Connect(fileName);

            //routeMgr = new RouteMgr(MapDataType.MapManager);

            RefleshMapCache();
            drawEnable = true;
            currentTileChanged = true;
            RefreshDrawArea();
        }


        public void SetDrawInterface(CmnDrawApi drawApi)
        {
            presenter.SetDrawInterface(drawApi);

        }

        public void Shutdown()
        {
            if(mapMgr != null)
                mapMgr.Disconnect();
        }

        public void SetViewSettings(InteractorSettings settings)
        {
            this.settings = settings;
        }


        /* データ管理 ***********************************************/

        private void RefleshMapCache()
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
                    //座標周辺の地図をロード
                    uint centerTileId = mapMgr.tileApi.CalcTileId(viewParam.viewCenter);
                    List<uint> tileIdList = mapMgr.tileApi.CalcTileIdAround(centerTileId, settings.tileLoadDistanceX, settings.tileLoadDistanceY);

                    foreach (uint tileId in tileIdList)
                    {
                        mapMgr.LoadTile(tileId, null);
                    }
                    //遠くの地図をアンロード

                    IEnumerable<CmnTile> tileList = mapMgr.GetLoadedTileList();
                    foreach (CmnTile tile in tileList)
                    {
                        TileXY offset = mapMgr.tileApi.CalcTileAbsOffset(centerTileId, tile.TileId);
                        if (offset.x > settings.tileReleaseDistanceX || offset.y > settings.tileReleaseDistanceY)
                            mapMgr.UnloadTile(tile.TileId);
                    }

                    currentTileChanged = false;
                }
            }
        }

        /* 検索 ***********************************************/



        public void SearchObject(LatLon baseLatLon)
        {
            if (!drawEnable)
                return;
            CmnObjHandle selectedHdl = mapMgr.SearchObj(baseLatLon, settings.drawMapObjFilter, settings.ClickSearchRange, settings.timeStamp);
            //CmnObjHandle selectedHdl = mapMgr.SearchObj(baseLatLon, settings.ClickSearchRange, true, settings.drawMapObjType);

            if (selectedHdl == null)
            {
                presenter.SetRelatedObj(null);
                return;
            }
            presenter.SetSelectedObjHdl(selectedHdl);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl).Where(x=>x.objHdl != null).ToList();
            //List<CmnObjRef> refList = selectedHdl.obj.GetObjRefList(selectedHdl.tile);
            presenter.SetRelatedObj(relatedHdlList);
            presenter.ShowAttribute(selectedHdl);

        }

        public CmnObjHandle SearchObject(uint tileId, uint objType, UInt64 objId)
        {
            if (!drawEnable)
                return null;
            CmnObjHandle selectedHdl = mapMgr.SearchObj(tileId, objType, objId);

            if (selectedHdl == null)
            {
                presenter.SetRelatedObj(null);
                return null;
            }
            presenter.SetSelectedObjHdl(selectedHdl);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl).Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);
            presenter.ShowAttribute(selectedHdl);

            return selectedHdl;
        }

        public CmnObjHandle SearchObject(uint tileId, uint objType, UInt16 objIndex)
        {
            if (!drawEnable)
                return null;
            CmnObjHandle selectedHdl = mapMgr.SearchObj(tileId, objType, objIndex);

            if (selectedHdl == null)
            {
                presenter.SetRelatedObj(null);
                return null;
            }
            presenter.SetSelectedObjHdl(selectedHdl);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl).Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);
            presenter.ShowAttribute(selectedHdl);

            return selectedHdl;
        }

        public CmnObjHandle SearchObject(CmnSearchKey key)
        {
            if (!drawEnable)
                return null;
            CmnObjHandle selectedHdl = mapMgr.SearchObj(key);

            if (selectedHdl == null)
            {
                presenter.SetRelatedObj(null);
                return null;
            }
            presenter.SetSelectedObjHdl(selectedHdl);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl).Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);
            presenter.ShowAttribute(selectedHdl);

            return selectedHdl;
        }

        /* 描画 ***********************************************/

        public void Paint()
        {
            if (!drawEnable || !isPaintNeeded)
                return;

            int timeS = Environment.TickCount;

            //タイル読み込み・解放
            RefleshMapCache();
            int timeRefleshCache = Environment.TickCount - timeS;

            //描画対象タイルを特定
            uint centerTileId = mapMgr.tileApi.CalcTileId(viewParam.viewCenter);
            IEnumerable<CmnTile> drawTileList = mapMgr.SearchTiles(centerTileId, settings.tileDrawDistanceX, settings.tileDrawDistanceY);

            //描画エリア内タイル抽出（隣接１メッシュ）
            TileXYL xylNW = mapMgr.tileApi.CalcTileXYL(GetLatLon(0,0));
            TileXYL xylSE = mapMgr.tileApi.CalcTileXYL(GetLatLon(viewParam.Width, viewParam.Height));

            List<CmnTile> drawTileList2 = drawTileList
                .Where(x => CheckInDrawArea(x.TileId, xylNW, xylSE, 0))
                .ToList();


            //各タイルを描画
            DrawTile(drawTileList2, viewParam, settings.drawMapObjFilter);

            int timeE = Environment.TickCount - timeS;

            presenter.PrintLog(0, $"{timeRefleshCache},{timeE}");

            isPaintNeeded = false;
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
        public void DrawTile(List<CmnTile> tileList, ViewParam viewParam, CmnObjFilter filter)
        {
            int timeS = Environment.TickCount;
            //設定
            presenter.SetViewSettings(settings);

            //Graphics初期化
            presenter.InitializeGraphics(viewParam);
            int timeGInit = Environment.TickCount - timeS;

            //背景形状を描画
            presenter.DrawBackGround();

            //各タイルを描画
            presenter.DrawMap(tileList, filter);
            int timeDrawMap = Environment.TickCount - timeS;

            //タイル枠描画
            presenter.DrawTileBorder(tileList);

            //選択座標点追加描画
            presenter.DrawPoint(selectedLatLon);

            //ルート形状描画
            presenter.DrawRouteGeometry();

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
            presenter.SetSelectedLatLon(latlon);
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

        public LatLon GetLatLon(int x, int y) //描画エリアのXY→緯度経度
        {
            int offsetX = x - viewParam.Width / 2;
            int offsetY = y - viewParam.Height / 2;

            return viewParam.GetLatLon(offsetX, offsetY);

        }

        public LatLon GetViewCenter()
        {
            return viewParam.viewCenter;
        }


        /* 描画用パラメータ変更 ***********************************************/

        public void SetDrawAreaSize(int width, int height)
        {
            viewParam.Width = width;
            viewParam.Height = height;
        }

        public void SetViewCenter(LatLon latlon)
        {
            viewParam.SetViewCenter(latlon);
            UpdateCurrentTileId();
            //RefreshDrawArea();

            //if
            //currentTileChanged = true;
        }

        public void MoveViewCenter(LatLon relLatLon)
        {
            viewParam.MoveViewCenter(relLatLon);
            //presenter.UpdateCenterTileId(mapMgr.tileApi.CalcTileId(viewParam.viewCenter));
            presenter.UpdateCenterLatLon(viewParam.viewCenter);
            UpdateCurrentTileId();
            // RefreshDrawArea();

            //if
            //currentTileChanged = true;
        }

        public void MoveViewCenter(int x, int y)
        {
            viewParam.MoveViewCenter(x, y);
            //presenter.UpdateCenterTileId(mapMgr.tileApi.CalcTileId(viewParam.viewCenter));
            presenter.UpdateCenterLatLon(viewParam.viewCenter);
            UpdateCurrentTileId();
            //RefreshDrawArea();

            //if
            //currentTileChanged = true;
        }

        public void ChangeZoom(double delta, int x, int y)
        {

            LatLon clickedLatLon = GetLatLon(x, y);

            viewParam.Zoom *= delta;

            LatLon afterLatLon = GetLatLon(x, y);

            //マウス位置保持のための移動
            LatLon relLatLon = new LatLon(clickedLatLon.lat - afterLatLon.lat, clickedLatLon.lon - afterLatLon.lon);
            MoveViewCenter(relLatLon);

            presenter.UpdateCenterLatLon(viewParam.viewCenter);

        }


        /* テキスト表示 ***********************************************/

        private void UpdateCurrentTileId()
        {
            uint newTileId = mapMgr?.tileApi.CalcTileId(viewParam.viewCenter) ?? 0;
            if(currentTileId != newTileId)
            {
                currentTileId = newTileId;
                currentTileChanged = true;
            }
        }


        /* その他 ***********************************************/


        public void CalcRoute(LatLon orgLatLon, LatLon dstLatLon)
        {
            //CmnRouteMgr routeMgr = new CmnRouteMgr(mapMgr);
            CmnRouteMgr routeMgr = mapMgr.CreateRouteMgr();
            
            routeMgr.orgLatLon = orgLatLon;
            routeMgr.dstLatLon = dstLatLon;

            //Prepare
            routeMgr.Prepare(false);

            //計算
            routeMgr.CalcRoute();

            SetRouteGeometry(routeMgr.GetResult());

            return;
        }



        public void ShowAttribute()
        { }
    }

    public interface IInputBoundary
    {
        void OpenFile(string fileName, CmnMapMgr mapMgr);
        void Shutdown();

        void CalcRoute(LatLon orgLatLon, LatLon dstLatLon);
        LatLon GetLatLon(int x, int y);
        void MoveViewCenter(int x, int y);

        void ChangeZoom(double v, int x, int y);
        void Paint();
        void RefreshDrawArea();

        void SearchObject(LatLon baseLatLon);
        CmnObjHandle SearchObject(uint tileId, uint objType, ulong objId);
        CmnObjHandle SearchObject(CmnSearchKey key);

        void SetBoundaryGeometry(List<LatLon[]> boundaryList);
        void SetClickedLatLon(LatLon clickedLatLon);
        void SetDrawAreaSize(int width, int height);
        void SetDrawInterface(CmnDrawApi drawApi);
        void SetRouteGeometry(LatLon[] routeGeometry);
        void SetSelectedAttr(CmnObjHandle attrObjHdl);
        void SetSelectedLatLon(LatLon latlon);
        void SetViewCenter(LatLon latlon);
        void SetViewSettings(InteractorSettings settings);
    }

    public interface IOutputBoundary
    {
        void SetDrawInterface(CmnDrawApi drawApi);
        void SetViewSettings(InteractorSettings settings);
     
        //void DrawTile(List<CmnTile> tileList, ViewParam viewParam, UInt32 objType, Dictionary<uint, UInt16> subType);
        //void DrawTile(List<CmnTile> tileList, ViewParam viewParam, CmnObjFilter filter);
        //       void DrawTile(Graphics g, List<CmnTile> tileList, UInt32 objType, ViewParam viewParam);
        //  void drawMapLink(Graphics g, List<CmnTile> tileList, ViewParam viewParam);
        void RefreshDrawArea();

        void UpdateCenterLatLon(LatLon latlon);
        void UpdateCenterTileId(uint tileId);
        void ShowAttribute(CmnObjHandle objHdl);
        void SetSelectedObjHdl(CmnObjHandle objHdl);

        void SetSelectedAttr(CmnObjHandle selectedAttr);
        void SetRelatedObj(List<CmnObjHdlRef> relatedHdlList);
        void UpdateClickedLatLon(LatLon clickedLatLon);

        void PrintLog(int logType, string logStr);
        void SetBoundaryList(List<LatLon[]> boundaryList);
        void SetRouteGeometry(LatLon[] routeGeometry);
        void SetSelectedLatLon(LatLon latlon);
        void InitializeGraphics(ViewParam viewParam);
        void DrawBackGround();
        void DrawMap(List<CmnTile> tileList, CmnObjFilter filter);
        void DrawTileBorder(List<CmnTile> tileList);
        void DrawPoint(LatLon selectedLatLon);
        void DrawRouteGeometry();
        void UpdateImage();
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
