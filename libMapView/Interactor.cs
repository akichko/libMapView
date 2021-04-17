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
        public int ClickSearchRange = 1; //無制限ならint.MaxValue
        public int tileLoadDistanceX = 3;
        public int tileLoadDistanceY = 2;
        public int tileReleaseDistanceX = 15;
        public int tileReleaseDistanceY = 10;
        public int tileDrawDistanceX = 2;
        public int tileDrawDistanceY = 1;
        public bool isAllTileReadMode = false;
        public bool isAllTileLoaded = false;

        //表示コンテンツ
        public ushort drawMapObjType = 0xffff;
        public bool isTileBorderDisp = true; 

        //制御用

        bool drawEnable = false;
        bool isPaintNeeded = true;
        UInt16 drawObjType = 0xffff;

        byte currentTileLv;
        uint currentTileId;
        bool currentTileChanged = false;

        /* 起動・設定・終了 ***********************************************/

        public Interactor(IViewApi outputClass)
        {
            viewParam = new ViewParam(35.4629, 139.62657, 1.0);
            presenter = new Presenter(outputClass);
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
            if (isAllTileReadMode)
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
                    List<uint> tileIdList = mapMgr.tileApi.CalcTileIdAround(centerTileId, tileLoadDistanceX, tileLoadDistanceY);

                    foreach (uint tileId in tileIdList)
                    {
                        mapMgr.LoadTile(tileId);
                    }
                    //遠くの地図をアンロード

                    List<CmnTile> tileList = mapMgr.GetLoadedTileList();
                    foreach (CmnTile tile in tileList)
                    {
                        TileXY offset = mapMgr.tileApi.CalcTileAbsOffset(centerTileId, tile.tileId);
                        if (offset.x > tileReleaseDistanceX || offset.y > tileReleaseDistanceY)
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
            CmnObjHandle selectedHdl = mapMgr.SearchObj(baseLatLon, ClickSearchRange);

            if (selectedHdl == null)
            {
                presenter.SetRelatedObj(null);
                return;
            }
            presenter.SetSelectedObj(selectedHdl.mapObj);

            List<CmnObjHdlRef> relatedHdlList = mapMgr.SearchRefObject(selectedHdl);
            //List<CmnObjRef> refList = selectedHdl.mapObj.GetObjRefList(selectedHdl.tile);
            presenter.SetRelatedObj(relatedHdlList);
            presenter.ShowAttribute(selectedHdl.mapObj);

            //RefreshDrawArea();
        }


        /* 描画 ***********************************************/

        public void Paint(Graphics g)
        {
            if (!drawEnable || !isPaintNeeded)
                return;

            //タイル読み込み・解放
            RefleshMapCache();
            
            //描画対象タイルを特定
            List<CmnTile> drawTileList = mapMgr.SearchTiles(mapMgr.tileApi.CalcTileId(viewParam.viewCenter), tileDrawDistanceX, tileDrawDistanceY);

            //各タイルを描画
            presenter.DrawTile(g, drawTileList, viewParam, drawObjType);

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

        public LatLon GetLatLon(int x, int y) //描画エリアのXY→緯度経度
        {
            int offsetX = x - viewParam.width / 2;
            int offsetY = y - viewParam.height / 2;

            return viewParam.GetLatLon(offsetX, offsetY);

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
        void DrawTile(Graphics g, List<CmnTile> tileList, ViewParam viewParam, UInt16 objType);
      //       void DrawTile(Graphics g, List<CmnTile> tileList, UInt16 objType, ViewParam viewParam);
      //  void drawMapLink(Graphics g, List<CmnTile> tileList, ViewParam viewParam);
        void RefreshDrawArea();
        void UpdateCenterLatLon(LatLon latlon);
        void UpdateCenterTileId(uint tileId);
        void ShowAttribute(CmnObj mapLink);
        void SetSelectedObj(CmnObj mapLink);

        void SetDrawInterface(CmnDrawApi drawApi);

        //void DispDest(CmnObjHandle linkHdl);
    }


}
