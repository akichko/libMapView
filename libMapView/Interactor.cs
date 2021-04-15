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
        CmnObjHandle selectedHdl;

        bool drawEnable = false;
        bool isPaintNeeded = true;

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

        public void Shutdown()
        {
            if(mapMgr != null)
                mapMgr.Disconnect();
        }


        public void SetDrawInterface(CmnDrawApi drawApi)
        {
            presenter.SetDrawInterface(drawApi);

        }

        public void SetDrawAreaSize(int width, int height)
        {
            viewParam.width = width;
            viewParam.height = height;
        }

        public void SetViewCenter(LatLon latlon)
        {

        }

        private void RefleshMapCache()
        {
            //座標周辺の地図をロード
            uint centerTileId = mapMgr.tileApi.CalcTileId(viewParam.viewCenter);
            List<uint> tileList = mapMgr.tileApi.CalcTileIdAround(centerTileId, 1, 1);

            foreach (uint tileId in tileList)
            {
                mapMgr.LoadTile(tileId);
            }

            //遠くの地図をアンロード

        }

        public void Paint(Graphics g)
        {
            if (!drawEnable || !isPaintNeeded)
                return;

            RefleshMapCache();
            
            //描画対象タイルを特定
            List<CmnTile> drawTileList = mapMgr.SearchTiles(mapMgr.tileApi.CalcTileId(viewParam.viewCenter), 1, 1);

            //各タイルを描画
            presenter.DrawTile(g, drawTileList, viewParam, 0xFFFF);

            isPaintNeeded = false;
            //presenter.drawMapLink(g, drawTileList, viewParam);
        }

        public void RefreshDrawArea()
        {
            isPaintNeeded = true;
            presenter.RefreshDrawArea();
        }

        public LatLon GetLatLon(int x, int y)
        {
            int offsetX = x - viewParam.width / 2;
            int offsetY = y - viewParam.height / 2;

            return viewParam.GetLatLon(offsetX, offsetY);

        }

        public LatLon GetLatLonOld(int offsetX, int offsetY)
        {
            return viewParam.GetLatLon(offsetX, offsetY);
        }


        public void MoveViewCenter(LatLon relLatLon)
        {
            viewParam.MoveViewCenter(relLatLon);
            presenter.UpdateCenterTileId(mapMgr.tileApi.CalcTileId(viewParam.viewCenter));
            presenter.UpdateCenterLatLon(viewParam.viewCenter);
            RefreshDrawArea();

        }

        public void MoveViewCenter(int x, int y)
        {
            viewParam.MoveViewCenter(x, y);
            //presenter.UpdateCenterTileId(mapMgr.tileApi.CalcTileId(viewParam.viewCenter));
            presenter.UpdateCenterLatLon(viewParam.viewCenter);
            RefreshDrawArea();
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



        public void SearchObject(LatLon baseLatLon)
        {
            if (!drawEnable)
                return;
            CmnObjHandle linkHdl = mapMgr.SearchObj(baseLatLon);

            if (linkHdl != null)
            {
                selectedHdl = linkHdl;
                presenter.SetSelectedLink(linkHdl.mapObj);
                presenter.ShowAttribute(linkHdl.mapObj);
            }
            RefreshDrawArea();
        }

        //public void SetRouteOrigin()
        //{
        //    orginHdl = selectedHdl;
        //}

        //public void SetRouteDestination()
        //{
        //    destHdl = selectedHdl;
        //    presenter.DispDest(destHdl);
        //}

        //public int CalcRoute()
        //{
        //    return 0;
        //}

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
        void SetSelectedLink(CmnObj mapLink);

        void SetDrawInterface(CmnDrawApi drawApi);

        //void DispDest(CmnObjHandle linkHdl);
    }


}
