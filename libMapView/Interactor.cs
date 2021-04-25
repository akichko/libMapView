using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using libGis;

namespace libMapView
{
    class Interactor
    {
        CmnMapMgr mapMgr;
        ViewParam viewParam;
        //IOutputBoundary presenter;
        Presenter presenter;

        //動作設定
        public InteractorSettings settings;

        //制御用
        bool drawEnable = false;
        bool isPaintNeeded = true;
        //UInt16 drawObjType = 0xffff;
        public bool isAllTileLoaded = false;

        byte currentTileLv;
        uint currentTileId;
        bool currentTileChanged = false;

        /* 起動・設定・終了 ***********************************************/

        public Interactor(IViewApi outputClass, InteractorSettings settings = null)
        {
            viewParam = new ViewParam(35.4629, 139.62657, 1.0);
            presenter = new Presenter(outputClass);
            if (settings != null)
                this.settings = settings;
            else
                this.settings = new InteractorSettings();
        }

        public void OpenFile(string fileName, CmnMapMgr mapMgr)
        {
            this.mapMgr = mapMgr;
            mapMgr.Connect(fileName);

            //routeMgr = new RouteMgr(MapDataType.MapManager);

            RefleshMapCache();
            drawEnable = true;
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
                        mapMgr.LoadTile(tileId);
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
                        mapMgr.LoadTile(tileId);
                    }
                    //遠くの地図をアンロード

                    List<CmnTile> tileList = mapMgr.GetLoadedTileList();
                    foreach (CmnTile tile in tileList)
                    {
                        TileXY offset = mapMgr.tileApi.CalcTileAbsOffset(centerTileId, tile.tileId);
                        if (offset.x > settings.tileReleaseDistanceX || offset.y > settings.tileReleaseDistanceY)
                            mapMgr.UnloadTile(tile.tileId);
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
            CmnObjHandle selectedHdl = mapMgr.SearchObj(baseLatLon, settings.ClickSearchRange, true, settings.drawMapObjType);

            if (selectedHdl == null)
            {
                presenter.SetRelatedObj(null);
                return;
            }
            presenter.SetSelectedObj(selectedHdl.obj);

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
            presenter.SetSelectedObj(selectedHdl.obj);

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
            presenter.SetSelectedObj(selectedHdl.obj);

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
            presenter.SetSelectedObj(selectedHdl.obj);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl).Where(x => x.objHdl != null).ToList();
            presenter.SetRelatedObj(relatedHdlList);
            presenter.ShowAttribute(selectedHdl);

            return selectedHdl;
        }

        /* 描画 ***********************************************/

        public void Paint(Graphics g)
        {
            if (!drawEnable || !isPaintNeeded)
                return;

            //タイル読み込み・解放
            RefleshMapCache();
            
            //描画対象タイルを特定
            List<CmnTile> drawTileList = mapMgr.SearchTiles(mapMgr.tileApi.CalcTileId(viewParam.viewCenter), settings.tileDrawDistanceX, settings.tileDrawDistanceY);

            //各タイルを描画
            presenter.DrawTile(g, drawTileList, viewParam, settings.drawMapObjType);

            isPaintNeeded = false;
            //presenter.drawMapLink(g, drawTileList, viewParam);
        }

        public void RefreshDrawArea()
        {
            isPaintNeeded = true;
            presenter.RefreshDrawArea();
        }

        public void SetSelectedLatLon(LatLon latlon)
        {
            presenter.selectedLatLon = latlon;
            isPaintNeeded = true;
        }

        public void SetRouteGeometry(LatLon[] routeGeometry)
        {
            presenter.routeGeometry = routeGeometry;
        }

        public void SetRouteObjList(List<CmnDirObjHandle> routeObjList)
        {
            presenter.SetRouteObjList(routeObjList);
        }

        public LatLon GetLatLon(int x, int y) //描画エリアのXY→緯度経度
        {
            int offsetX = x - viewParam.width / 2;
            int offsetY = y - viewParam.height / 2;

            return viewParam.GetLatLon(offsetX, offsetY);

        }

        public LatLon GetViewCenter()
        {
            return viewParam.viewCenter;
        }


        /* 描画用パラメータ変更 ***********************************************/

        public void SetDrawAreaSize(int width, int height)
        {
            viewParam.width = width;
            viewParam.height = height;
        }

        public void SetViewCenter(LatLon latlon)
        {
            viewParam.SetViewCenter(latlon);
            // UpdateCurrentTileId();
            //RefreshDrawArea();

            //if
            currentTileChanged = true;
        }

        public void MoveViewCenter(LatLon relLatLon)
        {
            viewParam.MoveViewCenter(relLatLon);
            //presenter.UpdateCenterTileId(mapMgr.tileApi.CalcTileId(viewParam.viewCenter));
            presenter.UpdateCenterLatLon(viewParam.viewCenter);
            // UpdateCurrentTileId();
            // RefreshDrawArea();

            //if
            currentTileChanged = true;
        }

        public void MoveViewCenter(int x, int y)
        {
            viewParam.MoveViewCenter(x, y);
            //presenter.UpdateCenterTileId(mapMgr.tileApi.CalcTileId(viewParam.viewCenter));
            presenter.UpdateCenterLatLon(viewParam.viewCenter);
            //UpdateCurrentTileId();
            //RefreshDrawArea();

            //if
            currentTileChanged = true;
        }

        public void ChangeZoom(double delta, int x, int y)
        {

            LatLon clickedLatLon = GetLatLon(x, y);

            viewParam.zoom *= delta;

            LatLon afterLatLon = GetLatLon(x, y);

            //マウス位置保持のための移動
            LatLon relLatLon = new LatLon(clickedLatLon.lat - afterLatLon.lat, clickedLatLon.lon - afterLatLon.lon);
            MoveViewCenter(relLatLon);

            presenter.UpdateCenterLatLon(viewParam.viewCenter);

        }


        /* テキスト表示 ***********************************************/

        private void UpdateCurrentTileId()
        {
            uint newTileId = mapMgr.tileApi.CalcTileId(viewParam.viewCenter);
            if(currentTileId != newTileId)
            {
                currentTileId = newTileId;
                currentTileChanged = true;
            }
        }


        /* その他 ***********************************************/


        public void ShowAttribute()
        { }
    }

    public interface IInputBoundary
    {

    }

    public interface IOutputBoundary
    {
        void DrawTile(Graphics g, List<CmnTile> tileList, ViewParam viewParam, UInt32 objType);
      //       void DrawTile(Graphics g, List<CmnTile> tileList, UInt32 objType, ViewParam viewParam);
      //  void drawMapLink(Graphics g, List<CmnTile> tileList, ViewParam viewParam);
        void RefreshDrawArea();
        void UpdateCenterLatLon(LatLon latlon);
        void UpdateCenterTileId(uint tileId);
        void ShowAttribute(CmnObjHandle objHdl);
        void SetSelectedObj(CmnObj mapLink);

        void SetDrawInterface(CmnDrawApi drawApi);

        //void DispDest(CmnObjHandle linkHdl);
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
        public UInt32 drawMapObjType = 0xffffffff;
        public bool isTileBorderDisp = true;
    }

}
